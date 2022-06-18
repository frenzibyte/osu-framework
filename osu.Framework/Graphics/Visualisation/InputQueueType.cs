// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Framework.Graphics.Visualisation
{
    public enum InputQueueType
    {
        [Description("Positional")]
        Positional,

        [Description("Non-positional")]
        NonPositional,
    }
}
