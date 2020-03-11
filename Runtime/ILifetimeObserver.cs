namespace CerealDevelopment.LifetimeManagement
{
    /// <summary>
    /// Interface for observing <see cref="ILifetime"/> object state transitions
    /// </summary>
    /// <typeparam name="T">Type that implements <see cref="ILifetime"/></typeparam>
    public interface ILifetimeObserver<T> where T : ILifetime
    {
        /// <summary>
        /// Invoked when <see cref="ILifetime"/> instance is entering active state
        /// </summary>
        /// <param name="initialized">Observed instance</param>
        void OnInitialized(T initialized);

        /// <summary>
        /// Invoked when <see cref="ILifetime"/> instance is exiting active state
        /// </summary>
        /// <param name="disposed">Observed instance</param>
        void OnDisposed(T disposed);
    }
}
