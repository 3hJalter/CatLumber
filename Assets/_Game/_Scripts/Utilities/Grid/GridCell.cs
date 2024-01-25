using System;
using MapEnum;
using UnityEngine;

namespace _Game.Utilities.Grid
{
    public class GridCell<T> : IOriginator
    {
        protected const int MIN = 0;
        protected const int MAX = 100;
        protected T data;
        public GridPlane GridPlaneType;
        public event Action<int, int, bool> OnValueChange;
        private Vector3 worldPos;
        [SerializeField] private float worldX;
        [SerializeField] private float worldY;

        protected int x;
        protected int y;

        protected GridCell()
        {
        }

        public GridCell(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        protected GridCell(GridCell<T> copy)
        {
            x = copy.x;
            y = copy.y;
            Size = copy.Size;
            worldX = copy.worldX;
            worldY = copy.worldY;
            worldPos = copy.worldPos;
            GridPlaneType = copy.GridPlaneType;
        }

        public int X => x;
        public int Y => y;
        public float WorldX => worldX;
        public float WorldY => worldY;
        public Vector3 WorldPos => worldPos;
        public T Data => data;

        public float Size { get; set; }

        public void SetCellValue(T valueIn)
        {
            data = valueIn;
        }


        public Vector2Int GetCellPosition()
        {
            return new Vector2Int(x, y);
        }

        public void SetCellPosition(int xIn, int yIn)
        {
            x = xIn;
            y = yIn;
        }

        public void UpdateWorldPosition(float originX, float originY)
        {
            worldX = originX + (x + 0.5f) * Size;
            worldY = originY + (y + 0.5f) * Size;

            switch (GridPlaneType)
            {
                case GridPlane.XY:
                    worldPos.Set(worldX, worldY, 0);
                    break;
                case GridPlane.XZ:
                    worldPos.Set(worldX, 0, worldY);
                    break;
                case GridPlane.YZ:
                    worldPos.Set(0, worldX, worldY);
                    break;
            }
        }

        public override string ToString()
        {
            return data.ToString();
        }

        public virtual void ValueChange(bool isRevert = false)
        {
            OnValueChange?.Invoke(x, y, isRevert);
        }

        public virtual IMemento Save() 
        { 
            return null; 
        }
    }
}
