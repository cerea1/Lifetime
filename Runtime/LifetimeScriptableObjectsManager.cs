
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace CerealDevelopment.LifetimeManagement
{
    public class LifetimeScriptableObjectsManager : MonoBehaviour
    {
        private static bool _needsInstance = true;
        private static LifetimeScriptableObjectsManager _instance;
        private static LifetimeScriptableObjectsManager Instance
        {
            get
            {
                if (_needsInstance)
                {
                    _instance = FindObjectOfType<LifetimeScriptableObjectsManager>();
                    _needsInstance = _instance == null;
                    if (_needsInstance)
                    {
                        var path = $"Singletons/{nameof(LifetimeScriptableObjectsManager)}";
                        var resource = Resources.Load<LifetimeScriptableObjectsManager>(path);
                        _instance = Instantiate(resource);
                        _needsInstance = false;
                    }
                }
                return _instance;
            }
        }

        [SerializeField]
        private List<LifetimeScriptableObject> scriptableObjects = new List<LifetimeScriptableObject>();

        private void Awake()
        {
            if (_instance == null || _instance == this)
            {
                _instance = this;
                _needsInstance = false;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogError($"{GetType().FullName} instance is not null, destroying");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            Debug.Log(Instance.name + " loaded");
            var interfaceType = typeof(IResourcesLifetimeScriptableObject);
            var types = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p));
            var objectType = typeof(LifetimeScriptableObject);
            var initializeMethod = interfaceType.GetMethod(nameof(IResourcesLifetimeScriptableObject.LifetimeInitialize));
            foreach (var type in types)
            {
                if (objectType.IsAssignableFrom(type))
                {
                    var resources = Resources.FindObjectsOfTypeAll(type);
                    foreach (var resource in resources)
                    {
                        if (resource is IResourcesLifetimeScriptableObject)
                        {
                            (resource as IResourcesLifetimeScriptableObject).LifetimeInitialize();
                        }
                    }
                }
                else
                {
                    Debug.Log($"Object not assignable from {type}");
                }
            }

        }

        [RuntimeInitializeOnLoadMethod]
        public static void InitializeOnLoad()
        {
            Debug.Log(Instance.name);
        }
    }
}
