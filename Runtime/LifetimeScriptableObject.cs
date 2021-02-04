
using UnityEngine;

namespace CerealDevelopment.LifetimeManagement
{
    public abstract class LifetimeScriptableObject : ScriptableObject, ILifetime
    {
        [System.NonSerialized]
        private bool isLifetimeInitialized = false;

        public bool IsLifetimeInitialized => isLifetimeInitialized;

        public void LifetimeInitialize()
        {
            if (!isLifetimeInitialized)
            {
                Initialize();
                isLifetimeInitialized = true;
                Lifetime.OnInitialized(this);
            }
        }

        public void LifetimeDispose()
        {
            if (isLifetimeInitialized)
            {
                isLifetimeInitialized = false;
                Dispose();
                Lifetime.OnDisposed(this);
                Lifetime.OnDestroyed(this);
            }
        }

        protected virtual void Initialize() { }
        protected virtual void Dispose() { }
    }
}
