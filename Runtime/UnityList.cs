using System.Collections.Generic;
using UnityEngine;

namespace CerealDevelopment.LifetimeManagement
{
	internal class UnityList<T> where T : Object
	{
		public int Count => count;
		private int count = 0;
		private Dictionary<int, int> hashIndexSet = new Dictionary<int, int>();
		private List<int> ids = new List<int>();
		private List<T> instances = new List<T>();

		public T this[int index]
		{
			get
			{
				return instances[index];
			}
		}

		public void Add(T instance)
		{
			var id = instance.GetInstanceID();
			var index = count;
			ids.Add(id);
			instances.Add(instance);
			hashIndexSet.Add(id, index);
			count++;
		}

		public bool AddUnique(T instance)
		{
			var id = instance.GetInstanceID();
			for (int i = 0; i < ids.Count; i++)
			{
				if (id == ids[i])
				{
					return false;
				}
			}
			var index = count;
			ids.Add(id);
			instances.Add(instance);
			hashIndexSet.Add(id, index);
			count++;
			return true;
		}

		public int IndexOf(T instance)
		{
			var id = instance.GetInstanceID();
			if (hashIndexSet.TryGetValue(id, out var index))
			{
				return index;
			}
			return -1;
		}
		public int IndexOf(int instanceID)
		{
			if (hashIndexSet.TryGetValue(instanceID, out var index))
			{
				return index;
			}
			return -1;
		}

		public int RemoveSwapBack(T instance)
		{
			var id = instance.GetInstanceID();
			return RemoveWithIDSwapBack(id);
		}
		public int RemoveWithIDSwapBack(int id)
		{
			var lastIndex = ids.Count - 1;
			if (hashIndexSet.TryGetValue(id, out var index))
			{
				if (index == lastIndex)
				{
					ids.RemoveAt(index);
					instances.RemoveAt(index);
					hashIndexSet.Remove(id);
					count--;
					return index;
				}
				else
				{
					ids[index] = ids[lastIndex];
					instances[index] = instances[lastIndex];

					hashIndexSet[ids[index]] = index;

					ids.RemoveAt(lastIndex);
					instances.RemoveAt(lastIndex);
					hashIndexSet.Remove(id);

					count--;
					return index;
				}
			}
			return -1;
		}

		public void RemoveAtSwapBack(int index)
		{
			var lastIndex = ids.Count - 1;
			var id = ids[index];
			if (index == lastIndex)
			{
				hashIndexSet.Remove(id);

				ids.RemoveAt(index);
				instances.RemoveAt(index);
				count--;
			}
			else
			{
				hashIndexSet.Remove(id);

				ids[index] = ids[lastIndex];
				instances[index] = instances[lastIndex];

				hashIndexSet[ids[index]] = index;

				ids.RemoveAt(lastIndex);
				instances.RemoveAt(lastIndex);

				count--;
			}
		}

		public void Clear()
		{
			ids.Clear();
			instances.Clear();
			hashIndexSet.Clear();
			count = 0;
		}
	}
}