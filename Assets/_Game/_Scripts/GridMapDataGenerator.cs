using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Game._Scripts.InGame;
using _Game.Data;
using _Game.DesignPattern;
using _Game.GameGrid.GridSurface;
using _Game.GameGrid.Unit;
using _Game.GameGrid.Unit.StaticUnit;
using _Game.Managers;
using _Game.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using VinhLB;

public class GridMapDataGenerator : MonoBehaviour
{
    [Title("Level Data")]
    [Tooltip("The distance between the corner cells and the surfaceUnit closest to it")]
    [SerializeField] private int offsetSurfaceWithFirstCell = 3; // Should be 3
    [Tooltip("Random rotation for rock")]
    [SerializeField] private bool isRandomRotationForRock = true;
    [InlineButton("SaveLevelAsJson", "Save Level")]
    [InfoBox("The name of the level, must be in the format Lvl_number or Lvl_DC_number or Lvl_S_number")]
    [SerializeField] private string mapLevelName = "Lvl_0";

    [Title("Load Level")]
    [InlineButton("LoadLevels", "Load")]
    [InlineButton("UnLoadLevels", "UnLoad")]
    [SerializeField] private new string name = "Lvl_";
    private Level _loadedLevel;

    [Title("Level Map Container")]
    [InlineButton("TestHintTrail", "Run")]
    [SerializeField]
    private TestHintLineInEditMode hintLineTrail;
    [SerializeField]
    private string hintLineString = "";
    [InlineButton("AddNewHintLineObjectList", "New"), PropertyTooltip("Use the position data from hintLineObjList to create hint lint string" +
                                                                      "\nNew: Add a new hint line string" +
                                                                      "\nAdd: Add a new string to the current hint line string" +
                                                                      "\nClear: Destroy and Clear all hint Line Object")]
    [InlineButton("AddHintLineObjectList", "Add")]
    [InlineButton("DestroyAllHintLine", "Clear")]
    [PropertySpace(SpaceBefore = 5, SpaceAfter = 10)]
    [SerializeField] private List<Transform> hintLineObj;

    [FoldoutGroup("Container")]
    [InfoBox("Container store where units in map are drawn.")]
    [SerializeField] private Transform surfaceContainer;
    [FoldoutGroup("Container")]
    [SerializeField] private Transform unitContainer;
    [FoldoutGroup("Container")]
    [SerializeField] private Transform shadowContainer;
    [FoldoutGroup("Container")]
    [SerializeField] private Transform hintLineContainer;

    [FoldoutGroup("Grid Utilities")]
    [HorizontalGroup("Grid Utilities/Horizontal")]
    [Button]
    [BoxGroup("Grid Utilities/Horizontal/1")]
    private void DestroyAll()
    {
        DestroyAllUnit();
        DestroyAllSurface();
        DestroyAllShadow();
        _loadedLevel = null;
    }
    [Button]
    [BoxGroup("Grid Utilities/Horizontal/1")]
    private void SetObjectToContainer()
    {
        // Find object with GridSurface and not in surfaceContainer, then set parent to surfaceContainer
        GridSurface[] gridSurfaces = FindObjectsOfType<GridSurface>();
        foreach (GridSurface gridSurface in gridSurfaces)
        {
            if (gridSurface.Tf.parent != surfaceContainer) gridSurface.Tf.parent = surfaceContainer;
        }
        // Find object with GridUnit and not in unitContainer or shadowContainer, then set parent to unitContainer
        GridUnit[] gridUnits = FindObjectsOfType<GridUnit>();
        foreach (GridUnit gridUnit in gridUnits)
        {
            if (gridUnit.Tf.parent != unitContainer && gridUnit.Tf.parent != shadowContainer) gridUnit.Tf.parent = unitContainer;
        }
    }

    [BoxGroup("Grid Utilities/Horizontal/1")]
    [Button]
    private void DestroyAllHintLine()
    {
        foreach (Transform child in hintLineContainer)
        {
            DestroyImmediate(child.gameObject);
        }
        hintLineObj.Clear();
    }

    [BoxGroup("Grid Utilities/Horizontal/2")]
    [Button]
    private void DestroyAllShadow()
    {
        GridUnit[] gridUnits = shadowContainer.GetComponentsInChildren<GridUnit>();
        foreach (GridUnit gridUnit in gridUnits) DestroyImmediate(gridUnit.gameObject);
    }

    [BoxGroup("Grid Utilities/Horizontal/2")]
    [Button]
    private void DestroyAllUnit()
    {
        GridUnit[] gridUnits = unitContainer.GetComponentsInChildren<GridUnit>();
        foreach (GridUnit gridUnit in gridUnits) DestroyImmediate(gridUnit.gameObject);
    }

    [BoxGroup("Grid Utilities/Horizontal/2")]
    [Button]
    private void DestroyAllSurface()
    {
        GridSurface[] gridSurfaces = surfaceContainer.GetComponentsInChildren<GridSurface>();
        foreach (GridSurface gridSurface in gridSurfaces) DestroyImmediate(gridSurface.gameObject);
    }

    [ContextMenu("Add Hint Line Object List")]
    private void AddHintLineObjectList()
    {
        // if hintLineString is not empty, add a ";"
        if (hintLineString != "") hintLineString += " ; ";
        int countChild = hintLineContainer.childCount;
        for (int i = 0; i < countChild; i++)
        {
            hintLineObj.Add(hintLineContainer.GetChild(i));
            // store the x and z pos of this obj
            hintLineString += hintLineObj[i].position.x + " " + hintLineObj[i].position.z;
            // if not last, add a ";"
            if (i != countChild - 1) hintLineString += " ; ";
        }
    }

    [ContextMenu("Add New Hint Line Object List")]
    private void AddNewHintLineObjectList()
    {
        // add all object in hintLintContainer to hintLineObj
        hintLineObj.Clear();
        hintLineString = "";
        int countChild = hintLineContainer.childCount;
        for (int i = 0; i < countChild; i++)
        {
            hintLineObj.Add(hintLineContainer.GetChild(i));
            // store the x and z pos of this obj
            hintLineString += hintLineObj[i].position.x + " " + hintLineObj[i].position.z;
            // if not last, add a ";"
            if (i != countChild - 1) hintLineString += " ; ";
        }
    }

    [ContextMenu("Load Levels")]
    private void LoadLevels()
    {
        if (_loadedLevel != null) UnLoadLevels();
        // Find the level index from name in DataManager
        LevelType levelType = VerifyLevelName(name);
        if (levelType == LevelType.None)
        {
            DevLog.Log(DevId.Hoang, "Name is not in the correct format");
            return;
        }
        TextAsset textAsset = Resources.Load<TextAsset>(GetPathFromName(levelType, name));
        // if not found, return
        if (!DataManager.Ins.HasGridTextData(levelType, textAsset))
        {
            DevLog.Log(DevId.Hoang, $"Not found level {name} in GridData");
            return;
        }
        int index = DataManager.Ins.GetGridTextDataIndex(levelType, textAsset);
        // if index < 0, return
        if (index < 0)
        {
            DevLog.Log(DevId.Hoang, $"Not found level ${name} in GridData");
            return;
        }
        // Create a new empty object
        GameObject levelObject = new(name);
        _loadedLevel = new Level(levelType, index, levelObject.transform);
        // Set all GridSurface to surfaceContainer
        GridSurface[] gridSurfaces = FindObjectsOfType<GridSurface>();
        foreach (GridSurface gridSurface in gridSurfaces)
        {
            if (gridSurface.Tf.parent != surfaceContainer) gridSurface.Tf.parent = surfaceContainer;
        }
        // Set all GridUnit from _loadedLevel.LevelUnitData.unit to unitContainer
        foreach (GridUnit unit in _loadedLevel.UnitDataList.Select(levelUnitData => levelUnitData.unit).Where(unit => unit.Tf.parent != unitContainer))
        {
            unit.Tf.parent = unitContainer;
            // Change the rotation of unit in Skin to the rotation in Tf
            // Get child with name Skin
            Transform skin = unit.Tf.Find("Skin");
            if (skin == null) continue;
            // Set the local rotation Tf to the local rotation of Skin
            unit.Tf.localRotation = skin.localRotation;
            // Set the local rotation of Skin to identity
            skin.localRotation = Quaternion.identity;
        }
        // Set all GridUnit from _loadedLevel.ShadowUnitList to shadowContainer
        if (_loadedLevel.ShadowUnitList.Count <= 0)
        {
            // Destroy the empty object
            DestroyImmediate(levelObject);
            return;
        }
        _loadedLevel.ShadowUnitList[0].SetAlphaTransparency(0.5f);
        foreach (GridUnit shadowUnit in _loadedLevel.ShadowUnitList.Where(shadowUnit => shadowUnit.Tf.parent != shadowContainer))
        {
            shadowUnit.Tf.parent = shadowContainer;
            shadowUnit.gameObject.SetActive(true);
        }
        // Destroy the empty object
        DestroyImmediate(levelObject);
    }

    private void UnLoadLevels()
    {
        SetObjectToContainer();
        DestroyAll();
        _loadedLevel = null;
    }
    
    [ContextMenu("Save JSON")]
    private void SaveLevelAsJson()
    {
        SetObjectToContainer();

        #region Verify

        // Verify name
        switch (mapLevelName)
        {
            case null:
            case " ":
            case "":
                Debug.LogError("Level name can not be empty");
                return;
        }

        LevelType levelType = VerifyLevelName(mapLevelName);
        if (levelType == LevelType.None)
        {
            Debug.LogError("Level name must be in the format Lvl_number or Lvl_DC_number or Lvl_S_number");
            return;
        }

        // Verify Grid Surface
        GridSurface[] gridSurfaces = surfaceContainer.GetComponentsInChildren<GridSurface>();
        if (gridSurfaces.Length == 0)
        {
            Debug.LogError("Grid must have at least 1 surface");
            return;
        }
        // Verify Grid Unit (Player has in this level)
        GridUnit[] gridUnits = unitContainer.GetComponentsInChildren<GridUnit>();
        // Check if have Player
        if (gridUnits.All(gridUnit => gridUnit.PoolType != PoolType.Player))
        {
            Debug.LogError("Grid must have at least 1 Player");
            return;
        }

        GridUnit[] shadowUnits = shadowContainer.GetComponentsInChildren<GridUnit>();

        #endregion

        #region Setup GridMapSize

        int minX = int.MaxValue;
        int minZ = int.MaxValue;
        int maxX = int.MinValue;
        int maxZ = int.MinValue;
        foreach (GridSurface gridSurface in gridSurfaces)
        {
            Vector3 position = gridSurface.Tf.position;
            if (position.x < minX) minX = (int)Math.Round(position.x);
            if (position.z < minZ) minZ = (int)Math.Round(position.z);
            if (position.x > maxX) maxX = (int)Math.Round(position.x);
            if (position.z > maxZ) maxZ = (int)Math.Round(position.z);
        }
        int offsetS = offsetSurfaceWithFirstCell * 2 + 1;

        #region Set MinX and MaxX

        if (minX < offsetS)
        {
            int offsetX = offsetS - minX;
            foreach (GridSurface gridSurface in gridSurfaces)
            {
                Vector3 position = gridSurface.Tf.position;
                position.x += offsetX;
                gridSurface.Tf.position = position;
            }

            foreach (GridUnit gridUnit in gridUnits)
            {
                Vector3 position = gridUnit.Tf.position;
                position.x += offsetX;
                gridUnit.Tf.position = position;
            }

            // ReSharper disable once RedundantAssignment
            minX += offsetX;
            maxX += offsetX;
        }
        else if (minX > offsetS)
        {
            int offsetX = minX - offsetS;
            foreach (GridSurface gridSurface in gridSurfaces)
            {
                Vector3 position = gridSurface.Tf.position;
                position.x -= offsetX;
                gridSurface.Tf.position = position;
            }

            foreach (GridUnit gridUnit in gridUnits)
            {
                Vector3 position = gridUnit.Tf.position;
                position.x -= offsetX;
                gridUnit.Tf.position = position;
            }

            // ReSharper disable once RedundantAssignment
            minX -= offsetX;
            maxX -= offsetX;
        }

        maxX += offsetS - 1; // add five more cell to maxX and maxY

        #endregion

        #region Set MinZ and MaxZ

        if (minZ < offsetS)
        {
            int offsetZ = offsetS - minZ;
            foreach (GridSurface gridSurface in gridSurfaces)
            {
                Vector3 position = gridSurface.Tf.position;
                position.z += offsetZ;
                gridSurface.Tf.position = position;
            }

            foreach (GridUnit gridUnit in gridUnits)
            {
                Vector3 position = gridUnit.Tf.position;
                position.z += offsetZ;
                gridUnit.Tf.position = position;
            }

            // ReSharper disable once RedundantAssignment
            minZ += offsetZ;
            maxZ += offsetZ;
        }
        else if (minZ > offsetS)
        {
            int offsetZ = minZ - offsetS;
            foreach (GridSurface gridSurface in gridSurfaces)
            {
                Vector3 position = gridSurface.Tf.position;
                position.z -= offsetZ;
                gridSurface.Tf.position = position;
            }

            foreach (GridUnit gridUnit in gridUnits)
            {
                Vector3 position = gridUnit.Tf.position;
                position.z -= offsetZ;
                gridUnit.Tf.position = position;
            }

            // ReSharper disable once RedundantAssignment
            minZ -= offsetZ;
            maxZ -= offsetZ;
        }

        maxZ += offsetS - 1;
        const int cellOffset = 1;
        maxX = (maxX + cellOffset) / 2;
        maxZ = (maxZ + cellOffset) / 2;

        #endregion

        // Set gridMapSize

        Vector2Int size = new(maxX, maxZ);

        #endregion

        #region Setup GridSurfaceData

        // Create a list of GridSurfaceData
        List<RawLevelData.GridSurfaceData> gridSurfaceDataList = new();

        // Loop all gridSurface 
        foreach (GridSurface gridSurface in gridSurfaces)
        {
            // Save the position of gridSurface
            Vector3 position = gridSurface.Tf.position;
            // Get the x and z index of gridSurface
            int x = (int)(position.x - 1) / 2;
            int z = (int)(position.z - 1) / 2;
            // Save the data to gridSurfaceData
            int m = -1;
            if (gridSurface is GroundSurface groundSurface)
            {
                if (groundSurface.groundMaterialEnum == MaterialEnum.None)
                {
                    // Random from 0 to last index of MaterialEnum
                    m = UnityEngine.Random.Range(0, Enum.GetValues(typeof(MaterialEnum)).Length - 1);
                }
                else
                {
                    m = (int)groundSurface.groundMaterialEnum;
                }
            }
            RawLevelData.GridSurfaceData gridSurfaceData = new()
            {
                p = new Vector2Int(x, z),
                t = (int)gridSurface.PoolType,
                d = (int)BuildingUnitData.GetDirection(gridSurface.Tf.eulerAngles.y),
                // save the ground material if it is ground surface, and if MaterialEnum is None, random it from 0 to 2
                m = m
            };
            // Add gridSurfaceData to gridSurfaceDataList
            gridSurfaceDataList.Add(gridSurfaceData);
        }

        #endregion

        #region Setup GridUnitData

        // Create a list of GridUnitData
        List<RawLevelData.GridUnitData> gridUnitDataList = new();

        // Loop all gridUnit

        foreach (GridUnit gridUnit in gridUnits)
        {
            // Save the position of gridUnit
            Vector3 position = gridUnit.Tf.position;
            // Get the x and z index of gridUnit
            int x = (int)(position.x - 1) / 2;
            int z = (int)(position.z - 1) / 2;
            // Check if gridUnit is Rock
            if (gridUnit is Rock && isRandomRotationForRock)
            {
                // Set random rotation for rock (0, 90, 180, 270)
                gridUnit.Tf.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 4) * 90, 0);
            }
            // Save the data to gridUnitData
            RawLevelData.GridUnitData gridUnitData = new()
            {
                c = new Vector2Int(x, z),
                t = (int)gridUnit.PoolType,
                // rotationDirection = (int)gridUnit.SkinRotationDirection
                // TEST
                d = (int)BuildingUnitData.GetDirection(gridUnit.Tf.eulerAngles.y),
            };
            // Add gridUnitData to gridUnitDataList
            gridUnitDataList.Add(gridUnitData);
        }

        #endregion

        #region Setup ShadowUnitData

        // Create a list of ShadowUnitData
        List<RawLevelData.ShadowUnitData> shadowUnitDataList = new();

        // Loop all shadowUnit
        foreach (GridUnit shadowUnit in shadowUnits)
        {
            // Check if unit is down
            Vector3 rotationAngle = shadowUnit.Tf.eulerAngles;
            // if rotation round int of x and z divided by 180, then it is up
            bool isUp = Mathf.RoundToInt(rotationAngle.x) % 180 == 0
                        && Mathf.RoundToInt(rotationAngle.z) % 180 == 0;
            // if not up, add the offset
            if (!isUp)
            {
                // if y position is floating number (not near integer with 0.05f), then it is done already, and we dont do anything
                // but if the y position is near integer with 0.05f, then we need to add the offset
                Vector3 position = shadowUnit.Tf.position;
                if (Mathf.Abs(position.y - Mathf.RoundToInt(position.y)) < 0.05f)
                {
                    position.y -= shadowUnit.yOffsetOnDown;
                    // Round the y position to 2 decimal
                    position.y = (float)Math.Round(position.y, 2);
                }
            }

            // Save the data to shadowUnitData
            RawLevelData.ShadowUnitData shadowUnitData = new()
            {
                p = shadowUnit.Tf.position,
                rA = rotationAngle,
                t = (int)shadowUnit.PoolType
            };
            // Add shadowUnitData to shadowUnitDataList
            shadowUnitDataList.Add(shadowUnitData);
        }

        #endregion

        #region Setup HintTrailData

        // Create a list of HintTrailData
        List<RawLevelData.HintTrailData> hintTrailDataList = new();

        bool isNotVerify = hintLineString.Any(c => !char.IsDigit(c) && c != ' ' && c != ';');

        // Check the hintLineString
        if (!isNotVerify)
        {
            // separate the hintLineString to each Vector3(x,3f,z)
            string[] splitHintLineString = hintLineString.Split(" ; ");
            // if hintLineString is empty, return
            if (splitHintLineString.Length == 0) return;
            foreach (string s in splitHintLineString)
            {
                string[] split = s.Split(' ');
                if (split.Length != 2) continue;
                if (!float.TryParse(split[0], out float x)) continue;
                if (!float.TryParse(split[1], out float z)) continue;
                RawLevelData.HintTrailData hintTrailData = new()
                {
                    p = new Vector2(x, z)
                };
                hintTrailDataList.Add(hintTrailData);
            }
        }

        #endregion

        RawLevelData levelData = new()
        {
            s = size,
            sfD = gridSurfaceDataList.ToArray(),
            uD = gridUnitDataList.ToArray(),
            suD = shadowUnitDataList.ToArray(),
            htD = hintTrailDataList.ToArray()
        };
        // Convert levelData to json
        DevLog.Log(DevId.Hoang, "Save levelData: " + levelData.sfD.Length);
        string json = JsonUtility.ToJson(levelData);
        // Format json
        DevLog.Log(DevId.Hoang, "Save json: " + json);
        // Save json to file
        string path = GetLongPathFromName(levelType, mapLevelName);
        DevLog.Log(DevId.Hoang, "Save to path: " + path);
        File.WriteAllText(path, json);
        // check if gridData contain this textAsset with this name, if not, add it 
        TextAsset textAsset = Resources.Load<TextAsset>(GetPathFromName(levelType, mapLevelName));
        if (textAsset == null) return;
        if (DataManager.Ins.HasGridTextData(levelType, textAsset)) return;
        // Add new txt file to gridData from DataManager from path 
        DataManager.Ins.AddGridTextData(levelType, textAsset);
    }

    [ContextMenu("Test Hint Trail")]
    private void TestHintTrail()
    {
        // separate the hintLineString to each Vector3(x,3f,z)
        string[] splitHintLineString = hintLineString.Split(" ; ");
        // if hintLineString is empty, return
        if (splitHintLineString.Length == 0 || hintLineTrail == null) return;
        List<Vector3> hintLinePosList = new();
        foreach (string s in splitHintLineString)
        {
            string[] split = s.Split(' ');
            if (split.Length != 2) continue;
            if (!float.TryParse(split[0], out float x)) continue;
            if (!float.TryParse(split[1], out float z)) continue;
            hintLinePosList.Add(new Vector3(x, 3f, z));
        }
        // Make the hintLineTrail follow the hintLinePosList
        hintLineTrail.TestHintMoving(hintLinePosList);
    }

    private static string GetPathFromName(LevelType type, string levelName)
    {
        return "Level/" + type switch
        {
            LevelType.Normal => "Normal/" + levelName,
            LevelType.DailyChallenger => "DailyChallenger/" + levelName,
            LevelType.Secret => "Secret/" + levelName,
            LevelType.None => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static string GetLongPathFromName(LevelType type, string levelName)
    {
        return "Assets/_Game/Resources/" + GetPathFromName(type, levelName) + ".json";
    }

    private static LevelType VerifyLevelName(string verifyName)
    {
        // if name is null or empty, return
        if (string.IsNullOrEmpty(verifyName)) return LevelType.None;
        // if name is not start with "Lvl_", return
        if (!verifyName.StartsWith("Lvl_")) return LevelType.None;
        // If name is in the format "Lvl_number", return LevelType.Normal
        // If name is in the format "Lvl_DC_number", return LevelType.DailyChallenger
        // If name is in the format "Lvl_S_number", return LevelType.Secret
        string[] split = verifyName.Split('_');
        return split.Length switch
        {
            // if split length is 2
            // if split[0] is "Lvl" and split[1] is a number, return LevelType.Normal
            2 when split[0] == "Lvl" && int.TryParse(split[1], out int _) => LevelType.Normal,
            2 => LevelType.None,
            // if split length is 3
            // if split[0] is "Lvl" and split[1] is "DC" and split[2] is a number, return LevelType.DailyChallenger
            3 when split[0] == "Lvl" && split[1] == "DC" && int.TryParse(split[2], out int _) => LevelType
                .DailyChallenger,
            // if split[0] is "Lvl" and split[1] is "S" and split[2] is a number, return LevelType.Secret
            3 when split[0] == "Lvl" && split[1] == "S" && int.TryParse(split[2], out int _) => LevelType.Secret,
            3 => LevelType.None,
            _ => LevelType.None
        };
    }
}
