using System;
using System.Collections.Generic;

namespace CerealDevelopment.LifetimeManagement
{
    internal abstract class DelegateTreeBase
    {
        public Type type;
        public List<DelegateTreeBase> interfaces = new List<DelegateTreeBase>();
        public List<DelegateTreeBase> childs = new List<DelegateTreeBase>();

        public List<DelegateTreeBase> autoPerceive = new List<DelegateTreeBase>();
        public List<Type> autoPerceiveTypes = new List<Type>();

        public List<DelegateTreeBase> observableInstancesTrees = new List<DelegateTreeBase>();
        public List<Type> observableInstancesTypes = new List<Type>();
        public abstract LifetimeListBase ListBase { get; }
        public DelegateTreeBase parent;


        public Delegate initializedDelegate;
        public Delegate disposedDelegate;

        public Dictionary<ILifetime, Delegate> initializedInstancesDelegate = new Dictionary<ILifetime, Delegate>();
        public Dictionary<ILifetime, Delegate> disposedInstancesDelegate = new Dictionary<ILifetime, Delegate>();

        public DelegateTreeBase(Type type)
        {
            this.type = type;
        }


        public abstract void InvokeInitialized(ILifetime lifetime);
        public abstract void InvokeInitializedSilent(ILifetime lifetime);
        public abstract void InvokeDisposed(ILifetime lifetime);
        public abstract void InvokeDisposedSilent(ILifetime lifetime);
        public abstract void InvokeDestroyed(ILifetime lifetime);

        public abstract void AddPerceiver(object perceiver);
        public abstract void RemovePerceiver(object perceiver);

        public abstract void AddObserver(ILifetime instance, object observer, bool forceCachedEvents);
        public abstract void RemoveObserver(ILifetime instance, object observer, bool forceCachedEvents);

        internal abstract void RemoveObserver(object observer);
    }
}
