﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ExceptionExtensions;
using osu.Framework.Logging;

namespace osu.Framework.IO.Network
{
    public class WebRequest : IDisposable
    {
        internal const int MAX_RETRIES = 1;

        /// <summary>
        /// Whether non-SSL requests should be allowed. Defaults to disabled.
        /// In the default state, http:// requests will be automatically converted to https://.
        /// </summary>
        public bool AllowInsecureRequests;

        /// <summary>
        /// Invoked when a response has been received, but not data has been received.
        /// </summary>
        public event Action Started;

        /// <summary>
        /// Invoked when the <see cref="WebRequest"/> has finished successfully.
        /// </summary>
        public event Action Finished;

        /// <summary>
        /// Invoked when the <see cref="WebRequest"/> has failed.
        /// </summary>
        public event Action<Exception> Failed;

        /// <summary>
        /// Invoked when the download progress has changed.
        /// </summary>
        public event Action<long, long> DownloadProgress;

        /// <summary>
        /// Invoked when the upload progress has changed.
        /// </summary>
        public event Action<long, long> UploadProgress;

        /// <summary>
        /// Whether the <see cref="WebRequest"/> was aborted due to an exception or a user abort request.
        /// </summary>
        public bool Aborted { get; private set; }

        private bool completed;

        /// <summary>
        /// Whether the <see cref="WebRequest"/> has been run.
        /// </summary>
        public bool Completed
        {
            get => completed;
            private set
            {
                completed = value;
                if (!completed) return;

                // WebRequests can only be used once - no need to keep events bound
                // This helps with disposal in PerformAsync usages
                Started = null;
                Finished = null;
                Failed = null;
                DownloadProgress = null;
                UploadProgress = null;
            }
        }

        /// <summary>
        /// The URL of this request.
        /// </summary>
        public string Url;

        /// <summary>
        /// POST parameters.
        /// </summary>
        private readonly Dictionary<string, string> parameters = new Dictionary<string, string>();

        /// <summary>
        /// FILE parameters.
        /// </summary>
        private readonly IDictionary<string, byte[]> files = new Dictionary<string, byte[]>();

        /// <summary>
        /// The request headers.
        /// </summary>
        private readonly IDictionary<string, string> headers = new Dictionary<string, string>();

        public const int DEFAULT_TIMEOUT = 10000;

        public HttpMethod Method = HttpMethod.Get;

        /// <summary>
        /// The amount of time from last sent or received data to trigger a timeout and abort the request.
        /// </summary>
        public int Timeout = DEFAULT_TIMEOUT;

        /// <summary>
        /// The type of content expected by this web request.
        /// </summary>
        protected virtual string Accept => string.Empty;

        /// <summary>
        /// The value of the User-agent HTTP header.
        /// </summary>
        protected virtual string UserAgent => "osu-framework";

        internal int RetryCount { get; private set; }

        /// <summary>
        /// Whether this request should internally retry (up to <see cref="MAX_RETRIES"/> times) on a timeout before throwing an exception.
        /// </summary>
        public bool AllowRetryOnTimeout { get; set; } = true;

        private static readonly HttpClient client = new HttpClient(
#if NET5_0
            new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                ConnectCallback = onConnect,
            }
#else
            new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }
#endif
        )
        {
            // Timeout is controlled manually through cancellation tokens because
            // HttpClient does not properly timeout while reading chunked data
            Timeout = System.Threading.Timeout.InfiniteTimeSpan
        };

        private static readonly Logger logger = Logger.GetLogger(LoggingTarget.Network);

        public WebRequest(string url = null, params object[] args)
        {
            if (!string.IsNullOrEmpty(url))
                Url = args.Length == 0 ? url : string.Format(url, args);
        }

        ~WebRequest()
        {
            Dispose(false);
        }

        private int responseBytesRead;

        private const int buffer_size = 32768;
        private byte[] buffer;

        private MemoryStream rawContent;

        public string ContentType;

        protected virtual Stream CreateOutputStream() => new MemoryStream();

        public Stream ResponseStream;

        /// <summary>
        /// Retrieve the full response body as a UTF8 encoded string.
        /// </summary>
        /// <returns>The response body.</returns>
        [CanBeNull]
        public string GetResponseString()
        {
            try
            {
                ResponseStream.Seek(0, SeekOrigin.Begin);
                StreamReader r = new StreamReader(ResponseStream, Encoding.UTF8);
                return r.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve the full response body as an array of bytes.
        /// </summary>
        /// <returns>The response body.</returns>
        public byte[] GetResponseData()
        {
            try
            {
                byte[] data = new byte[ResponseStream.Length];
                ResponseStream.Seek(0, SeekOrigin.Begin);
                ResponseStream.Read(data, 0, data.Length);
                return data;
            }
            catch
            {
                return null;
            }
        }

        public HttpResponseHeaders ResponseHeaders => response.Headers;

        private CancellationToken? userToken;
        private CancellationTokenSource abortToken;
        private CancellationTokenSource timeoutToken;

        private LengthTrackingStream requestStream;
        private HttpResponseMessage response;

        private long contentLength => requestStream?.Length ?? 0;

        private const string form_boundary = "-----------------------------28947758029299";

        private const string form_content_type = "multipart/form-data; boundary=" + form_boundary;

        /// <summary>
        /// Performs the request asynchronously.
        /// </summary>
        public async Task PerformAsync() => await PerformAsync(default);

        /// <summary>
        /// Performs the request asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        public async Task PerformAsync(CancellationToken cancellationToken)
        {
            if (Completed)
                throw new InvalidOperationException($"The {nameof(WebRequest)} has already been run.");

            try
            {
                await internalPerform(cancellationToken);
            }
            catch (AggregateException ae)
            {
                ae.RethrowAsSingular();
            }
        }

        private async Task internalPerform(CancellationToken cancellationToken = default)
        {
            var url = Url;

            if (!AllowInsecureRequests && !url.StartsWith(@"https://", StringComparison.Ordinal))
            {
                logger.Add($"Insecure request was automatically converted to https ({Url})");
                url = @"https://" + url.Replace(@"http://", @"");
            }

            // If a user token already exists, keep it. Otherwise, take on the previous user token, as this could be a retry of the request.
            userToken ??= cancellationToken;
            cancellationToken = userToken.Value;

            using (abortToken ??= new CancellationTokenSource()) // don't recreate if already non-null. is used during retry logic.
            using (timeoutToken = new CancellationTokenSource())
            using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(abortToken.Token, timeoutToken.Token, cancellationToken))
            {
                try
                {
                    PrePerform();

                    HttpRequestMessage request;

                    if (Method == HttpMethod.Get)
                    {
                        if (files.Count > 0)
                            throw new InvalidOperationException($"Cannot use {nameof(AddFile)} in a GET request. Please set the {nameof(Method)} to POST.");

                        StringBuilder requestParameters = new StringBuilder();
                        foreach (var p in parameters)
                            requestParameters.Append($@"{p.Key}={p.Value}&");
                        string requestString = requestParameters.ToString().TrimEnd('&');

                        request = new HttpRequestMessage(HttpMethod.Get, string.IsNullOrEmpty(requestString) ? url : $"{url}?{requestString}");
                    }
                    else
                    {
                        request = new HttpRequestMessage(Method, url);

                        Stream postContent;

                        if (rawContent != null)
                        {
                            if (parameters.Count > 0)
                                throw new InvalidOperationException($"Cannot use {nameof(AddRaw)} in conjunction with {nameof(AddParameter)}");
                            if (files.Count > 0)
                                throw new InvalidOperationException($"Cannot use {nameof(AddRaw)} in conjunction with {nameof(AddFile)}");

                            postContent = new MemoryStream();
                            rawContent.Position = 0;

                            await rawContent.CopyToAsync(postContent, linkedToken.Token);

                            postContent.Position = 0;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(ContentType) && ContentType != form_content_type)
                                throw new InvalidOperationException($"Cannot use custom {nameof(ContentType)} in a POST request.");

                            ContentType = form_content_type;

                            var formData = new MultipartFormDataContent(form_boundary);

                            foreach (var p in parameters)
                                formData.Add(new StringContent(p.Value), p.Key);

                            foreach (var p in files)
                            {
                                var byteContent = new ByteArrayContent(p.Value);
                                byteContent.Headers.Add("Content-Type", "application/octet-stream");
                                formData.Add(byteContent, p.Key, p.Key);
                            }

#if NET5_0
                            postContent = await formData.ReadAsStreamAsync(linkedToken.Token);
#else
                            postContent = await formData.ReadAsStreamAsync();
#endif
                        }

                        requestStream = new LengthTrackingStream(postContent);
                        requestStream.BytesRead.ValueChanged += e =>
                        {
                            reportForwardProgress();
                            UploadProgress?.Invoke(e.NewValue, contentLength);
                        };

                        request.Content = new StreamContent(requestStream);
                        if (!string.IsNullOrEmpty(ContentType))
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);
                    }

                    request.Headers.UserAgent.TryParseAdd(UserAgent);

                    if (!string.IsNullOrEmpty(Accept))
                        request.Headers.Accept.TryParseAdd(Accept);

                    foreach (var kvp in headers)
                        request.Headers.Add(kvp.Key, kvp.Value);

                    reportForwardProgress();

                    using (request)
                    {
                        response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedToken.Token);

                        ResponseStream = CreateOutputStream();

                        if (Method == HttpMethod.Get)
                        {
                            //GETs are easy
                            await beginResponse(linkedToken.Token);
                        }
                        else
                        {
                            reportForwardProgress();
                            UploadProgress?.Invoke(0, contentLength);

                            await beginResponse(linkedToken.Token);
                        }
                    }
                }
                catch (Exception) when (timeoutToken.IsCancellationRequested)
                {
                    Complete(new WebException($"Request to {url} timed out after {timeSinceLastAction / 1000} seconds idle (read {responseBytesRead} bytes, retried {RetryCount} times).",
                        WebExceptionStatus.Timeout));
                }
                catch (Exception) when (abortToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    onAborted();
                }
                catch (Exception e)
                {
                    if (Completed)
                        // we may be coming from one of the exception blocks handled above (as Complete will rethrow all exceptions).
                        throw;

                    Complete(e);
                }
            }

            void onAborted()
            {
                // Aborting via the cancellation token will not set the correct aborted/completion states. Make sure they're set here.
                Abort();

                Complete(new WebException($"Request to {url} aborted by user.", WebExceptionStatus.RequestCanceled));
            }
        }

        /// <summary>
        /// Performs the request synchronously.
        /// </summary>
        public void Perform()
        {
            try
            {
                PerformAsync().Wait();
            }
            catch (AggregateException ae)
            {
                ae.RethrowAsSingular();
            }
        }

        /// <summary>
        /// Task to run direct before performing the request.
        /// </summary>
        protected virtual void PrePerform()
        {
        }

        private async Task beginResponse(CancellationToken cancellationToken)
        {
#if NET5_0
            using (var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken))
#else
            using (var responseStream = await response.Content.ReadAsStreamAsync())
#endif
            {
                reportForwardProgress();
                Started?.Invoke();

                buffer = new byte[buffer_size];

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int read = await responseStream.ReadAsync(buffer.AsMemory(), cancellationToken);

                    reportForwardProgress();

                    if (read > 0)
                    {
                        await ResponseStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                        responseBytesRead += read;
                        DownloadProgress?.Invoke(responseBytesRead, response.Content.Headers.ContentLength ?? responseBytesRead);
                    }
                    else
                    {
                        ResponseStream.Seek(0, SeekOrigin.Begin);
                        Complete();
                        break;
                    }
                }
            }
        }

        protected virtual void Complete(Exception e = null)
        {
            if (Aborted)
                return;

            var we = e as WebException;

            bool allowRetry = AllowRetryOnTimeout;
            bool wasTimeout = false;

            if (e != null)
                wasTimeout = we?.Status == WebExceptionStatus.Timeout;
            else if (!response.IsSuccessStatusCode)
            {
                e = new WebException(response.StatusCode.ToString());

                switch (response.StatusCode)
                {
                    case HttpStatusCode.GatewayTimeout:
                    case HttpStatusCode.RequestTimeout:
                        wasTimeout = true;
                        break;
                }
            }

            allowRetry &= wasTimeout;

            if (e != null)
            {
                if (allowRetry && RetryCount < MAX_RETRIES && responseBytesRead == 0)
                {
                    RetryCount++;

                    logger.Add($@"Request to {Url} failed with {e} (retrying {RetryCount}/{MAX_RETRIES}).");

                    //do a retry
                    internalPerform().Wait();
                    return;
                }

                logger.Add($"Request to {Url} failed with {e}.");

                if (ResponseStream?.CanSeek == true && ResponseStream.Length > 0)
                {
                    // in the case we fail a request, spitting out the response in the log is quite helpful.
                    ResponseStream.Seek(0, SeekOrigin.Begin);

                    using (StreamReader r = new StreamReader(ResponseStream, new UTF8Encoding(false, true), true, 1024, true))
                    {
                        try
                        {
                            char[] output = new char[1024];
                            int read = r.ReadBlock(output, 0, 1024);
                            string trimmedResponse = new string(output, 0, read);
                            logger.Add($"Response was: {trimmedResponse}");
                            if (read == 1024)
                                logger.Add("(Response was trimmed)");
                        }
                        catch (DecoderFallbackException)
                        {
                            // Ignore non-text format
                        }
                    }
                }
            }
            else
                logger.Add($@"Request to {Url} successfully completed!");

            // if a failure happened on performing the request, there are still situations where we want to process the response.
            // consider the case of a server returned error code which triggers a WebException, but the server is also returning details on the error in the response.
            try
            {
                if (!wasTimeout)
                    ProcessResponse();
            }
            catch (Exception se)
            {
                // that said, we don't really care about an error when processing the response if there is already a higher level exception.
                if (e == null)
                {
                    logger.Add($"Processing response from {Url} failed with {se}.");
                    Failed?.Invoke(se);
                    Completed = true;
                    Aborted = true;
                    throw;
                }
            }

            if (e == null)
            {
                Finished?.Invoke();
                Completed = true;
            }
            else
            {
                Failed?.Invoke(e);
                Completed = true;
                Aborted = true;
                throw e;
            }
        }

        /// <summary>
        /// Performs any post-processing of the response.
        /// Exceptions thrown in this method will be passed to <see cref="Failed"/>.
        /// </summary>
        protected virtual void ProcessResponse()
        {
        }

        /// <summary>
        /// Forcefully abort the request.
        /// </summary>
        public void Abort()
        {
            if (Aborted || Completed) return;

            Aborted = true;
            Completed = true;

            try
            {
                abortToken?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// Adds a raw POST body to this request.
        /// This may not be used in conjunction with <see cref="AddFile"/> and <see cref="AddParameter"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        public void AddRaw(string text)
        {
            AddRaw(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Adds a raw POST body to this request.
        /// This may not be used in conjunction with <see cref="AddFile"/> and <see cref="AddParameter"/>.
        /// </summary>
        /// <param name="bytes">The raw data.</param>
        public void AddRaw(byte[] bytes)
        {
            AddRaw(new MemoryStream(bytes));
        }

        /// <summary>
        /// Adds a raw POST body to this request.
        /// This may not be used in conjunction with <see cref="AddFile"/> and <see cref="AddParameter"/>.
        /// </summary>
        /// <param name="stream">The stream containing the raw data. This stream will _not_ be finalized by this request.</param>
        public void AddRaw(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            rawContent ??= new MemoryStream();

            stream.CopyTo(rawContent);
        }

        /// <summary>
        /// Add a new FILE parameter to this request. Replaces any existing file with the same name.
        /// This may not be used in conjunction with <see cref="AddRaw(Stream)"/>. GET requests may not contain files.
        /// </summary>
        /// <param name="name">The name of the file. This becomes the name of the file in a multi-part form POST content.</param>
        /// <param name="data">The file data.</param>
        public void AddFile(string name, byte[] data)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (data == null) throw new ArgumentNullException(nameof(data));

            files[name] = data;
        }

        /// <summary>
        /// Add a new POST parameter to this request. Replaces any existing parameter with the same name.
        /// This may not be used in conjunction with <see cref="AddRaw(Stream)"/>.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The parameter value.</param>
        public void AddParameter(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            parameters[name] = value;
        }

        /// <summary>
        /// Adds a new header to this request. Replaces any existing header with the same name.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <param name="value">The header value.</param>
        public void AddHeader(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            headers[name] = value;
        }

        #region Timeout Handling

        private long lastAction;

        private long timeSinceLastAction => (DateTime.Now.Ticks - lastAction) / TimeSpan.TicksPerMillisecond;

        private void reportForwardProgress()
        {
            lastAction = DateTime.Now.Ticks;
            timeoutToken.CancelAfter(Timeout);
        }

        #endregion

        #region IDisposable Support

        private bool isDisposed;

        protected void Dispose(bool disposing)
        {
            if (isDisposed) return;

            isDisposed = true;

            Abort();

            requestStream?.Dispose();
            response?.Dispose();

            if (!(ResponseStream is MemoryStream))
                ResponseStream?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IPv4 fallback implementation

#if NET5_0
        /// <summary>
        /// Whether IPv6 should be preferred. Value may change based on runtime failures.
        /// </summary>
        private static bool useIPv6 = Socket.OSSupportsIPv6;

        /// <summary>
        /// Whether the initial IPv6 check has been performed (to determine whether v6 is available or not).
        /// </summary>
        private static bool hasResolvedIPv6Availability;

        private const int connection_establish_timeout = 2000;

        private static async ValueTask<Stream> onConnect(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            // Until .NET supports an implementation of Happy Eyeballs (https://tools.ietf.org/html/rfc8305#section-2), let's make IPv4 fallback work in a simple way.
            // This issue is being tracked at https://github.com/dotnet/runtime/issues/26177 and expected to be fixed in .NET 6.

            if (useIPv6)
            {
                try
                {
                    var localToken = cancellationToken;

                    if (!hasResolvedIPv6Availability)
                    {
                        // to make things move fast, use a very low timeout for the initial ipv6 attempt.
                        var quickFailCts = new CancellationTokenSource(connection_establish_timeout);
                        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, quickFailCts.Token);

                        localToken = linkedTokenSource.Token;
                    }

                    return await attemptConnection(AddressFamily.InterNetworkV6, context, localToken);
                }
                catch
                {
                    // very naively fallback to ipv4 permanently for this execution based on the response of the first connection attempt.
                    // note that this may cause users to eventually get switched to ipv4 (on a random failure when they are switching networks, for instance)
                    // but in the interest of keeping this implementation simple, this is acceptable.
                    useIPv6 = false;
                }
                finally
                {
                    hasResolvedIPv6Availability = true;
                }
            }

            // fallback to IPv4.
            return await attemptConnection(AddressFamily.InterNetwork, context, cancellationToken);
        }

        private static async ValueTask<Stream> attemptConnection(AddressFamily addressFamily, SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            // The following socket constructor will create a dual-mode socket on systems where IPV6 is available.
            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                // Turn off Nagle's algorithm since it degrades performance in most HttpClient scenarios.
                NoDelay = true
            };

            try
            {
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                // The stream should take the ownership of the underlying socket,
                // closing it when it's disposed.
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
#endif

        #endregion

        private class LengthTrackingStream : Stream
        {
            public readonly BindableLong BytesRead = new BindableLong();

            private readonly Stream baseStream;

            public LengthTrackingStream(Stream baseStream)
            {
                this.baseStream = baseStream;
            }

            public override void Flush()
            {
                baseStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int read = baseStream.Read(buffer, offset, count);
                BytesRead.Value += read;
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin) => baseStream.Seek(offset, origin);

            public override void SetLength(long value)
            {
                baseStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                baseStream.Write(buffer, offset, count);
            }

            public override bool CanRead => baseStream.CanRead;
            public override bool CanSeek => baseStream.CanSeek;
            public override bool CanWrite => baseStream.CanWrite;
            public override long Length => baseStream.Length;

            public override long Position
            {
                get => baseStream.Position;
                set => baseStream.Position = value;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                baseStream.Dispose();
            }
        }
    }
}
