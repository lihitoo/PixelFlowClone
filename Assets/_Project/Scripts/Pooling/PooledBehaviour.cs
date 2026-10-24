using Lean.Pool;
using UnityEngine;

namespace Dreamy.Template.Pooling
{
    public abstract class PooledBehaviour : MonoBehaviour, IPoolable
    {
        public virtual void OnSpawn()
        {
        }

        public virtual void OnDespawn()
        {
        }
    }
}
