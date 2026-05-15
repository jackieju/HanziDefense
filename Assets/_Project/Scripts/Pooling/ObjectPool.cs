using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.Pooling
{
    /// <summary>
    /// Optional lifecycle hooks for pooled objects. Implement on pooled MonoBehaviours
    /// to receive spawn/despawn callbacks at the right moment in the pool's flow.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called after the GameObject is reactivated, before <see cref="ObjectPool{T}.Get"/> returns.</summary>
        void OnSpawn();

        /// <summary>Called before the GameObject is deactivated, at the start of <see cref="ObjectPool{T}.Return"/>.</summary>
        void OnDespawn();
    }

    /// <summary>
    /// Generic component pool. Instantiates copies of <typeparamref name="T"/>'s prefab,
    /// keeps them parented under a transform, and recycles them via <see cref="Get"/> /
    /// <see cref="Return"/>. Pool grows on demand if exhausted.
    /// </summary>
    /// <typeparam name="T">Component type on the pooled prefab.</typeparam>
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _available;
        private readonly HashSet<T> _inUse;

        /// <summary>Number of instances currently checked out from the pool.</summary>
        public int ActiveCount => _inUse.Count;

        /// <summary>Number of instances sitting idle in the pool.</summary>
        public int InactiveCount => _available.Count;

        /// <summary>Total instances created by this pool (active + inactive).</summary>
        public int TotalCount => ActiveCount + InactiveCount;

        /// <summary>
        /// Construct the pool and pre-warm with <paramref name="initialSize"/> instances.
        /// </summary>
        /// <param name="prefab">Prefab to clone. Must be non-null and have component <typeparamref name="T"/>.</param>
        /// <param name="parent">Parent transform for pooled instances; may be null.</param>
        /// <param name="initialSize">Number of instances to pre-instantiate. Negative values clamp to 0.</param>
        public ObjectPool(T prefab, Transform parent, int initialSize)
        {
            if (prefab == null)
            {
                throw new System.ArgumentNullException(nameof(prefab));
            }

            _prefab = prefab;
            _parent = parent;
            _available = new Stack<T>(Mathf.Max(initialSize, 4));
            _inUse = new HashSet<T>();

            for (int i = 0; i < Mathf.Max(initialSize, 0); i++)
            {
                var instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _available.Push(instance);
            }
        }

        /// <summary>
        /// Retrieve an instance from the pool. Activates the GameObject and invokes
        /// <see cref="IPoolable.OnSpawn"/> on every <see cref="IPoolable"/> on the instance.
        /// Grows the pool if no instances are idle.
        /// </summary>
        public T Get()
        {
            T instance = _available.Count > 0 ? _available.Pop() : CreateInstance();

            instance.gameObject.SetActive(true);
            _inUse.Add(instance);

            var hooks = instance.GetComponents<IPoolable>();
            for (int i = 0; i < hooks.Length; i++)
            {
                hooks[i].OnSpawn();
            }

            return instance;
        }

        /// <summary>
        /// Return <paramref name="instance"/> to the pool. Invokes <see cref="IPoolable.OnDespawn"/>
        /// then deactivates the GameObject. Safe to call with null or a foreign instance (logged + ignored).
        /// </summary>
        public void Return(T instance)
        {
            if (instance == null) return;

            if (!_inUse.Remove(instance))
            {
                Debug.LogWarning(
                    $"[ObjectPool<{typeof(T).Name}>] Returned instance was not active in this pool.");
                return;
            }

            var hooks = instance.GetComponents<IPoolable>();
            for (int i = 0; i < hooks.Length; i++)
            {
                hooks[i].OnDespawn();
            }

            instance.gameObject.SetActive(false);
            if (_parent != null && instance.transform.parent != _parent)
            {
                instance.transform.SetParent(_parent, false);
            }
            _available.Push(instance);
        }

        /// <summary>
        /// Forcibly return every active instance to the pool. Useful for level resets.
        /// </summary>
        public void ReturnAll()
        {
            var snapshot = new List<T>(_inUse);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Return(snapshot[i]);
            }
        }

        /// <summary>Destroy all idle instances. Active instances are left alone.</summary>
        public void Trim()
        {
            while (_available.Count > 0)
            {
                var instance = _available.Pop();
                if (instance != null)
                {
                    Object.Destroy(instance.gameObject);
                }
            }
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(_prefab, _parent);
            instance.name = _prefab.name;
            return instance;
        }
    }
}
