namespace MapEnum
{
    public enum GridPlane
    {
        XY = 0,
        XZ = 1,
        YZ = 2
    }
}

public enum Direction
{
    None = -1,
    Left = 0,
    Right = 1,
    Forward = 2,
    Back = 3
}
public enum InputAction
{
    None = -1,
    ButtonDown = 0,
    ButtonUp = 1,
    ButtonHold = 2,
}

public enum CONDITION
{
    NONE = -1,
    BE_BLOCKED_BY_TREE_ROOT = 0,
    ROLL_AROUND_BLOCK_CHUMP = 1,
}

namespace TweenTypeEnum
{
    public enum EasingType
    {
        Linear = 1,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InExpo,
        OutExpo,
        InOutExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        InElastic,
        OutElastic,
        InOutElastic,
        InBack,
        OutBack,
        InOutBack,
        InBounce,
        OutBounce,
        InOutBounce,
        Flash,
        InFlash,
        OutFlash,
        InOutFlash,
        Custom
    }
}

namespace GameGridEnum
{
    public enum HeightLevel
    {
        None = -1,
        Zero = 0,
        ZeroPointFive = 1,
        One = 2,
        OnePointFive = 3,
        Two = 4,
        TwoPointFive = 5,
        Three = 6,
        ThreePointFive = 7,
        Four = 8,
        FourPointFive = 9
    }

    public enum GridSurfaceType
    {
        // Default value of all GameGridCell is Water, so no need to create a WaterGridSurface
        // Other GridSurfaceType must be created when needed (e.g. GroundGridSurface) to overwrite the default value
        Water = 0,
        Ground = 1
    }

    public enum GridUnitStaticType
    {
        None = -1,
        RockShort = 0,
        RockHigh = 1,
        TreeRoot = 2,
        TreeShort = 3,
        TreeHigh = 4,
        FinalPoint = 5,
        FloatingChest = 6, 
        ChestButtonUnit = 7,
        RockUnderwaterSmall = 8,
        RockUnderWaterBig = 9,
    }

    public enum GridUnitDynamicType
    {
        None = -1,
        Player = 0,
        ChumpShort = 1,
        ChumpHigh = 2,
        Raft = 3,
        RaftLong = 4,
        Box = 5,
        Bomb = 6,
        ButtonUnit = 7,
        Archer_Enemy = 100,
        Mage_Enemy = 101,
    }
    
    public enum VFXType
    {
        Dust = 0,
        WaterSplash = 1,
        LeafExplosion = 2,
        DangerIndicator = 3,
        BombExplosion = 4,
        SmokeExplosion = 5,
    }
}
