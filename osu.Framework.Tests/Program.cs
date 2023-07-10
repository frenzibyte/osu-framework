// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Platform;
using osu.Framework.Platform.MacOS.Native;

namespace osu.Framework.Tests
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            bool benchmark = args.Contains(@"--benchmark");
            bool portable = args.Contains(@"--portable");

            using (GameHost host = Host.GetSuitableDesktopHost(@"visual-tests", new HostOptions { PortableInstallation = portable }))
            {
                if (benchmark)
                    host.Run(new AutomatedVisualTestGame());
                else
                    host.Run(new VisualTestGame());
            }
        }
    }
}

/*

#define os_signpost_interval_begin(log, interval_id, name, ...) \
        os_signpost_emit_with_type(log, OS_SIGNPOST_INTERVAL_BEGIN, interval_id, name, ##__VA_ARGS__)

#define os_signpost_emit_with_type(log, type, spid, name, ...) \
        _os_signpost_emit_with_type(_os_signpost_emit_with_name_impl, log, type, spid, name, ##__VA_ARGS__)

_os_signpost_emit_with_type(_os_signpost_emit_with_name_impl, log, OS_SIGNPOST_INTERVAL_BEGIN, interval_id, name, ##__VA_ARGS__)

#define _os_signpost_emit_with_type(emitfn, log, type, spid, name, ...) \
    __extension__({ \
        os_log_t _log_tmp = (log); \
        os_signpost_type_t _type_tmp = (type); \
        os_signpost_id_t _spid_tmp = (spid); \
        if (_spid_tmp != OS_SIGNPOST_ID_NULL && \
                _spid_tmp != OS_SIGNPOST_ID_INVALID && \
                os_signpost_enabled(_log_tmp)) { \
            OS_LOG_CALL_WITH_FORMAT_NAME((emitfn), \
                    (&__dso_handle, _log_tmp, _type_tmp, _spid_tmp), \
                    name, "" __VA_ARGS__); \
        } \
    })

    if (spid != OS_SIGNPOST_ID_NULL && spid != OS_SIGNPOST_ID_INVALID && os_signpost_enabled(log))
        OS_LOG_CALL_WITH_FORMAT_NAME(_os_signpost_emit_with_name_impl, (&__dso_handle, log, OS_SIGNPOST_INTERVAL_BEGIN, interval_id), name, "" __VA_ARGS__);

#define OS_LOG_CALL_WITH_FORMAT_NAME(fun, fun_args, name, fmt, ...) __extension__({ \
        OS_LOG_PRAGMA_PUSH OS_LOG_STRING(LOG, _os_fmt_str, fmt); \
        OS_LOG_STRING(LOG, _os_name_str, name); \
        uint8_t _Alignas(16) OS_LOG_UNINITIALIZED _os_fmt_buf[__builtin_os_log_format_buffer_size(fmt, ##__VA_ARGS__)]; \
        fun(OS_LOG_REMOVE_PARENS fun_args, _os_name_str, _os_fmt_str, \
                (uint8_t *)__builtin_os_log_format(_os_fmt_buf, fmt, ##__VA_ARGS__), \
                (uint32_t)sizeof(_os_fmt_buf)) OS_LOG_PRAGMA_POP; \
})

OS_LOG_PRAGMA_PUSH OS_LOG_STRING(LOG, _os_fmt_str, "");
OS_LOG_STRING(LOG, _os_name_str, name);
uint8_t _Alignas(16) OS_LOG_UNINITIALIZED _os_fmt_buf[__builtin_os_log_format_buffer_size("", ##__VA_ARGS__)];
_os_signpost_emit_with_name_impl(&__dso_handle, log, OS_SIGNPOST_INTERVAL_BEGIN, interval_id, _os_name_str, _os_fmt_str, (uint8_t *)__builtin_os_log_format(_os_fmt_buf, "", ##__VA_ARGS__), (uint32_t)sizeof(_os_fmt_buf)) OS_LOG_PRAGMA_POP;

_os_signpost_emit_with_name_impl(void *dso, os_log_t log,
        os_signpost_type_t type, os_signpost_id_t spid, const char *name,
        const char *format, uint8_t *buf, uint32_t size);

*/
