using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.ScriptableObjects
{
    /// <summary>Authoring asset listing a curated subset of hanzi (e.g. HSK1 vocabulary).</summary>
    [CreateAssetMenu(
        fileName = "HanziSetDefinition",
        menuName = "HanziZombieDefense/Hanzi Set Definition",
        order = 120)]
    public sealed class HanziSetDefinition : ScriptableObject
    {
        [SerializeField] private string setName = "HSK1";
        [SerializeField, Min(0)] private int hskLevel = 1;
        [SerializeField, Tooltip("Inclusive maximum stroke count for characters drawn from this set.")]
        private int maxStrokeCount = 8;

        [SerializeField, Tooltip("Hanzi glyph strings — one character per element.")]
        private List<string> characters = new List<string>();

        public string SetName => setName;
        public int HskLevel => hskLevel;
        public int MaxStrokeCount => maxStrokeCount;
        public IReadOnlyList<string> Characters => characters;
        public int Count => characters.Count;

        /// <summary>True when <paramref name="character"/> is listed in this set.</summary>
        public bool Contains(string character) => characters.Contains(character);
    }
}
