using System;
using System.Collections.Generic;
using GameGridEnum;
using UnityEngine;

public static class Constants
{
    public const int CELL_SIZE = 2;

    // String Constants

    /// <summary>
    ///     Player Prefab Path
    /// </summary>
    public const string LEVEL_INDEX = "levelIndex";

    public const string TUTORIAL_INDEX = "tutorialIndex";

    /// <summary>
    ///     Path to the folder in Resources
    /// </summary>
    public const string POOL_PATH = "Pool";

    public const string UI_PATH = "UI/";

    /// <summary>
    ///     Control input axis
    /// </summary>
    public const string HORIZONTAL = "Horizontal";

    public const string VERTICAL = "Vertical";
    public const float INPUT_THRESHOLD_P2 = 0.09f; // Threshold Power 2 from 0 to 1 -> Default = 0.16

    /// <summary>
    ///     Player Animation
    /// </summary>
    public const string INIT_ANIM = " ";

    public const string IDLE_ANIM = "idle";
    public const string MOVE_ANIM = "move";
    public const string JUMP_UP_ANIM = "jumpUp";
    public const string JUMP_DOWN_ANIM = "jumpDown";
    public const string INTERACT_ANIM = "interact";
    public const string PUSH_ANIM = "push";
    public const string CUT_TREE_ANIM = "cutTree";
    public const string FALLING_ANIM = "falling";
    public const string DIE_ANIM = "die";
    public const string HAPPY_ANIM = "happy";
    public const string OPEN_ANIM = "open";
    public const string ATTACK = "attack";
    
    
    public const float CUT_TREE_ANIM_TIME = 0.5f;
    public const float PUSH_ANIM_TIME = 0.4f;
    public const int WAIT_CUT_TREE_FRAMES = 15;

    /// <summary>
    ///     UI Animation Trigger
    /// </summary>
    public const string OPEN = "Open";
    public const string CLOSE = "Close";
    public const string ENTER_ANIM = "enter";
    public const string EXIT_ANIM = "exit";

    // Value Constants
    public const int UPPER_HEIGHT = 1;
    public const int BELOW_HEIGHT = 1;

    public const float DELAY_INTERACT_TIME = 0.25f;
    public const float MOVING_TIME = 0.25f;
    public const float MOVING_TIME_FAST_RATE = 1f;
    public const float FALLING_TIME = 0.2f;
    public const float POS_Y_BOTTOM = -1f;
    public const float CUT_TREE_TIME = 0.3f;
    public const float PUSH_TIME = 0.2f;

    public const HeightLevel MIN_HEIGHT = HeightLevel.Zero;
    public const HeightLevel MAX_HEIGHT = HeightLevel.FourPointFive;
    
    public const int MAX_SMALL_ISLAND_SIZE = 5;

    public const string BASE_COLOR = "_BaseColor";

    public const float TOLERANCE = 0.01f;
    
    public const float HOLD_TOUCH_TIME = 0.3f;
    public const float ZOOM_OUT_TIME = 1f;
    
    public const string NONE = "None";

    public const float DEFAULT_HINT_TRAIL_HEIGHT = 3f;
    public const int LEVEL_TIME = 30;
    public const int UNDO_COUNT = 10;
    
    public const float HINT_LINE_TRAIL_SPEED = 10f;
    
    // OLD 

    public static readonly HeightLevel MaxHeight = (HeightLevel)(Enum.GetValues(typeof(HeightLevel)).Length - 1);
    public static readonly Vector3 HorizontalSkinRotation = new(0, 0, 90);

    public static readonly Vector3 VerticalSkinRotation = new(0, -90, 90);
    //

    // Dictionary constants

    /// <summary>
    ///     Dictionary
    /// </summary>
    public static readonly Dictionary<GridSurfaceType, HeightLevel> DirFirstHeightOfSurface = new()
    {
        { GridSurfaceType.Ground, HeightLevel.One },
        { GridSurfaceType.Water, HeightLevel.Zero }
    };

    public static readonly Dictionary<Direction, Vector3> DirVector3F = new()
    {
        { Direction.None, Vector3.zero },
        { Direction.Left, Vector3.left },
        { Direction.Right, Vector3.right },
        { Direction.Forward, Vector3.forward },
        { Direction.Back, Vector3.back }
    };

    public static readonly Dictionary<Direction, Vector3Int> DirVector3 = new()
    {
        { Direction.None, Vector3Int.zero },
        { Direction.Left, Vector3Int.left },
        { Direction.Right, Vector3Int.right },
        { Direction.Forward, Vector3Int.forward },
        { Direction.Back, Vector3Int.back }
    };

    public static readonly Dictionary<Direction, Vector2Int> DirVector = new()
    {
        { Direction.None, Vector2Int.zero },
        { Direction.Left, Vector2Int.left },
        { Direction.Right, Vector2Int.right },
        { Direction.Forward, Vector2Int.up },
        { Direction.Back, Vector2Int.down }
    };
    public static readonly Dictionary<Direction, Direction> InvDirection = new()
    {
        {Direction.None, Direction.None },
        {Direction.Left, Direction.Right },
        {Direction.Right, Direction.Left },
        {Direction.Forward, Direction.Back },
        {Direction.Back, Direction.Forward },
    };
}


namespace _Game
{
    public interface IInit
    {
        public void OnInit();
    }
}
