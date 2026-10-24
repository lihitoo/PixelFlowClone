using System;
using System.Collections.Generic;
using Dreamy.Core.Pooling;
using Lean.Pool;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dreamy.Template.Pooling
{
    public sealed class LeanPoolService : IPoolService, IDisposable
    {
        private const string PoolRootName = "[PoolService]";
        private readonly Dictionary<GameObject, LeanGameObjectPool> pools = new();
        private readonly Dictionary<GameObject, LeanGameObjectPool> owners = new();
        private readonly List<GameObject> activeClones = new();
        private readonly Transform poolRoot;
        private bool disposed;

        public LeanPoolService()
        {
            GameObject root = new(PoolRootName);
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(root);
            }
            poolRoot = root.transform;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            => Spawn(prefab, PoolSpawnOptions.World(position, rotation, parent));

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
            => PoolServiceExtensions.Spawn(this, prefab, PoolSpawnOptions.World(position, rotation, parent));

        public GameObject Spawn(GameObject prefab, Transform parent)
            => Spawn(prefab, new PoolSpawnOptions { Space = Space.Self, Parent = parent });

        public T Spawn<T>(T prefab, Transform parent) where T : Component
            => PoolServiceExtensions.Spawn(this, prefab, new PoolSpawnOptions { Space = Space.Self, Parent = parent });

        public T Spawn<T>(GameObject prefab, Transform parent) where T : Component
            => PoolServiceExtensions.Spawn<T>(this, prefab, new PoolSpawnOptions { Space = Space.Self, Parent = parent });

        public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
            => PoolServiceExtensions.Spawn<T>(this, prefab, PoolSpawnOptions.World(position, rotation, parent));

        public GameObject Spawn(GameObject prefab, PoolSpawnOptions options)
        {
            TrySpawn(prefab, options, out GameObject instance);
            return instance;
        }

        public bool TrySpawn(GameObject prefab, PoolSpawnOptions options, out GameObject instance)
        {
            ThrowIfDisposed();
            if (!prefab) throw new ArgumentNullException(nameof(prefab));
            if (options.Space != Space.World && options.Space != Space.Self)
                throw new ArgumentOutOfRangeException(nameof(options), "Space must be World or Self.");

            Transform source = prefab.transform;
            Vector3 position = options.Position ?? (options.Space == Space.Self ? source.localPosition : source.position);
            Quaternion rotation = options.Rotation ?? (options.Space == Space.Self ? source.localRotation : source.rotation);
            if (options.Space == Space.World && options.Parent)
            {
                position = options.Parent.InverseTransformPoint(position);
                rotation = Quaternion.Inverse(options.Parent.rotation) * rotation;
            }

            LeanGameObjectPool pool = GetOrCreatePool(prefab);
            instance = null;
            if (!pool.TrySpawn(ref instance, position, rotation, options.LocalScale ?? source.localScale, options.Parent, false))
            {
                instance = null;
                return false;
            }

            owners[instance] = pool;
            try
            {
                instance.name = options.Name ?? prefab.name;
                options.Configure?.Invoke(instance);
                return true;
            }
            catch
            {
                // Return the clone (and cancel any scheduled return) before propagating configuration errors.
                Despawn(instance);
                instance = null;
                throw;
            }
        }

        public void Despawn(GameObject instance, float delay = 0f)
        {
            if (!instance) return;
            ThrowIfDisposed();
            if (!owners.TryGetValue(instance, out LeanGameObjectPool pool) || !pool)
                throw new ArgumentException("Instance was not spawned by this pool service.", nameof(instance));

            // Delayed returns run in Lean's scaled-time Update, so query the backend's current state.
            pool.GetClones(activeClones, addSpawnedClones: true, addDespawnedClones: false);
            bool active = activeClones.Contains(instance);
            activeClones.Clear();
            if (active) pool.Despawn(instance, delay);
        }

        public void Preload(GameObject prefab, int count, int capacity = 0, bool persistent = false)
        {
            ThrowIfDisposed();
            if (!prefab) throw new ArgumentNullException(nameof(prefab));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (capacity > 0 && count > capacity)
                throw new ArgumentOutOfRangeException(nameof(count), "Preload count cannot exceed capacity.");

            LeanGameObjectPool pool = GetOrCreatePool(prefab);
            pool.Persist = persistent;
            // Persist is consumed only in Lean's Awake; explicitly move existing pools as well.
            pool.transform.SetParent(persistent ? poolRoot : null, false);
            if (!persistent && pool.gameObject.scene != SceneManager.GetActiveScene())
                SceneManager.MoveGameObjectToScene(pool.gameObject, SceneManager.GetActiveScene());
            pool.Preload = count;
            pool.Capacity = capacity;
            pool.PreloadAll();
        }

        public void DespawnAll()
        {
            ThrowIfDisposed();
            foreach (LeanGameObjectPool pool in pools.Values)
            {
                if (pool) pool.DespawnAll(false);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            DespawnAll();
            disposed = true;
            foreach (LeanGameObjectPool pool in pools.Values)
            {
                if (pool) DestroyOwned(pool.gameObject);
            }
            if (poolRoot) DestroyOwned(poolRoot.gameObject);
            pools.Clear();
            owners.Clear();
            activeClones.Clear();
        }

        private LeanGameObjectPool GetOrCreatePool(GameObject prefab)
        {
            if (pools.TryGetValue(prefab, out LeanGameObjectPool pool) && pool) return pool;
            GameObject poolObject = new($"Pool - {prefab.name}");
            pool = poolObject.AddComponent<LeanGameObjectPool>();
            pool.Prefab = prefab;
            pool.Notification = LeanGameObjectPool.NotificationType.IPoolable;
            pool.Recycle = false;
            pools[prefab] = pool;
            return pool;
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(LeanPoolService));
        }

        private static void DestroyOwned(GameObject instance)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(instance);
            else UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
