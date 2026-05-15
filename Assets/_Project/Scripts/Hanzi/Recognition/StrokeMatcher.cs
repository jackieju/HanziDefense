using System.Collections.Generic;
using UnityEngine;
using HanziZombieDefense.Hanzi.Data;

namespace HanziZombieDefense.Hanzi.Recognition
{
    /// <summary>
    /// Strategy interface for stroke matchers. Implementations compare a single
    /// player-drawn polyline against the expected (template) stroke from a
    /// <see cref="HanziCharacter"/> and report a structured score.
    /// </summary>
    public interface IStrokeMatcher
    {
        /// <summary>
        /// Compare <paramref name="drawnPoints"/> against <paramref name="expected"/>
        /// and return the recognition outcome. Implementations MUST be pure /
        /// thread-safe with respect to their own state.
        /// </summary>
        RecognitionResult Match(List<Vector2> drawnPoints, HanziStroke expected);
    }
}
