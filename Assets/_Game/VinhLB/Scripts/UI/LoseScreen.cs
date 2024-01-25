using System.Collections;
using System.Collections.Generic;
using _Game.GameGrid;
using _Game.Managers;
using _Game.DesignPattern;
using UnityEngine;

namespace VinhLB
{
    public class LoseScreen : UICanvas
    {
        public void OnClickRestartButton()
        {
            LevelManager.Ins.OnRestart();
            GameManager.Ins.PostEvent(EventID.StartGame);
            Close();
        }

        public void OnClickMoreTimeButton()
        {
            GameplayManager.Ins.OnResetTime();
            Close();
        }
    }
}