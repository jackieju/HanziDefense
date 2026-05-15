using UnityEngine;

namespace HanziZombieDefense.Player
{
    /// <summary>
    /// Static "player rig" marker for the mobile, fixed-position wave-defense build.
    /// The player no longer moves or looks around — the camera stays put and zombies
    /// approach from the front. This component simply exposes a Camera reference so
    /// other systems (targeting, FX, audio) have a stable rig anchor.
    ///
    /// The class is intentionally named <c>PlayerController</c> so existing scene
    /// references (prefabs, inspectors, ServiceLocator lookups) continue to resolve;
    /// the legacy FPS movement/look behaviour has been removed.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField, Tooltip("Camera used for the static first-person view. If unset, the first child camera is used at Awake.")]
        private Camera playerCamera;

        /// <summary>The camera attached to this rig. May be null if none is configured.</summary>
        public Camera PlayerCamera => playerCamera;

        /// <summary>Convenience accessor for the camera transform.</summary>
        public Transform CameraTransform => playerCamera != null ? playerCamera.transform : null;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }
    }
}
