namespace CerealDevelopment.LifetimeManagement
{
    /// <summary>
    /// Interface for <see cref="LifetimePool"/> object implementation
    /// </summary>
    public interface ILifetimePoolable
    {
        /// <summary>
        /// Invoked when pool object gets constructed
        /// </summary>
        void ForceConstruct();
        /// <summary>
        /// Invoked when object gets removed from pool
        /// </summary>
        void Pick();
        /// <summary>
        /// Invoked when object gets back in pool
        /// </summary>
        void Release();
    }
}