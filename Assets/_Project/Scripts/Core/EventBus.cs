using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.Core
{
    /// <summary>
    /// Strongly-typed publish/subscribe bus keyed by event struct type.
    /// Single-threaded (Unity main thread) — no locking.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers
            = new Dictionary<Type, List<Delegate>>();

        /// <summary>Register <paramref name="handler"/> for events of type <typeparamref name="T"/>.</summary>
        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var key = typeof(T);
            if (!_subscribers.TryGetValue(key, out var list))
            {
                list = new List<Delegate>(4);
                _subscribers[key] = list;
            }
            list.Add(handler);
        }

        /// <summary>Remove a previously registered <paramref name="handler"/>.</summary>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            if (!_subscribers.TryGetValue(typeof(T), out var list)) return;

            list.Remove(handler);
            if (list.Count == 0)
            {
                _subscribers.Remove(typeof(T));
            }
        }

        /// <summary>Synchronously dispatch <paramref name="evt"/> to all subscribers of <typeparamref name="T"/>.</summary>
        public static void Publish<T>(T evt)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var list) || list.Count == 0)
            {
                return;
            }

            var snapshot = new Delegate[list.Count];
            list.CopyTo(snapshot);

            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    ((Action<T>)snapshot[i])?.Invoke(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Handler for {typeof(T).Name} threw: {ex}");
                }
            }
        }

        /// <summary>Drop all subscribers. Useful for domain reload / play-mode teardown.</summary>
        public static void ClearAll()
        {
            _subscribers.Clear();
        }

        /// <summary>Number of registered handlers for <typeparamref name="T"/>.</summary>
        public static int SubscriberCount<T>()
        {
            return _subscribers.TryGetValue(typeof(T), out var list) ? list.Count : 0;
        }
    }
}
