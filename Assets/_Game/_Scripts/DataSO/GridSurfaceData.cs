using System.Collections.Generic;
using _Game.DesignPattern;
using _Game.GameGrid.GridSurface;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Game.Data
{
    [CreateAssetMenu(fileName = "GridSurfaceData", menuName = "ScriptableObjects/GridSurfaceData", order = 1)]
    public class GridSurfaceData : SerializedScriptableObject
    {
        [BoxGroup("Center")] [Title("Center")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _centerDic = new();

        [BoxGroup("Corner")] [Title("Corner Bottom Left")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _cornerBottomLeftDic = new();

        [BoxGroup("Corner")] [Title("Corner Bottom Right")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _cornerBottomRightDic = new();

        [BoxGroup("Corner")] [Title("Corner Top left")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _cornerTopLeftDic = new();

        [BoxGroup("Corner")] [Title("Corner Top Right")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _cornerTopRightDic = new();

        [BoxGroup("Edge")] [Title("Edge Bottom")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _edgeBottomDic = new();

        [BoxGroup("Edge")] [Title("Edge Left")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _edgeLeftDic = new();

        [BoxGroup("Edge")] [Title("Edge Right")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _edgeRightDic = new();

        [BoxGroup("Edge")] [Title("Edge Top")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _edgeTopDic = new();

        [BoxGroup("4 Side")] [Title("4 Side")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _fourSideDic = new();

        [BoxGroup("3 Side")] [Title("3 Side Bottom Left")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _threeSideBottomLeftDic = new();

        [BoxGroup("3 Side")] [Title("3 Side Bottom Right")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _threeSideBottomRightDic = new();

        [BoxGroup("3 Side")] [Title("3 Side Top Left")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _threeSideTopLeftDic = new();

        [BoxGroup("3 Side")] [Title("3 Side Top Right")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _threeSideTopRightDic = new();

        [BoxGroup("2 Side")] [Title("2 Side Horizontal")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _twoSideHorizontalDic = new();

        [BoxGroup("2 Side")] [Title("2 Side Vertical")] [SerializeField]
        private Dictionary<PoolType, GridSurface> _twoSideVerticalDic = new();
    }
}
