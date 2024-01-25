using UnityEngine;

namespace _Game.DesignPattern
{
    public class Singleton<T> : HMonoBehaviour where T : HMonoBehaviour
    {
        private static T _instance;

        public static T Ins
        {
            get
            {
                if (_instance is not null) return _instance;
                _instance = FindObjectOfType<T>() ?? new GameObject().AddComponent<T>();
                return _instance;
            }
        }
    }
}
