using UnityEngine;

namespace _Game.daivq.Utilities
{
    /// <summary>
    ///     This singleton need pre instantiated object in scene
    /// </summary>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        public static bool IsExistInstance => Instance != null;

        protected virtual void Awake()
        {
            if (IsExistInstance)
            {
                GameObject obj = gameObject;
                Destroy(this);
                Destroy(obj);
                return;
            }

            Instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (this == Instance) Instance = null;
        }
    }
}
