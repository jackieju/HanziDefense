using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.Core
{
    /// <summary>
    /// Lightweight type-keyed registry for non-MonoBehaviour services.
    /// Alternative to singletons; favored for plain C# systems (audio, scoring, difficulty…).
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services
            = new Dictionary<Type, object>();

        /// <summary>Register <paramref name="instance"/> as the implementation of <typeparamref name="T"/>.</summary>
        /// <exception cref="ArgumentNullException">Thrown when instance is null.</exception>
        public static void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var key = typeof(T);
            if (_services.ContainsKey(key))
            {
                Debug.LogWarning($"[ServiceLocator] Overwriting existing service of type {key.Name}.");
            }
            _services[key] = instance;
        }

        /// <summary>Resolve the registered service of type <typeparamref name="T"/>.</summary>
        /// <exception cref="InvalidOperationException">Thrown when no service is registered.</exception>
        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var svc))
            {
                return (T)svc;
            }
            throw new InvalidOperationException(
                $"[ServiceLocator] No service registered for type {typeof(T).Name}.");
        }

        /// <summary>Try-resolve variant that returns false instead of throwing.</summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var svc))
            {
                service = (T)svc;
                return true;
            }
            service = null;
            return false;
        }

        /// <summary>Returns true if a service of type <typeparamref name="T"/> is registered.</summary>
        public static bool Has<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>Remove the service of type <typeparamref name="T"/>. No-op if not present.</summary>
        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        /// <summary>Drop every registered service. Use on scene teardown / domain reload.</summary>
        public static void Clear()
        {
            _services.Clear();
        }
    }
}
