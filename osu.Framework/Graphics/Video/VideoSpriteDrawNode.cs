// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Video
{
    internal class VideoSpriteDrawNode : SpriteDrawNode
    {
        private readonly Video video;

        public VideoSpriteDrawNode(Video source)
            : base(source.Sprite)
        {
            video = source;
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            var yuvCoeff = video.ConversionMatrix;
            Shader.GetUniform<Matrix3>("yuvCoeff").UpdateValue(ref yuvCoeff);

            base.Draw(vertexAction);
        }
    }
}
