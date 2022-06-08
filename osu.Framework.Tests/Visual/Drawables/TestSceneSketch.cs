// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneSketch : FrameworkTestScene
    {
        [SetUp]
        public void SetUp() => Schedule(() => Child = new AudioLatencyTester());
    }
}
