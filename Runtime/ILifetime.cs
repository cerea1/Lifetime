using System;

namespace CerealDevelopment.LifetimeManagement
{
    /// <summary>
    /// Lifetime base interface.
    /// Every type that implements <see cref="ILifetime"/> is responsible for calling
    /// <see cref="Lifetime.OnInitialized(ILifetime)"/>,
    /// <see cref="Lifetime.OnDisposed(ILifetime)"/> and
    /// <see cref="Lifetime.OnDestroyed(ILifetime)"/>
    /// </summary>
    /// <seealso cref="Lifetime"/>
    /// <seealso cref="LifetimeMonoBehaviour"/>
    public interface ILifetime : IUnityObject
    {
        /// <summary>
        /// Flags if lifetime object is in active state
        /// </summary>        
        bool IsLifetimeInitialized { get; }
    }
}
