using UnityEngine;

namespace HanziZombieDefense.ScriptableObjects
{
    /// <summary>
    /// Authoring asset describing a zombie archetype. Spawners read this asset to
    /// configure stats and pick the right prefab to instantiate / pool.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ZombieDefinition",
        menuName = "HanziZombieDefense/Zombie Definition",
        order = 100)]
    public sealed class ZombieDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Human-readable name shown in editors / debug overlays.")]
        private string zombieName = "Zombie";

        [Header("Stats")]
        [SerializeField, Min(0f), Tooltip("Base movement speed in m/s before difficulty multipliers.")]
        private float baseSpeed = 3f;

        [SerializeField, Min(1f), Tooltip("Base hit points before difficulty modifiers.")]
        private float baseHP = 10f;

        [SerializeField, Min(0f), Tooltip("Damage dealt to the player on contact / attack hit.")]
        private float contactDamage = 10f;

        [SerializeField, Min(0), Tooltip("Score awarded when this zombie is killed (pre-combo).")]
        private int scoreValue = 100;

        [Header("Prefab")]
        [SerializeField, Tooltip("Prefab to instantiate. Must contain a Zombie component on the root.")]
        private GameObject prefab;

        public string ZombieName => zombieName;
        public float BaseSpeed => baseSpeed;
        public float BaseHP => baseHP;
        public float ContactDamage => contactDamage;
        public int ScoreValue => scoreValue;
        public GameObject Prefab => prefab;
    }
}
