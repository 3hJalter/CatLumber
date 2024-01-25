using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Game.daivq.Utilities
{
    /// <summary>
    ///     This type of pool require object that done used have to return to pool explicitly
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class PrefabStackPool<T> where T : MonoBehaviour
    {
        private Transform _container;
        private T _prefab;
        private Stack<T> _stack;

        public PrefabStackPool(T prefab, Transform container, int capacity)
        {
            _prefab = prefab;
            _container = container;
            _stack = new Stack<T>(capacity);
        }

        public void PrePool(int prePoolAmount)
        {
            for (int i = 0; i < prePoolAmount; i++)
            {
                T spawnedObjectComp = Object.Instantiate(_prefab, _container);
                spawnedObjectComp.gameObject.SetActive(false);
                _stack.Push(spawnedObjectComp);
            }
        }

        public T UseGameObject()
        {
            T result = null;
            if (_stack.Count > 0)
            {
                result = _stack.Pop();
                result.gameObject.SetActive(true);
            }
            else
            {
                result = Object.Instantiate(_prefab, _container);
                result.gameObject.SetActive(true);
            }

            return result;
        }

        public void ReturnGameObject(T gameObjectComponent)
        {
            gameObjectComponent.gameObject.SetActive(false);
            _stack.Push(gameObjectComponent);
        }
    }
}
