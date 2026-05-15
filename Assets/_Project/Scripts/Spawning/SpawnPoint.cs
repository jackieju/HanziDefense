using UnityEngine;

namespace HanziZombieDefense.Spawning
{
    /// <summary>
    /// Discrete lane in front of the player. Used by <see cref="SpawnPoint"/>
    /// and <see cref="ZombieSpawner"/> to arrange zombies in a Left / Center /
    /// Right column for the fixed-position mobile build.
    /// </summary>
    public enum SpawnLane
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    /// <summary>
    /// Marker placed in the level. Each spawn point is associated with a
    /// <see cref="SpawnLane"/> (Left, Center, Right) so the wave director can
    /// arrange zombies in lanes in front of the static camera.
    /// <see cref="GetSpawnPosition"/> returns this transform's world position
    /// offset by a random vector inside a disc of radius
    /// <see cref="spawnRadius"/> on the XZ plane.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField, Tooltip("Which lane this spawn point belongs to.")]
        private SpawnLane lane = SpawnLane.Center;

        [SerializeField, Min(0f), Tooltip("Random horizontal jitter applied to spawn positions, in meters.")]
        private float spawnRadius = 1.5f;

        public SpawnLane Lane => lane;
        public float SpawnRadius => spawnRadius;

        /// <summary>World position with random XZ offset within <see cref="spawnRadius"/>.</summary>
        public Vector3 GetSpawnPosition()
        {
            if (spawnRadius <= 0f) return transform.position;

            Vector2 disc = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(disc.x, 0f, disc.y);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = LaneColor();
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.25f, spawnRadius));
            Gizmos.color = LaneColor() * new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.2f);
        }

        private Color LaneColor()
        {
            switch (lane)
            {
                case SpawnLane.Left:   return new Color(0.3f, 0.6f, 1f, 0.85f);
                case SpawnLane.Right:  return new Color(1f, 0.6f, 0.3f, 0.85f);
                default:               return new Color(1f, 0.2f, 0.2f, 0.85f);
            }
        }
#endif
    }
}
