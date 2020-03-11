using System;
using System.Collections.Generic;
using UnityEngine;

namespace CerealDevelopment.LifetimeManagement
{
    internal class DelegateTree<T> : DelegateTreeBase where T : ILifetime
    {
        private List<ILifetimePerceiver<T>> perceivers = new List<ILifetimePerceiver<T>>();

        private Dictionary<ILifetime, List<ILifetimeObserver<T>>> observers = new Dictionary<ILifetime, List<ILifetimeObserver<T>>>();


        internal LifetimeList<T> lifetimeList = new LifetimeList<T>();
        public override LifetimeListBase ListBase => lifetimeList as LifetimeListBase;

        protected List<DelegateTreeBase> childsInvoking;

        public DelegateTree(Type type) : base(type)
        {
            {
                var autoPerceiveType = typeof(ILifetimePerceiver<>);
                var interfaces = type.GetInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    var seekInterface = interfaces[i];
                    if (seekInterface.IsGenericType)
                    {
                        var baseType = seekInterface.GetGenericTypeDefinition();
                        if (baseType == autoPerceiveType)
                        {
                            var perceiveType = seekInterface.GetGenericArguments()[0];
                            autoPerceiveTypes.Add(perceiveType);
                        }
                    }
                }
            }
            {
                var instanceObserveType = typeof(ILifetimeObserver<>);
                var interfaces = type.GetInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    var seekInterface = interfaces[i];
                    if (seekInterface.IsGenericType)
                    {
                        var baseType = seekInterface.GetGenericTypeDefinition();
                        if (baseType == instanceObserveType)
                        {
                            var perceiveType = seekInterface.GetGenericArguments()[0];
                            if (perceiveType.GetInterfaces().Contains(typeof(ILifetime)))
                            {
                                observableInstancesTypes.Add(perceiveType);
                            }
                        }
                    }
                }
            }

        }
        public override void InvokeInitialized(ILifetime lifetime)
        {
            if (childsInvoking == null)
            {
                childsInvoking = new List<DelegateTreeBase>();
                childsInvoking.AddRange(childs);

                var iterator = 0;
                while (iterator < childsInvoking.Count)
                {
                    var child = childsInvoking[iterator];
                    for (int i = 0; i < child.childs.Count; i++)
                    {
                        if (!childsInvoking.Contains(child.childs[i]))
                        {
                            childsInvoking.Add(child.childs[i]);
                        }
                    }
                    iterator++;
                }
            }

            var lifetimeInstance = (T)lifetime;
            lifetimeList.Add(lifetimeInstance);

            if (observers.TryGetValue(lifetime, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].OnInitialized(lifetimeInstance);
                }
            }
            if (initializedInstancesDelegate.TryGetValue(lifetime, out var @delegate))
            {
                @delegate?.DynamicInvoke(lifetime);
            }

            for (int i = 0; i < perceivers.Count; i++)
            {
                perceivers[i].OnInitialized(lifetimeInstance);
            }
            initializedDelegate?.DynamicInvoke(lifetime);

            for (int i = 0; i < childsInvoking.Count; i++)
            {
                childsInvoking[i].InvokeInitializedSilent(lifetime);
                childsInvoking[i].initializedDelegate?.DynamicInvoke(lifetime);
            }

            var parentTree = parent;
            while (parentTree != null)
            {
                parentTree.InvokeInitializedSilent(lifetime);
                parentTree.initializedDelegate?.DynamicInvoke(lifetime);
                parentTree = parentTree.parent;
            }

            for (int i = 0; i < interfaces.Count; i++)
            {
                interfaces[i].InvokeInitializedSilent(lifetime);
                interfaces[i].initializedDelegate?.DynamicInvoke(lifetime);
            }

            for (int i = 0; i < autoPerceive.Count; i++)
            {
                autoPerceive[i].AddPerceiver(lifetime);
            }
        }

        public override void InvokeInitializedSilent(ILifetime lifetime)
        {
            if (observers.TryGetValue(lifetime, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].OnInitialized((T)lifetime);
                }
            }
            if (initializedInstancesDelegate.TryGetValue(lifetime, out var @delegate))
            {
                @delegate?.DynamicInvoke(lifetime);
            }
            if (perceivers.Count > 0)
            {
                var casted = (T)lifetime;
                for (int i = 0; i < perceivers.Count; i++)
                {
                    perceivers[i].OnInitialized(casted);
                }
            }
        }
        public override void InvokeDisposed(ILifetime lifetime)
        {
            var lifetimeInstance = (T)lifetime;
            lifetimeList.Remove(lifetimeInstance);
            for (int i = 0; i < perceivers.Count; i++)
            {
                perceivers[i].OnDisposed(lifetimeInstance);
            }
            disposedDelegate?.DynamicInvoke(lifetime);

            if (observers.TryGetValue(lifetime, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].OnDisposed(lifetimeInstance);
                }
            }
            if (disposedInstancesDelegate.TryGetValue(lifetime, out var @delegate))
            {
                @delegate?.DynamicInvoke(lifetime);
            }

#if UNITY_EDITOR
            if (childsInvoking == null)
            {
                Debug.LogError($"Invocation tree for {lifetime} is not ready", lifetime as UnityEngine.Object);
            }
#endif
            for (int i = 0; i < childsInvoking.Count; i++)
            {
                childsInvoking[i].InvokeDisposedSilent(lifetime);
                childsInvoking[i].disposedDelegate?.DynamicInvoke(lifetime);
            }

            var parentTree = parent;
            while (parentTree != null)
            {
                parentTree.InvokeDisposedSilent(lifetime);
                parentTree.disposedDelegate?.DynamicInvoke(lifetime);
                parentTree = parentTree.parent;
            }

            for (int i = 0; i < interfaces.Count; i++)
            {
                interfaces[i].InvokeDisposedSilent(lifetime);
                interfaces[i].disposedDelegate?.DynamicInvoke(lifetime);
            }

            for (int i = 0; i < autoPerceive.Count; i++)
            {
                autoPerceive[i].RemovePerceiver(lifetime);
            }

            for (int i = 0; i < observableInstancesTrees.Count; i++)
            {
                observableInstancesTrees[i].RemoveObserver(lifetime);
            }
        }

        public override void InvokeDisposedSilent(ILifetime lifetime)
        {
            if (observers.TryGetValue(lifetime, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].OnDisposed((T)lifetime);
                }
            }
            if (disposedInstancesDelegate.TryGetValue(lifetime, out var @delegate))
            {
                @delegate?.DynamicInvoke(lifetime);
            }
            if (perceivers.Count > 0)
            {
                var casted = (T)lifetime;
                for (int i = 0; i < perceivers.Count; i++)
                {
                    perceivers[i].OnDisposed(casted);
                }
            }
        }

        public override void InvokeDestroyed(ILifetime lifetime)
        {
            if (lifetime.IsLifetimeInitialized)
            {
                throw new Exception();
            }
            observers.Remove(lifetime);
            initializedInstancesDelegate.Remove(lifetime);
            disposedInstancesDelegate.Remove(lifetime);
        }

        public override void AddPerceiver(object observer)
        {
            var lifetimeObserver = (ILifetimePerceiver<T>)observer;
            if (perceivers.AddUnique(lifetimeObserver))
            {
                var list = lifetimeList;
                for (int i = 0; i < list.Count; i++)
                {
                    lifetimeObserver.OnInitialized(list[i]);
                }
            }
        }

        public override void RemovePerceiver(object observer)
        {
            var lifetimeObserver = (ILifetimePerceiver<T>)observer;
            if (perceivers.RemoveSwapBack(lifetimeObserver))
            {
                var list = lifetimeList;
                for (int i = 0; i < list.Count; i++)
                {
                    lifetimeObserver.OnDisposed(list[i]);
                }
            }
        }
        public override void AddObserver(ILifetime instance, object objObserver, bool forceCachedEvents)
        {
            var observer = (ILifetimeObserver<T>)objObserver;
            if (observers.TryGetValue(instance, out var list))
            {
                if (!list.Contains(observer))
                {
                    list.Add(observer);
                }
            }
            else
            {
                observers.Add(instance, new List<ILifetimeObserver<T>> { observer });
            }
            if (forceCachedEvents)
            {
                if (instance.IsLifetimeInitialized)
                {
                    observer.OnInitialized((T)instance);
                }
            }
        }
        public override void RemoveObserver(ILifetime instance, object observer, bool forceCachedEvents)
        {
            if (observers.TryGetValue(instance, out var list))
            {
                var lifetimeObserver = (ILifetimeObserver<T>)observer;
                if (list.RemoveSwapBack(lifetimeObserver) && forceCachedEvents && instance.IsLifetimeInitialized)
                {
                    lifetimeObserver.OnDisposed((T)instance);
                }
            }
        }

        internal override void RemoveObserver(object objObserver)
        {
            var observer = (ILifetimeObserver<T>)objObserver;
            foreach (var keyValue in observers)
            {
                if (keyValue.Value.RemoveSwapBack(observer))
                {
                    var id = keyValue.Key;
                    var list = lifetimeList;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var instance = list[i];
                        if (Equals(instance, id))
                        {
                            observer.OnDisposed(instance);
                        }
                    }

                }
            }
        }

        internal T GetCached()
        {
            var list = lifetimeList;
            if (list.Count > 0)
            {
                return list[0];
            }
            return default(T);
        }
        internal LifetimeList<T> GetCachedList()
        {
            return lifetimeList;
        }
    }
}
