using _Game.DesignPattern.StateMachine;
using GameGridEnum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Game.GameGrid.Unit
{
    public abstract class GridUnitDynamic : GridUnit
    {
        [Title("Dynamic Unit")]
        [SerializeField] protected GridUnitDynamicType gridUnitDynamicType;
        [SerializeField] public Anchor anchor;
        public abstract StateEnum CurrentStateId
        {
            get;
            set;
        }

        #region SAVING DATA
        public override IMemento Save()
        {
            IMemento save;
            if (overrideSpawnSave != null)
            {
                save = overrideSpawnSave;
                overrideSpawnSave = null;
            }
            else
            {
                save = new DynamicUnitMemento<GridUnitDynamic>(this, CurrentStateId, isSpawn, Tf.position, skin.rotation, startHeight, endHeight
                , unitTypeY, unitTypeXZ, belowUnits, neighborUnits, upperUnits, mainCell, cellInUnits, islandID, lastPushedDirection);
            }
            return save;
        }

        public class DynamicUnitMemento<T> : UnitMemento<T> where T : GridUnitDynamic
        {
            protected StateEnum currentState;
            public DynamicUnitMemento(GridUnitDynamic main,StateEnum currentState, params object[] data) : base((T)main, data)
            {
                this.currentState = currentState;
            }

            public override void Restore()
            {
                base.Restore();
                main.CurrentStateId = currentState;
            }
        }
        #endregion
    }
}
