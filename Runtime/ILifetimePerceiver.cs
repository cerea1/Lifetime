namespace CerealDevelopment.LifetimeManagement
{
    /// <summary>
    /// Interface for observing every object state transitions of generic <see cref="ILifetime"/> type
    /// </summary>
    /// <typeparam name="T">Type that implements <see cref="ILifetime"/></typeparam>
    public interface ILifetimePerceiver<T> where T : ILifetime
    {
        /// <summary>
        /// Invoked when any object of type T gets initialized (enters active state)
        /// </summary>
        /// <param name="initialized">Initialized object</param>
        void OnInitialized(T initialized);
        /// <summary>
        /// Invoked when any object of type T gets disposed (exits active state)
        /// </summary>
        /// <param name="disposed">Disposed object</param>
        void OnDisposed(T disposed);
    }
}
