namespace _Game.DesignPattern
{
    public class SimpleSingleton<T> : HMonoBehaviour where T : HMonoBehaviour
    {
        private static T _instance;

        public static T Ins
        {
            get
            {
                if (_instance is not null) return _instance;
                _instance = FindObjectOfType<T>();
                return _instance;
            }
        }
    }
}
