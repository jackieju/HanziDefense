using HanziZombieDefense.Hanzi.Data;
using HanziZombieDefense.Zombies;

namespace HanziZombieDefense.Core
{
    /// <summary>
    /// Container namespace for all event payload structs dispatched through <see cref="EventBus"/>.
    /// Structs are used to avoid GC allocation per publish.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>Fired whenever <see cref="GameManager.CurrentState"/> transitions.</summary>
        public struct GameStateChanged
        {
            public GameState previousState;
            public GameState newState;
        }

        /// <summary>A zombie has entered the world (post-spawn, post-pool-init).</summary>
        public struct ZombieSpawned
        {
            public Zombie zombie;
        }

        public struct ZombieKilled
        {
            public Zombie Zombie;
            public int scoreAwarded;
            public HanziCharacter Character;
            public UnityEngine.Vector3 Position;
        }

        /// <summary>Player took damage.</summary>
        public struct PlayerDamaged
        {
            public float Amount;
            public float CurrentHealth;
            public float MaxHealth;
            public float remainingHP;
        }

        /// <summary>Player HP reached zero.</summary>
        public struct PlayerDied { }

        /// <summary>Active targeting changed. <see cref="newTarget"/> may be null (target lost / cleared).</summary>
        public struct TargetChanged
        {
            public Zombie Previous;
            public Zombie Current;
            public Zombie newTarget;
        }

        /// <summary>A drawn stroke matched the expected stroke at <see cref="strokeIndex"/>.</summary>
        public struct StrokeAccepted
        {
            public int strokeIndex;
            public int totalStrokes;
        }

        /// <summary>A drawn stroke failed validation at <see cref="strokeIndex"/>.</summary>
        public struct StrokeRejected
        {
            public int strokeIndex;
        }

        /// <summary>All strokes for <see cref="character"/> on <see cref="zombie"/> completed successfully.</summary>
        public struct CharacterCompleted
        {
            public string character;
            public Zombie zombie;
        }

        /// <summary>A new wave has begun.</summary>
        public struct WaveStarted
        {
            public int waveNumber;
        }

        /// <summary>Wave cleared (last zombie of the wave killed).</summary>
        public struct WaveCompleted
        {
            public int waveNumber;
        }
    }
}
