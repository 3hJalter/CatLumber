using _Game.DesignPattern;
using _Game.GameGrid;
using _Game.Utilities.Grid;
using System.Collections;
using System.Collections.Generic;
using _Game.Data;
using UnityEngine;

public class DebugManager : SimpleSingleton<DebugManager>
{
    private Grid<GameGridCell, GameGridCellData>.DebugGrid debugGrid;
    [SerializeField]
    GameObject FpsDebug;
    [SerializeField]
    GameObject LogDebug;
    int level = -1;
    public bool IsDebugGridLogic => debugGrid != null ? true : false;
    public int Level => level;
    public void DebugGridData(Grid<GameGridCell, GameGridCellData> grid)
    {
        debugGrid?.DrawGrid(grid, true);
    }
    public void OnInit(bool isDebugGridLogic, bool isDebugFps, bool isDebugLog, int level = -1)
    {
        if(isDebugGridLogic)
        {
            debugGrid = new Grid<GameGridCell, GameGridCellData>.DebugGrid();
        }
        FpsDebug.SetActive(isDebugFps);
        LogDebug.SetActive(isDebugLog);
        this.level = level;
        // Save the level to database
        GameData gameData = Database.LoadData();
        gameData.user.normalLevelIndex = level;
        Database.SaveData(gameData);
    }
}
