// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Veldrid.Vertices;

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
