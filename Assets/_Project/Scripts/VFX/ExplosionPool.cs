using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.VFX
{
    /// <summary>
    /// Pool of explosion <see cref="ParticleSystem"/> instances. Caller invokes
    /// <see cref="SpawnExplosion"/>; the system measures the prefab's effective
    /// lifetime and auto-returns the instance to the pool when the burst is done.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExplosionPool : MonoBehaviour
    {
        [SerializeField, Tooltip("Particle system prefab cloned to populate the pool.")]
        private ParticleSystem explosionPrefab;

        [SerializeField, Min(1)] private int poolSize = 20;

        [SerializeField, Tooltip("Fallback lifetime (seconds) when a particle system has 0 / infinite duration.")]
        private float fallbackLifetime = 2f;

        private readonly Stack<ParticleSystem> _idle = new Stack<ParticleSystem>();
        private readonly HashSet<ParticleSystem> _active = new HashSet<ParticleSystem>();

        public int ActiveCount => _active.Count;
        public int IdleCount => _idle.Count;

        private void Awake()
        {
            if (explosionPrefab == null)
            {
                Debug.LogError("[ExplosionPool] explosionPrefab is not assigned.");
                return;
            }

            for (int i = 0; i < poolSize; i++)
            {
                _idle.Push(CreateInstance());
            }

            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<ExplosionPool>(out var registered) && registered == this)
            {
                ServiceLocator.Unregister<ExplosionPool>();
            }
        }

        /// <summary>
        /// Spawn an explosion at <paramref name="position"/>, scaling the prefab by
        /// <paramref name="scale"/>. Returns the <see cref="ParticleSystem"/> in case
        /// the caller wants to attach extra effects.
        /// </summary>
        public ParticleSystem SpawnExplosion(Vector3 position, float scale = 1f)
        {
            ParticleSystem ps = Acquire();
            if (ps == null) return null;

            var t = ps.transform;
            t.position = position;
            t.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            ps.gameObject.SetActive(true);

            ps.Clear(true);
            ps.Play(true);

            float lifetime = MeasureLifetime(ps);
            StartCoroutine(ReturnAfter(ps, lifetime));
            return ps;
        }

        private ParticleSystem CreateInstance()
        {
            var instance = Instantiate(explosionPrefab, transform);
            instance.name = explosionPrefab.name;
            instance.gameObject.SetActive(false);
            return instance;
        }

        private ParticleSystem Acquire()
        {
            ParticleSystem ps = _idle.Count > 0 ? _idle.Pop() : CreateInstance();
            _active.Add(ps);
            return ps;
        }

        private void Return(ParticleSystem ps)
        {
            if (ps == null) return;
            if (!_active.Remove(ps)) return;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
            ps.transform.localScale = Vector3.one;
            _idle.Push(ps);
        }

        private float MeasureLifetime(ParticleSystem ps)
        {
            float maxLife = 0f;
            CollectLifetime(ps, ref maxLife);
            return maxLife > 0.05f ? maxLife : fallbackLifetime;
        }

        private static void CollectLifetime(ParticleSystem ps, ref float maxLife)
        {
            if (ps == null) return;

            var main = ps.main;
            float life = main.duration + main.startLifetime.constantMax;
            if (life > maxLife) maxLife = life;

            var children = ps.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == ps) continue;
                var cm = children[i].main;
                float cl = cm.duration + cm.startLifetime.constantMax;
                if (cl > maxLife) maxLife = cl;
            }
        }

        private IEnumerator ReturnAfter(ParticleSystem ps, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Return(ps);
        }
    }
}
