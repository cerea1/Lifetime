using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace CerealDevelopment.LifetimeManagement
{
	/// <summary>
	/// Simple Unity objects pooling
	/// </summary>
	public sealed class LifetimePool : MonoBehaviour
	{
		private static LifetimePool _instance;
		private static LifetimePool prefabsPoolInstance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<LifetimePool>();
					if (_instance == null)
					{
						_instance = new GameObject(typeof(LifetimePool).Name).AddComponent<LifetimePool>();
					}
				}
				return _instance;
			}
		}

		private List<ILifetimePoolable> pickPoolables = new List<ILifetimePoolable>();
		private class Pool : IUnityObject
		{
			public GameObject prefab;
			private int prefabInstanceID;

			public Scene targetScene;

			public UnityList<GameObject> inPoolObjects;
			public UnityList<GameObject> outOfPoolObjects;
			private int maxObjects;
			public int MaxObjects
			{
				get
				{
					return maxObjects;
				}
				set
				{
					if (value > maxObjects)
					{
						maxObjects = value;
						Populate();
					}
				}
			}

			private int additionalObjects = 5;


			public Pool(GameObject prefab, Scene targetScene, int maxObjects = 10)
			{
				inPoolObjects = new UnityList<GameObject>();
				outOfPoolObjects = new UnityList<GameObject>();
				this.targetScene = targetScene;
				this.prefab = prefab;
				this.prefabInstanceID = prefab.GetInstanceID();
				this.maxObjects = maxObjects;
			}

			public void Populate()
			{
				int totalCount = inPoolObjects.Count + outOfPoolObjects.Count;
				while (maxObjects >= totalCount)
				{
					InstantiateObject();
					totalCount++;
				}
			}

			public bool PrePopulate()
			{
				var objectsCount = inPoolObjects.Count + outOfPoolObjects.Count;
				var maxObjects = MaxObjects + additionalObjects;
				if (maxObjects > objectsCount)
				{
					InstantiateObject();
					return true;
				}
				return false;
			}

			private void InstantiateObject()
			{
				var newObject = Instantiate(prefab);

				SceneManager.MoveGameObjectToScene(newObject, targetScene);
				newObject.SetActive(false);
#if UNITY_EDITOR
				newObject.name = prefab.name + "(PoolClone)";
#endif

				var poolable = newObject.GetComponentsInChildren<ILifetimePoolable>(true);
				for (int i = 0; i < poolable.Length; i++)
				{
					poolable[i].ForceConstruct();
				}
				inPoolObjects.Add(newObject);
			}

			public GameObject GetInstance(Transform parent, Vector3 position, Quaternion rotation)
			{
				if (inPoolObjects.Count == 0)
				{
					MaxObjects += 1;
				}
				if (inPoolObjects.Count == 0)
				{
					InstantiateObject();
				}
				var lastIndex = inPoolObjects.Count - 1;
				var targetObject = inPoolObjects[lastIndex];

				inPoolObjects.RemoveAtSwapBack(lastIndex);
				while (targetObject == null && inPoolObjects.Count > 0)
				{
					lastIndex = inPoolObjects.Count - 1;
					targetObject = inPoolObjects[lastIndex];

					inPoolObjects.RemoveAtSwapBack(lastIndex);
				}

				if(targetObject == null)
				{
					if (inPoolObjects.Count == 0)
					{
						InstantiateObject();
					}
					lastIndex = inPoolObjects.Count - 1;
					targetObject = inPoolObjects[lastIndex];
				}


				outOfPoolObjects.Add(targetObject);
				if (targetObject.transform.parent != parent)
				{
					targetObject.transform.SetParent(parent);
				}
				targetObject.transform.position = position;
				targetObject.transform.rotation = rotation;

				targetObject.SetActive(true);
				return targetObject;
			}

			public void RestoreInstance(GameObject instance)
			{
				var index = outOfPoolObjects.IndexOf(instance);
				if (index < 0)
				{
					throw new System.ArgumentException();
				}
				inPoolObjects.Add(outOfPoolObjects[index]);
				outOfPoolObjects.RemoveAtSwapBack(index);

				var poolableComponents = instance.GetComponentsInChildren<ILifetimePoolable>(true);
				for (int i = 0; i < poolableComponents.Length; i++)
				{
					poolableComponents[i].Release();
				}
				instance.gameObject.SetActive(false);
				//instance.transform.SetParent(poolContainer);
				RestoreObject(instance);

			}

			public GameObject GetObject(int instanceID)
			{
				var index = outOfPoolObjects.IndexOf(instanceID);
				if(index != -1)
				{
					return outOfPoolObjects[index];
				}
				return null;
			}

			public void RestoreInstance(int instanceID)
			{
				var index = outOfPoolObjects.IndexOf(instanceID);
				if (index < 0)
				{
					throw new System.ArgumentException();
				}
				var poolObject = outOfPoolObjects[index];
				var instance = poolObject.gameObject;
				inPoolObjects.Add(poolObject);
				outOfPoolObjects.RemoveAtSwapBack(index);
				//instance.transform.SetParent(poolContainer);
				RestoreObject(instance);

				var poolableComponents = instance.GetComponentsInChildren<ILifetimePoolable>(true);
				for (int i = 0; i < poolableComponents.Length; i++)
				{
					poolableComponents[i].Release();
				}

				instance.SetActive(false);
			}

			private void RestoreObject(GameObject instance)
			{
				//TODO: замутить Restore
			}

			public int GetInstanceID()
			{
				return prefabInstanceID;
			}
		}

		public List<GameObject> prefabs = new List<GameObject>();

		[SerializeField]
		private int initialSpawnCount = 10;

		private UnityInterfacedList<Pool> prefabsHashPool = new UnityInterfacedList<Pool>();
		private Dictionary<int, Pool> instancesHashPool = new Dictionary<int, Pool>();

		private Scene poolScene;

		private void Awake()
		{
			_instance = this;

			//poolScene = SceneManager.GetSceneByName("Pool");
			//if (poolScene == default(Scene))
			{
				poolScene = SceneManager.CreateScene("Pool");
				//SceneManager.LoadSceneAsync(poolScene.name, LoadSceneMode.Additive);
			}

			//poolContainer = new GameObject().transform;
			//poolContainer.SetParent(transform);
			//poolContainer.gameObject.SetActive(false);

			foreach (var prefab in prefabs)
			{
				AddPool_Private(prefab, initialSpawnCount);
			}
		}

		private void OnDestroy()
		{
			_instance = null;

			pickPoolables.Clear();

			if (poolScene.isLoaded)
			{
				SceneManager.UnloadSceneAsync(poolScene);
			}
		}


		void LateUpdate()
		{
			var iterator = 0;
			while (iterator < pickPoolables.Count)
			{
				try
				{
					for (; iterator < pickPoolables.Count; iterator++)
					{
						pickPoolables[iterator].Pick();
					}
				}
				catch (System.Exception e)
				{
					Debug.LogException(e);
				}
				iterator++;
			}

			pickPoolables.Clear();

			for(int i = 0; i < prefabsHashPool.Count; i++)
			{
				if (prefabsHashPool[i].PrePopulate())
				{
					break;
				}
			}
		}

		private Pool AddPool_Private(GameObject prefab, int spawnCount)
		{
			if (spawnCount <= 0)
			{
				spawnCount = prefabsPoolInstance.initialSpawnCount;
			}
			Pool pool;
			var index = prefabsHashPool.IndexOf(prefab.GetInstanceID());
			if (index != -1)
			{
				pool = prefabsHashPool[index];
				if (pool.MaxObjects < spawnCount)
				{
					pool.MaxObjects = spawnCount;
				}
				return pool;
			}

			pool = new Pool(prefab, poolScene, spawnCount);
			prefabsHashPool.Add(pool);
			pool.Populate();
			return pool;
		}

		private GameObject GetInstance_Private(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
		{
			int hash = prefab.GetInstanceID();
			Pool pool;
			var index = prefabsHashPool.IndexOf(hash);
			if (index == -1)
			{
				pool = AddPool_Private(prefab, initialSpawnCount);
			}
			else
			{
				pool = prefabsHashPool[index];
			}
			var gameObject = pool.GetInstance(parent, position, rotation);


			var poolableComponents = gameObject.GetComponentsInChildren<ILifetimePoolable>(true);
			for (int i = 0; i < poolableComponents.Length; i++)
			{
				pickPoolables.Add(poolableComponents[i]);
			}


			instancesHashPool.Add(gameObject.GetInstanceID(), pool);
			return gameObject;
		}

		/// <summary>
		/// Creates or grows objects pool for prefab
		/// </summary>
		/// <param name="prefab">Prefab to build pool</param>
		/// <param name="spawnCount">Initial pool size</param>
		public static void AddPool(GameObject prefab, int spawnCount)
		{
			prefabsPoolInstance.AddPool_Private(prefab, spawnCount);
		}
		/// <summary>
		/// Creates or grows objects pool for prefab
		/// </summary>
		/// <param name="prefab">Prefab to build pool</param>
		/// <param name="spawnCount">Initial pool size</param>
		public static void AddPool<T>(T prefab, int spawnCount) where T : Component
		{
			prefabsPoolInstance.AddPool_Private(prefab.gameObject, spawnCount);
		}

		public static GameObject GetInstance(GameObject prefab, Transform parent)     //TODO: намутить конвертеров там всяких
		{
			return prefabsPoolInstance.GetInstance_Private(prefab, parent, prefab.transform.position, prefab.transform.rotation);
		}
		public static GameObject GetInstance(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)     //TODO: намутить конвертеров там всяких
		{
			return prefabsPoolInstance.GetInstance_Private(prefab, parent, position, rotation);
		}

		public static T GetInstance<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component     //TODO: намутить конвертеров там всяких
		{
			return prefabsPoolInstance.GetInstance_Private(prefab.gameObject, null, position, rotation).GetComponent<T>();
		}

		public static T GetInstance<T>(T prefab, Transform parent) where T : Component     //TODO: намутить конвертеров там всяких
		{
			return prefabsPoolInstance.GetInstance_Private(prefab.gameObject, parent, prefab.transform.position, prefab.transform.rotation).GetComponent<T>();
		}
		public static T GetInstance<T>(T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component     //TODO: намутить конвертеров там всяких
		{
			return prefabsPoolInstance.GetInstance_Private(prefab.gameObject, parent, position, rotation).GetComponent<T>();
		}

		private bool RestoreInstance_Private(GameObject instance)
		{
			int instanceID = instance.GetInstanceID();
			Pool pool;
			if (instancesHashPool.TryGetValue(instanceID, out pool))
			{
				instancesHashPool.Remove(instanceID);
				pool.RestoreInstance(instance);

				var poolableComponents = instance.GetComponentsInChildren<ILifetimePoolable>(true);
				for (int i = 0; i < poolableComponents.Length; i++)
				{
					pickPoolables.RemoveSwapBack(poolableComponents[i]);
				}

				return true;
			}
			else
			{
				Debug.LogException(new System.Exception($"Cannot find object {instance} in pool"), instance);

				var poolableComponents = instance.GetComponentsInChildren<ILifetimePoolable>(true);
				for (int i = 0; i < poolableComponents.Length; i++)
				{
					poolableComponents[i].Release();
				}
				instance.SetActive(false);
				return false;
			}
		}
		private bool RestoreInstance_Private(int instanceID)
		{
			Pool pool;
			if (instancesHashPool.TryGetValue(instanceID, out pool))
			{
				var instance = pool.GetObject(instanceID);
				instancesHashPool.Remove(instanceID);
				pool.RestoreInstance(instanceID);

				var poolableComponents = instance.GetComponentsInChildren<ILifetimePoolable>(true);
				for (int i = 0; i < poolableComponents.Length; i++)
				{
					pickPoolables.RemoveSwapBack(poolableComponents[i]);
				}

				return true;
			}
			else
			{
				Debug.LogException(new System.Exception("Cannot find object in pool"));
				return false;
			}
		}

		public static bool RestoreInstance(GameObject instance)
		{
			return LifetimePool.prefabsPoolInstance.RestoreInstance_Private(instance);
		}

		public static bool RestoreInstance<T>(T instance) where T : Component
		{
			return RestoreInstance(instance.gameObject);
		}
		public static bool RestoreInstance(int instanceID)
		{
			return LifetimePool.prefabsPoolInstance.RestoreInstance_Private(instanceID);
		}
	}
}