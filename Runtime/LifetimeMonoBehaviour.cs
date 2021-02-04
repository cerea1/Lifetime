using System;

using UnityEngine;

namespace CerealDevelopment.LifetimeManagement
{
    /// <summary>
    /// Standard <see cref="ILifetime"/> implementation on <see cref="MonoBehaviour"/>
    /// </summary>
    public abstract class LifetimeMonoBehaviour : MonoBehaviour, ILifetime, ILifetimePoolable
    {
        /// <summary>
        /// <see cref="Awake"/> invokation flag
        /// </summary>
        [System.NonSerialized]
        protected bool isLifetimeConstructed = false;

        /// <summary>
        /// Lifetime state and <see cref="Start"/> invokation flag
        /// </summary>
        [System.NonSerialized]
        protected bool isLifetimeInitialized = false;

        /// <summary>
        /// Lifetime state
        /// </summary>
        public bool IsLifetimeInitialized => isLifetimeInitialized;

        void ILifetimePoolable.ForceConstruct()
        {
            Awake();
        }
        void ILifetimePoolable.Pick()
        {
            Start();
        }

        //ILifetimePoolable
        public virtual void Release()
        {
            if (isLifetimeInitialized)
            {
                Dispose();
                FireDisposedEvent();
            }
        }
        protected void Awake()
        {
            if (!isLifetimeConstructed)
            {
                Construct();

                isLifetimeConstructed = true;
            }
        }
        protected void Start()
        {
            if (!isLifetimeConstructed)
            {
                Awake();
            }
            if (!isLifetimeInitialized)
            {
                Initialize();
                FireInitializedEvent();
            }
        }

        protected void OnDestroy()
        {
            if (isLifetimeInitialized)
            {
                Dispose();
                FireDisposedEvent();
            }

            Destroy();
            FireDestroyedEvent();
        }

        /// <summary>
        /// Replacement for <see cref="Awake"/>
        /// </summary>
        protected virtual void Construct() { }

        /// <summary>
        /// Replacement for <see cref="Start"/>
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Pre-OnDestroy
        /// </summary>
        protected virtual void Dispose() { }

        /// <summary>
        /// Replacement for <see cref="OnDestroy"/>
        /// </summary>
        protected virtual void Destroy() { }

        /// <summary>
        /// <see cref="Lifetime.OnInitialized(ILifetime)"/> invokation
        /// </summary>
        private void FireInitializedEvent()
        {
            isLifetimeInitialized = true;

            Lifetime.OnInitialized(this);
        }

        /// <summary>
        /// <see cref="Lifetime.OnDisposed(ILifetime)"/> invokation
        /// </summary>
        private void FireDisposedEvent()
        {
            isLifetimeInitialized = false;

            Lifetime.OnDisposed(this);
        }

        /// <summary>
        /// <see cref="Lifetime.OnDestroyed(ILifetime)"/> invokation
        /// </summary>
        private void FireDestroyedEvent()
        {
            Lifetime.OnDestroyed(this);
        }
    }
}
