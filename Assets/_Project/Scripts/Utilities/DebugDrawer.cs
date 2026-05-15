using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.Utilities
{
    /// <summary>
    /// Centralised gizmo renderer. Other systems push <see cref="Drawable"/> records here
    /// and a single MonoBehaviour in the scene draws them all via <c>OnDrawGizmos</c>.
    /// Active in the editor and in development builds; compiles to a no-op in release players.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class DebugDrawer : MonoBehaviour
    {
        /// <summary>Primitive shapes the drawer can render.</summary>
        public enum Shape
        {
            Sphere,
            WireSphere,
            Cube,
            WireCube,
            Line,
            Ray
        }

        /// <summary>A single registered gizmo entry. Optional <see cref="ttl"/> auto-expires the entry.</summary>
        public struct Drawable
        {
            public Shape shape;
            public Vector3 position;
            public Vector3 secondary;
            public float size;
            public Color color;
            public float ttl;
            public string label;
        }

        private static readonly List<Drawable> _items = new List<Drawable>();
        private static DebugDrawer _instance;

        [Tooltip("Master toggle for gizmo rendering.")]
        [SerializeField] private bool drawEnabled = true;

        [Tooltip("Render labels next to each drawable in the scene view.")]
        [SerializeField] private bool drawLabels = true;

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnDisable()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>Register a sphere gizmo at <paramref name="position"/>.</summary>
        public static void DrawSphere(Vector3 position, float radius, Color color, float ttl = 0f, string label = null)
        {
            Add(new Drawable
            {
                shape = Shape.Sphere,
                position = position,
                size = radius,
                color = color,
                ttl = ttl,
                label = label
            });
        }

        /// <summary>Register a wire sphere gizmo at <paramref name="position"/>.</summary>
        public static void DrawWireSphere(Vector3 position, float radius, Color color, float ttl = 0f, string label = null)
        {
            Add(new Drawable
            {
                shape = Shape.WireSphere,
                position = position,
                size = radius,
                color = color,
                ttl = ttl,
                label = label
            });
        }

        /// <summary>Register a wire cube gizmo at <paramref name="center"/> with side length <paramref name="size"/>.</summary>
        public static void DrawWireCube(Vector3 center, float size, Color color, float ttl = 0f, string label = null)
        {
            Add(new Drawable
            {
                shape = Shape.WireCube,
                position = center,
                size = size,
                color = color,
                ttl = ttl,
                label = label
            });
        }

        /// <summary>Register a line gizmo from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static void DrawLine(Vector3 from, Vector3 to, Color color, float ttl = 0f, string label = null)
        {
            Add(new Drawable
            {
                shape = Shape.Line,
                position = from,
                secondary = to,
                color = color,
                ttl = ttl,
                label = label
            });
        }

        /// <summary>Register a polyline as a sequence of <see cref="Shape.Line"/> drawables.</summary>
        public static void DrawPath(IList<Vector3> path, Color color, float ttl = 0f, string label = null)
        {
            if (path == null || path.Count < 2) return;
            for (int i = 1; i < path.Count; i++)
            {
                DrawLine(path[i - 1], path[i], color, ttl, i == 1 ? label : null);
            }
        }

        /// <summary>Register a ray gizmo originating at <paramref name="origin"/>.</summary>
        public static void DrawRay(Vector3 origin, Vector3 direction, Color color, float ttl = 0f, string label = null)
        {
            Add(new Drawable
            {
                shape = Shape.Ray,
                position = origin,
                secondary = direction,
                color = color,
                ttl = ttl,
                label = label
            });
        }

        /// <summary>Drop every registered drawable.</summary>
        public static void Clear()
        {
            _items.Clear();
        }

        /// <summary>Number of currently registered drawables.</summary>
        public static int Count => _items.Count;

        private static void Add(Drawable item)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _items.Add(item);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (_items.Count == 0) return;
            float dt = Application.isPlaying ? Time.deltaTime : 0f;

            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item.ttl <= 0f) continue;

                item.ttl -= dt;
                if (item.ttl <= 0f)
                {
                    _items.RemoveAt(i);
                }
                else
                {
                    _items[i] = item;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawEnabled) return;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                Gizmos.color = item.color;

                switch (item.shape)
                {
                    case Shape.Sphere:
                        Gizmos.DrawSphere(item.position, item.size);
                        break;
                    case Shape.WireSphere:
                        Gizmos.DrawWireSphere(item.position, item.size);
                        break;
                    case Shape.Cube:
                        Gizmos.DrawCube(item.position, Vector3.one * item.size);
                        break;
                    case Shape.WireCube:
                        Gizmos.DrawWireCube(item.position, Vector3.one * item.size);
                        break;
                    case Shape.Line:
                        Gizmos.DrawLine(item.position, item.secondary);
                        break;
                    case Shape.Ray:
                        Gizmos.DrawRay(item.position, item.secondary);
                        break;
                }

#if UNITY_EDITOR
                if (drawLabels && !string.IsNullOrEmpty(item.label))
                {
                    UnityEditor.Handles.color = item.color;
                    UnityEditor.Handles.Label(item.position, item.label);
                }
#endif
            }
        }
#endif
    }
}
