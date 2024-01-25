using System.Collections.Generic;
using _Game.DesignPattern;
using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.DynamicUnit.Interface;
using _Game.Managers;
using _Game.Utilities;
using GameGridEnum;

namespace _Game.GameGrid.Unit.DynamicUnit.Bomb.BombState
{
    public class ExplodeBombState : IState<Bomb>
    {
        public StateEnum Id => StateEnum.Explode;
        public void OnEnter(Bomb t)
        {
            DevLog.Log(DevId.Hoang, "ExplodeBombState OnEnter");
            // Cast to ExplosiveBox
            ParticlePool.Play(DataManager.Ins.VFXData.GetParticleSystem(VFXType.BombExplosion),
                t.Tf.position);
            Explode(t);
        }

        public void OnExecute(Bomb t)
        {
        }

        public void OnExit(Bomb t)
        {
           
        }
        
        private static void Explode(GridUnit t)
        {
            // Merge all neighbor, below, upper units
            List<GridUnit> units = new();
            units.AddRange(t.neighborUnits);
            units.AddRange(t.belowUnits);
            units.AddRange(t.upperUnits);
            units.Add(t);
            // Despawn all units, include this unit
            // TODO: Handle with Player and Enemy -> Die State instead of Despawn
            for (int index = 0; index < units.Count; index++)
            {
                GridUnit unit = units[index];
                if (unit is ICharacter character) character.OnCharacterDie();
                else if (unit != t && unit is IExplosives explosives) explosives.Explode();
                else unit.OnDespawn();
            }
        }
    }
}
