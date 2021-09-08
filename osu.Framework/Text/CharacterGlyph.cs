// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using osu.Framework.IO.Stores;

namespace osu.Framework.Text
{
    public sealed class CharacterGlyph : ICharacterGlyph
    {
        private readonly IGlyphStore containingStore;

        public float XOffset { get; }
        public float YOffset { get; }
        public float XAdvance { get; }
        public float Baseline { get; }
        public char Character { get; }

        public FontMetrics? Metrics => containingStore?.Metrics;

        public CharacterGlyph(char character, float xOffset, float yOffset, float xAdvance, float baseline, [CanBeNull] IGlyphStore containingStore)
        {
            this.containingStore = containingStore;

            Character = character;
            XOffset = xOffset;
            YOffset = yOffset;
            XAdvance = xAdvance;
            Baseline = baseline;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetKerning<T>(T lastGlyph)
            where T : ICharacterGlyph
            => containingStore?.GetKerning(lastGlyph.Character, Character) ?? 0;
    }
}
