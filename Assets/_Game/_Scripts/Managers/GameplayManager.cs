using _Game.DesignPattern;
using _Game.GameGrid;
using _Game.UIs.Screen;
using _Game.Utilities;
using _Game.Utilities.Timer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VinhLB;

namespace _Game.Managers
{
    [DefaultExecutionOrder(-90)]
    public class GameplayManager : Singleton<GameplayManager>
    {
        private int time;
        private int undoCount;
        STimer timer;

        InGameScreen screen;

        private bool isCanUndo = true;
        private bool isCanResetIsland = true;
        
        public bool IsCanUndo
        {
            get => isCanUndo;
            set
            {
                isCanUndo = value;
                screen.SetActiveUndo(value);
            }
        }

        public bool IsCanResetIsland
        {
            get => isCanResetIsland;
            set
            {
                isCanResetIsland = value;
                screen.SetActiveResetIsland(value);
            }
        }
        private void Awake()
        {
            screen = UIManager.Ins.GetUI<InGameScreen>();
            screen.Close();
            timer = TimerManager.Inst.PopSTimer();
            GameManager.Ins.RegisterListenerEvent(EventID.StartGame, OnStartGame);
            GameManager.Ins.RegisterListenerEvent(EventID.WinGame, OnWinGame);
            GameManager.Ins.RegisterListenerEvent(EventID.LoseGame, OnLoseGame);
            screen.OnUndo += OnUndo;
            screen.OnResetIsland += OnResetIsland;
        }

        public void OnResetTime()
        {
            time += Constants.LEVEL_TIME;
            screen.Time = time;
            timer.Start(1f, CountTime, true);
        }
        
        private void OnStartGame()
        {
            OnResetTime();
            undoCount = Constants.UNDO_COUNT;
            IsCanResetIsland = true;
            IsCanUndo = true;
            GameManager.Ins.ChangeState(GameState.InGame);
        }

        private void OnWinGame()
        {
            timer.Stop();
            DevLog.Log(DevId.Hung, "ENDGAME - Show Win Screen");
            UIManager.Ins.OpenUI<WinScreen>();
            GameManager.Ins.ChangeState(GameState.EndGame);
        }

        private void OnLoseGame()
        {
            timer.Stop();
            DevLog.Log(DevId.Hung, "ENDGAME - Show Lose Screen");
            UIManager.Ins.OpenUI<LoseScreen>();
            GameManager.Ins.ChangeState(GameState.EndGame);
        }

        private void OnDestroy()
        {
            GameManager.Ins.UnregisterListenerEvent(EventID.StartGame, OnStartGame);
            GameManager.Ins.UnregisterListenerEvent(EventID.WinGame, OnWinGame);
            GameManager.Ins.UnregisterListenerEvent(EventID.LoseGame, OnLoseGame);
            screen.OnUndo -= OnUndo;
            screen.OnResetIsland -= OnResetIsland;
            TimerManager.Inst.PushSTimer(timer);         
        }

        private void OnUndo()
        {
            if (!isCanUndo) return;
            LevelManager.Ins.OnUndo();
        }

        private void OnResetIsland()
        {
            if (!isCanResetIsland) return;
            LevelManager.Ins.ResetLevelIsland();
        }
        
        private void CountTime()
        {
            time -= 1;
            screen.Time = time;
            if(time <= 0)
            {
                OnLoseGame();
            }
        }
    }
}