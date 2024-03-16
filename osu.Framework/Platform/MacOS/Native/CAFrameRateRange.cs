// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct CAFrameRateRange
    {
        public float Maximum { get; }
        public float Minimum { get; }
        public float Preferred { get; }

        public CAFrameRateRange(float maximum, float minimum, float preferred)
        {
            Maximum = maximum;
            Minimum = minimum;
            Preferred = preferred;
        }
    }
}
