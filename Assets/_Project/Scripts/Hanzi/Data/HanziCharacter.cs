using System;
using System.Collections.Generic;

namespace HanziZombieDefense.Hanzi.Data
{
    /// <summary>
    /// Immutable description of a single Chinese character: the glyph itself
    /// and the ordered list of strokes that compose it. Sourced from the
    /// Make Me a Hanzi / HanziWriter dataset (medians arrays only).
    /// </summary>
    public sealed class HanziCharacter
    {
        /// <summary>The character glyph (one or more code points, typically a single CJK ideograph).</summary>
        public string Character { get; }

        /// <summary>
        /// Ordered list of strokes. Index 0 is the first stroke to draw.
        /// Each stroke is itself a polyline in normalized space.
        /// </summary>
        public IReadOnlyList<HanziStroke> Strokes { get; }

        /// <summary>Total number of strokes — equivalent to <see cref="Strokes"/>.Count.</summary>
        public int StrokeCount => Strokes.Count;

        /// <summary>
        /// Construct a character from its glyph and an ordered stroke list.
        /// </summary>
        /// <param name="character">Character glyph string (e.g. "你"). Must be non-empty.</param>
        /// <param name="strokes">Ordered stroke list. Must be non-null and non-empty.</param>
        public HanziCharacter(string character, List<HanziStroke> strokes)
        {
            if (string.IsNullOrEmpty(character))
                throw new ArgumentException("Character must be non-empty.", nameof(character));
            if (strokes == null)
                throw new ArgumentNullException(nameof(strokes));
            if (strokes.Count == 0)
                throw new ArgumentException("Character must have at least one stroke.", nameof(strokes));

            Character = character;
            // Defensive copy → the public list is read-only and decoupled from caller mutations.
            Strokes = strokes.AsReadOnly();
        }

        public override string ToString() => $"{Character} ({StrokeCount} strokes)";
    }
}
