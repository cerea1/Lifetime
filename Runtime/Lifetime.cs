using System;
using System.Collections.Generic;
using UnityEngine;

namespace CerealDevelopment.LifetimeManagement
{
    public static class LifetimeExtensions
    {
        public static void AddObserver<T>(this T instance, ILifetimeObserver<T> observer, bool forceCachedEvents = true) where T : ILifetime
        {
            Lifetime.AddObserver(instance, observer, forceCachedEvents);
        }
        public static void RemoveObserver<T>(this T instance, ILifetimeObserver<T> observer, bool forceCachedEvents = true) where T : ILifetime
        {
            Lifetime.RemoveObserver(instance, observer, forceCachedEvents);
        }
    }

    /// <summary>
    /// Lifetime system
    /// </summary>
    /// <remarks>
    /// <b>2018.09.05:</b> Добавлена сдвоенная подписка на инициализацию и уничтожение <see cref="AddLifetimeCallbacks{T}(Action{T}, Action{T})"/>
    /// <b>2019.06.03:</b> Реализация изменена на структуру "дерево". Кэш перекочевал в дерево, теперь доступ в <see cref="Lifetime.GetCached{T}"/>
    /// <br/>
    /// </remarks>
    public sealed class Lifetime : MonoBehaviour
    {
        #region Singleton
        internal static Lifetime _instance;
        internal static Lifetime Instance
        {
            get
            {
                if (IsRunning)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<Lifetime>();
                        if (_instance == null)
                        {
                            _instance = new GameObject(typeof(Lifetime).Name).AddComponent<Lifetime>();
                        }
                    }
                    if (_instance.availableTypes == null)
                    {
                        _instance.InitializeTypes();
                    }
                    return _instance;
                }
                return null;
            }
        }
        private static bool IsRunning = false;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                IsRunning = true;
            }
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(_instance.gameObject);
                if (availableTypes == null)
                {
                    InitializeTypes();
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                IsRunning = false;
            }
        }
        private void OnApplicationQuit()
        {
            if (_instance == this)
            {
                IsRunning = false;
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            if (_instance == null)
            {
                if (Application.isPlaying)
                {
                    IsRunning = true;
                }
            }
        }
        #endregion

        internal Dictionary<Type, Type[]> availableTypes;

        private readonly Dictionary<Type, DelegateTreeBase> delegateTrees = new Dictionary<Type, DelegateTreeBase>();


        public static void AddPerceiver<T>(ILifetimePerceiver<T> perceiver) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.AddPerceiverInternal<T>(perceiver);
            }
        }
        public static void RemovePerceiver<T>(ILifetimePerceiver<T> perceiver) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.RemovePerceiverInternal<T>(perceiver);
            }
        }

        public static void AddObserver<T>(T instance, ILifetimeObserver<T> observer, bool forceCachedEvents = true) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.AddObserverInternal<T>(instance, observer, forceCachedEvents);
            }
        }
        public static void RemoveObserver<T>(T instance, ILifetimeObserver<T> observer, bool forceCachedEvents = true) where T : ILifetime
        {
            if (IsRunning)
            {
                if (instance != null)
                {
                    Instance.RemoveObserverInternal<T>(instance, observer, forceCachedEvents);
                }
            }
        }

        public static T GetCached<T>() where T : ILifetime
        {
            if (IsRunning)
            {
                return Instance.GetCachedInternal<T>();
            }

            return default(T);
        }

        public static LifetimeList<T> GetList<T>() where T : ILifetime
        {
            if (IsRunning)
            {
                return Instance.GetCachedListInternal<T>();
            }

            return default(LifetimeList<T>);
        }

        public static void OnInitialized(ILifetime lifetime)
        {
            if (IsRunning)
            {
                Instance.OnInitializedInternal(lifetime);
            }
        }

        public static void OnDisposed(ILifetime lifetime)
        {
            if (IsRunning)
            {
                Instance.OnDisposedInternal(lifetime);
            }
        }

        public static void OnDestroyed(ILifetime lifetime)
        {
            if (IsRunning)
            {
                Instance.OnDestroyedInternal(lifetime);
            }
        }


        public static void AddCallbacks<T>(Action<T> initializedCallback, Action<T> disposedCallback, bool useCached = true) where T : ILifetime
        {
            if (IsRunning)
            {
                if (initializedCallback != null)
                {
                    Instance.AddInitializedCallbackInternal<T>(initializedCallback);
                }

                if (disposedCallback != null)
                {
                    Instance.AddDisposedCallbackInternal<T>(disposedCallback);
                }

                if (useCached && initializedCallback != null)
                {
                    var cached = GetList<T>();
                    for (int i = 0; i < cached.Count; i++)
                    {
                        if (cached[i] != null)
                        {
                            initializedCallback.Invoke(cached[i]);
                        }
                    }
                }
            }
        }

        public static void RemoveCallbacks<T>(Action<T> initializedCallback, Action<T> disposedCallback, bool forceDisposed = true) where T : ILifetime
        {
            if (IsRunning)
            {
                if (initializedCallback != null)
                {
                    Instance.RemoveInitializedCallbackInternal<T>(initializedCallback);
                }
                if (disposedCallback != null)
                {
                    Instance.RemoveDisposedCallbackInternal<T>(disposedCallback);

                    if (forceDisposed)
                    {
                        var list = GetList<T>();
                        for (int i = 0; i < list.Count; i++)
                        {
                            disposedCallback.Invoke(list[i]);
                        }
                    }
                }
            }
        }

        public static void AddInitializedCallback<T>(Action<T> callback) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.AddInitializedCallbackInternal<T>(callback);
            }
        }
        public static void RemoveInitializedCallback<T>(Action<T> callback) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.RemoveInitializedCallbackInternal<T>(callback);
            }
        }

        public static void AddDisposedCallback<T>(Action<T> callback) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.AddDisposedCallbackInternal<T>(callback);
            }
        }
        public static void RemoveDisposedCallback<T>(Action<T> callback) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.RemoveDisposedCallbackInternal<T>(callback);
            }
        }

        private void AddPerceiverInternal<T>(ILifetimePerceiver<T> perceiver) where T : ILifetime
        {
            var type = typeof(T);
            delegateTrees[type].AddPerceiver(perceiver);
        }


        private void RemovePerceiverInternal<T>(ILifetimePerceiver<T> perceiver) where T : ILifetime
        {
            var type = typeof(T);
            delegateTrees[type].RemovePerceiver(perceiver);
        }


        private void AddObserverInternal<T>(T instance, ILifetimeObserver<T> observer, bool forceCachedEvents) where T : ILifetime
        {
            var type = typeof(T);
            delegateTrees[type].AddObserver(instance, observer, forceCachedEvents);
        }

        private void RemoveObserverInternal<T>(T instance, ILifetimeObserver<T> observer, bool forceCachedEvents) where T : ILifetime
        {
            var type = typeof(T);
            delegateTrees[type].RemoveObserver(instance, observer, forceCachedEvents);
        }




        public static void AddInstanceInitializedCallback<T>(T instance, Action<T> callback) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.AddInstanceInitializedCallbackInternal(instance, callback);
            }
        }
        public static void AddInstanceDisposedCallback<T>(T instance, Action<T> callback) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.AddInstanceDisposedCallbackInternal(instance, callback);
            }
        }
        public static void RemoveInstanceInitializedCallback<T>(T instance, Action<T> callback) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.RemoveInstanceInitializedCallbackInternal(instance, callback);
            }
        }
        public static void RemoveInstanceDisposedCallback<T>(T instance, Action<T> callback) where T : ILifetime
        {
            if (IsRunning)
            {
                Instance.RemoveInstanceDisposedCallbackInternal(instance, callback);
            }
        }



        private T GetCachedInternal<T>() where T : ILifetime
        {
            var tree = delegateTrees[typeof(T)] as DelegateTree<T>;
            if (tree.lifetimeList.Count > 0)
            {
                return tree.lifetimeList[0];
            }
            return default(T);
        }

        private LifetimeList<T> GetCachedListInternal<T>() where T : ILifetime
        {
            var tree = delegateTrees[typeof(T)] as DelegateTree<T>;
            return tree.lifetimeList;
        }



        private void AddInitializedCallbackInternal<T>(Action<T> callback) where T : ILifetime
        {
            var tree = delegateTrees[typeof(T)];
            tree.initializedDelegate = Delegate.Combine(tree.initializedDelegate, callback);
        }

        private void AddDisposedCallbackInternal<T>(Action<T> callback) where T : ILifetime
        {
            var tree = delegateTrees[typeof(T)];
            tree.disposedDelegate = Delegate.Combine(tree.disposedDelegate, callback);
        }

        private void AddInstanceInitializedCallbackInternal<T>(T instance, Action<T> callback) where T : ILifetime
        {
            var tree = delegateTrees[typeof(T)];
            var dictionary = tree.initializedInstancesDelegate;
            if (dictionary.TryGetValue(instance, out var @delegate))
            {
                dictionary[instance] = Delegate.Combine(@delegate, callback);
            }
            else
            {
                dictionary.Add(instance, callback);
            }
        }
        private void AddInstanceDisposedCallbackInternal<T>(T instance, Action<T> callback) where T : ILifetime
        {
            var tree = delegateTrees[typeof(T)];
            var dictionary = tree.disposedInstancesDelegate;
            if (dictionary.TryGetValue(instance, out var @delegate))
            {
                dictionary[instance] = Delegate.Combine(@delegate, callback);
            }
            else
            {
                dictionary.Add(instance, callback);
            }
        }

        private void InitializeTypes()
        {
            availableTypes = new Dictionary<Type, Type[]>();
            var typesList = new List<Type> { typeof(ILifetime) };

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            int assembliesLength = assemblies.Length;
            for (int assemblyIterator = 0; assemblyIterator < assembliesLength; assemblyIterator++)
            {
                var assemblyTypes = assemblies[assemblyIterator].GetTypes();
                int typesCount = assemblyTypes.Length;
                for (int typeIterator = 0; typeIterator < typesCount; typeIterator++)
                {
                    if (assemblyTypes[typeIterator].GetInterfaces().Contains(typeof(ILifetime)))
                    {
                        typesList.Add(assemblyTypes[typeIterator]);
                        var interfaces = assemblyTypes[typeIterator].GetInterfaces();

                        int interfacesCount = interfaces.Length;
                        for (int i = 0; i < interfacesCount; i++)
                        {
                            if (interfaces[i].GetInterfaces().Contains(typeof(ILifetime)) && !typesList.Contains(interfaces[i]))
                            {
                                typesList.Add(interfaces[i]);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < typesList.Count; i++)
            {
                var subclasses = new List<Type>();
                if (typesList[i].IsInterface)
                {
                    for (int j = 0; j < typesList.Count; j++)
                    {
                        var interfaces = typesList[j].GetInterfaces();
                        if (interfaces.Contains(typeof(ILifetime)))
                        {
                            int interfacesCount = interfaces.Length;
                            for (int k = 0; k < interfacesCount; k++)
                            {
                                if (interfaces[k].Equals(typesList[i]) && typesList[i] != typesList[j])
                                {
                                    subclasses.Add(typesList[j]);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (typesList[i].GetInterfaces().Contains(typeof(ILifetime)))
                    {
                        subclasses.Add(typesList[i]);
                        for (int j = 0; j < typesList.Count; j++)
                        {
                            if (typesList[j].IsSubclassOf(typesList[i]))
                            {
                                subclasses.Add(typesList[j]);
                            }
                        }
                    }
                }
                if ((typesList[i] == typeof(ILifetime) || typesList[i].GetInterfaces().Contains(typeof(ILifetime))) && !availableTypes.ContainsKey(typesList[i]))
                {
                    availableTypes.Add(typesList[i], subclasses.ToArray());
                }
            }
            InitializeTrees();
        }

        private void InitializeTrees()
        {
            foreach (var availableType in availableTypes)
            {
                AddTreeType(availableType.Key);
                foreach (var value in availableType.Value)
                {
                    AddTreeType(availableType.Key);
                }
            }

            foreach (var treePair in delegateTrees)
            {
                var tree = treePair.Value;
                for (int i = 0; i < tree.autoPerceiveTypes.Count; i++)
                {
                    if (delegateTrees.TryGetValue(tree.autoPerceiveTypes[i], out var perceiveTree))
                    {
                        if (!tree.autoPerceive.Contains(perceiveTree))
                        {
                            tree.autoPerceive.Add(perceiveTree);
                        }
                    }
                    else
                    {
                        Debug.LogException(new Exception());
                    }
                }

                for (int i = 0; i < tree.observableInstancesTypes.Count; i++)
                {
                    if (delegateTrees.TryGetValue(tree.observableInstancesTypes[i], out var observeInstance))
                    {
                        if (!tree.observableInstancesTrees.Contains(observeInstance))
                        {
                            tree.observableInstancesTrees.Add(observeInstance);
                        }
                    }
                    else
                    {
                        Debug.LogException(new Exception());
                    }
                }

                var list = tree.ListBase;
                var parent = tree.parent;
                while (parent != null)
                {
                    parent.ListBase.sublists.AddUnique(list);
                    parent = parent.parent;
                }
                for (int i = 0; i < tree.interfaces.Count; i++)
                {
                    tree.interfaces[i].ListBase.sublists.AddUnique(list);
                }
            }
            foreach (var treePair in delegateTrees.Values)
            {
                treePair.ListBase.Initialize();
            }
        }

        private DelegateTreeBase AddTreeType(Type type)
        {
            Type delegateTreeType = typeof(DelegateTree<>);
            var interfaces = type.GetInterfaces();
            if (type == typeof(ILifetime) || interfaces.Contains(typeof(ILifetime)))
            {
                DelegateTreeBase delegateTree;
                if (!delegateTrees.TryGetValue(type, out delegateTree))
                {
                    var treeType = delegateTreeType.MakeGenericType(type);
                    if (treeType.ContainsGenericParameters)
                    {
                        return null;
                    }
                    var constructor = treeType.GetConstructor(new Type[] { typeof(Type) });
                    delegateTree = (DelegateTreeBase)(constructor.Invoke(new object[] { type }));
                    delegateTrees.Add(type, delegateTree);

                    var parent = type.BaseType;
                    if (parent != null)
                    {
                        var parentTree = AddTreeType(parent);
                        if (parentTree != null)
                        {
                            parentTree.childs.Add(delegateTree);
                            delegateTree.parent = parentTree;
                        }
                    }

                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        var interfaceTree = AddTreeType(interfaces[i]);
                        if (interfaceTree != null)
                        {
                            interfaceTree.childs.Add(delegateTree);
                            delegateTree.interfaces.Add(interfaceTree);
                        }
                    }

                    return delegateTree;
                }
                else
                {
                    return delegateTree;
                }
            }
            return null;
        }


        private void OnInitializedInternal(ILifetime lifetime)
        {
            delegateTrees[lifetime.GetType()].InvokeInitialized(lifetime);
        }

        private void OnDisposedInternal(ILifetime lifetime)
        {
            delegateTrees[lifetime.GetType()].InvokeDisposed(lifetime);
        }

        private void OnDestroyedInternal(ILifetime lifetime)
        {
            delegateTrees[lifetime.GetType()].InvokeDestroyed(lifetime);
        }


        private void RemoveInitializedCallbackInternal<T>(Action<T> callback) where T : ILifetime
        {
            var tree = delegateTrees[typeof(T)];
            tree.initializedDelegate = Delegate.Remove(tree.initializedDelegate, callback);
        }

        private void RemoveDisposedCallbackInternal<T>(Action<T> callback) where T : ILifetime
        {

            var tree = delegateTrees[typeof(T)];
            tree.disposedDelegate = Delegate.Remove(tree.disposedDelegate, callback);
        }

        private void RemoveInstanceInitializedCallbackInternal<T>(T instance, Action<T> callback) where T : ILifetime
        {
            var tree = delegateTrees[typeof(T)];
            if (tree.initializedInstancesDelegate.TryGetValue(instance, out var @delegate))
            {
                tree.initializedInstancesDelegate[instance] = Delegate.Remove(@delegate, callback);
            }
        }
        private void RemoveInstanceDisposedCallbackInternal<T>(T instance, Action<T> callback) where T : ILifetime
        {
            var tree = delegateTrees[typeof(T)];
            if (tree.disposedInstancesDelegate.TryGetValue(instance, out var @delegate))
            {
                tree.disposedInstancesDelegate[instance] = Delegate.Remove(@delegate, callback);
            }
        }
    }

    internal static class Helper
    {
        internal static bool Contains<T>(this T[] array, params T[] obj)
        {
            var arrayLength = array.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                if (array[i] != null)
                {
                    var objLength = obj.Length;
                    for (int j = 0; j < objLength; j++)
                    {
                        if (array[i].Equals(obj[j]))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool AddUnique<T>(this List<T> list, T obj)
        {
            var count = list.Count;
            for (int i = 0; i < count; i++)
            {
                if (ReferenceEquals(list[i], obj))
                {
                    return false;
                }
            }
            list.Add(obj);
            return true;
        }

        internal static bool RemoveSwapBack<T>(this List<T> list, T obj)
        {
            var count = list.Count;
            for (int i = 0; i < count; i++)
            {
                if (ReferenceEquals(list[i], obj))
                {
                    if (i == count - 1)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        list[i] = list[count - 1];
                        list.RemoveAt(count - 1);
                    }
                    return true;
                }
            }
            return false;
        }
        internal static void RemoveAtSwapBack<T>(this List<T> list, int index)
        {
            if (index == list.Count - 1)
            {
                list.RemoveAt(index);
            }
            else
            {
                list[index] = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
            }
        }
    }
}
