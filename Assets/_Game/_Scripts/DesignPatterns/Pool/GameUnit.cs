using UnityEngine;

namespace _Game.DesignPattern
{
    public abstract class GameUnit : HMonoBehaviour
    {
        [SerializeField]
        private PoolType poolType;
        public PoolType PoolType => poolType;
    }
}
