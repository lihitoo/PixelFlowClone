using System;
using Dreamy.Core.Pooling;
using Dreamy.Template.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace Dreamy.Template.Tests
{
    public sealed class LeanPoolServiceTests
    {
        private LeanPoolService service;
        private GameObject prefab;
        private GameObject parent;

        [SetUp]
        public void SetUp()
        {
            service = new LeanPoolService();
            prefab = new GameObject("Pool test prefab");
            parent = new GameObject("Pool test parent");
            prefab.transform.localPosition = new Vector3(1, 2, 3);
            prefab.transform.localRotation = Quaternion.Euler(0, 30, 0);
            prefab.transform.localScale = new Vector3(2, 3, 4);
            parent.transform.position = new Vector3(10, 20, 30);
            parent.transform.rotation = Quaternion.Euler(0, 90, 0);
            parent.transform.localScale = Vector3.one * 2;
        }

        [TearDown]
        public void TearDown()
        {
            service.Dispose();
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(parent);
        }

        [Test]
        public void Spawn_ResetsPrefabDefaultsAndNameOnReuse()
        {
            var instance = service.Spawn(prefab, PoolSpawnOptions.Local(parent.transform, Vector3.zero,
                Quaternion.identity, Vector3.one));
            instance.name = "Changed";
            service.Despawn(instance);
            service.Despawn(instance, 1f);

            var reused = service.Spawn(prefab, new PoolSpawnOptions { Space = Space.Self, Parent = parent.transform });

            Assert.That(reused, Is.SameAs(instance));
            Assert.That(reused.name, Is.EqualTo(prefab.name));
            Assert.That(reused.transform.localPosition, Is.EqualTo(prefab.transform.localPosition));
            Assert.That(Quaternion.Angle(reused.transform.localRotation, prefab.transform.localRotation), Is.LessThan(0.001f));
            Assert.That(reused.transform.localScale, Is.EqualTo(prefab.transform.localScale));
        }

        [Test]
        public void Spawn_WorldOverridesRespectTransformedParent()
        {
            var position = new Vector3(4, 5, 6);
            var rotation = Quaternion.Euler(15, 25, 35);
            var instance = service.Spawn(prefab, PoolSpawnOptions.World(position, rotation, parent.transform));

            Assert.That(Vector3.Distance(instance.transform.position, position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(instance.transform.rotation, rotation), Is.LessThan(0.001f));
            Assert.That(instance.transform.localScale, Is.EqualTo(prefab.transform.localScale));
        }

        [Test]
        public void Preload_IsTotalTargetAndTrySpawnReturnsFalseAtCapacity()
        {
            service.Preload(prefab, 2, 2);
            var first = service.Spawn(prefab, default(PoolSpawnOptions));
            service.Preload(prefab, 2, 2);
            Assert.That(service.TrySpawn(prefab, default, out var second), Is.True);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(service.TrySpawn(prefab, default, out var exhausted), Is.False);
            Assert.That(exhausted, Is.Null);
            Assert.That(service.Spawn(prefab, default(PoolSpawnOptions)), Is.Null);
            service.Despawn(first);
            Assert.That(service.TrySpawn(prefab, default, out var reused), Is.True);
            Assert.That(reused, Is.SameAs(first));
        }

        [Test]
        public void ConfigureError_ReturnsInstanceAndPropagatesOriginalError()
        {
            service.Preload(prefab, 1, 1);
            var expected = new InvalidOperationException("Configuration failed");
            GameObject configured = null;
            var actual = Assert.Throws<InvalidOperationException>(() => service.Spawn(prefab,
                new PoolSpawnOptions
                {
                    Name = "Configured",
                    Configure = instance =>
                    {
                        configured = instance;
                        Assert.That(instance.activeSelf, Is.True);
                        Assert.That(instance.name, Is.EqualTo("Configured"));
                        service.Despawn(instance, 1f);
                        throw expected;
                    }
                }));

            Assert.That(actual, Is.SameAs(expected));
            Assert.That(configured.activeSelf, Is.False);
            Assert.That(service.Spawn(prefab, default(PoolSpawnOptions)), Is.SameAs(configured));
        }

        [Test]
        public void DespawnAll_LeavesOtherServicesActiveAndRejectsForeignInstances()
        {
            var otherPrefab = new GameObject("Other prefab");
            using var other = new LeanPoolService();
            try
            {
                var owned = service.Spawn(prefab, default(PoolSpawnOptions));
                var foreign = other.Spawn(otherPrefab, default(PoolSpawnOptions));
                Assert.Throws<ArgumentException>(() => service.Despawn(foreign));
                Assert.Throws<ArgumentException>(() => service.Despawn(otherPrefab));
                service.Despawn(owned, 1f);
                service.DespawnAll();
                Assert.That(owned.activeSelf, Is.False);
                Assert.That(foreign.activeSelf, Is.True);
                Assert.That(service.Spawn(prefab, default(PoolSpawnOptions)), Is.SameAs(owned));
                service.Dispose();
                Assert.That(foreign.activeSelf, Is.True);
            }
            finally
            {
                other.Dispose();
                UnityEngine.Object.DestroyImmediate(otherPrefab);
            }
        }

        [Test]
        public void InvalidArguments_ThrowWithoutConsumingCapacity()
        {
            Assert.Throws<ArgumentNullException>(() => service.Spawn(null, default(PoolSpawnOptions)));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.Preload(prefab, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.Preload(prefab, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.Preload(prefab, 2, 1));
            service.Preload(prefab, 1, 1);
            Assert.Throws<ArgumentException>(() => service.Spawn<BoxCollider>(prefab, parent.transform));
            Assert.That(service.TrySpawn(prefab, default, out _), Is.True);
            service.Dispose();
            service.Dispose();
            Assert.Throws<ObjectDisposedException>(() => service.Spawn(prefab, default(PoolSpawnOptions)));
        }
    }
}
