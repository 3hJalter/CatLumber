using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VinhLB
{
    public class FlexibleGridLayout : LayoutGroup
    {
        public enum FitType
        {
            Uniform = 0,
            Width = 1,
            Height = 2,
            FixedRows = 3,
            FixedColumns = 4
        }
        
        [SerializeField]
        private FitType _fitType;
        [SerializeField]
        private int _rows;
        [SerializeField]
        private int _columns;
        [SerializeField]
        private Vector2 _cellSize;
        [SerializeField]
        private Vector2 _spacing;
        [SerializeField]
        private bool _fitX;
        [SerializeField]
        private bool _fitY;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            if (_fitType == FitType.Uniform || _fitType == FitType.Width || _fitType == FitType.Height)
            {
                _fitX = true;
                _fitY = true;
                
                float sqrt = Mathf.Sqrt(transform.childCount);
                _rows = Mathf.CeilToInt(sqrt);
                _columns = Mathf.CeilToInt(sqrt);
            }

            if (_fitType == FitType.Width || _fitType == FitType.FixedColumns)
            {
                _rows = Mathf.CeilToInt((float)transform.childCount / _columns);
            }
            else if (_fitType == FitType.Height || _fitType == FitType.FixedRows)
            {
                _columns = Mathf.CeilToInt((float)transform.childCount / _rows);
            }

            float parentWidth = rectTransform.rect.width;
            float parentHeight = rectTransform.rect.height;

            float cellWidth = (parentWidth / _columns) - (_spacing.x / _columns * (_columns - 1)) -
                              ((float)padding.left / _columns) - ((float)padding.right / _columns);
            float cellHeight = (parentHeight / _rows) - (_spacing.y / _rows * (_rows - 1)) -
                               ((float)padding.top / _rows) - ((float)padding.bottom / _rows);

            _cellSize.x = _fitX ? cellWidth : _cellSize.x;
            _cellSize.y = _fitY ? cellHeight : _cellSize.y;

            int columnCount = 0;
            int rowCount = 0;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                rowCount = i / _columns;
                columnCount = i % _columns;

                RectTransform item = rectChildren[i];

                float xPos = (_cellSize.x * columnCount) + (_spacing.x * columnCount) + padding.left;
                float yPos = (_cellSize.y * rowCount) + (_spacing.y * rowCount) + padding.top;

                SetChildAlongAxis(item, 0, xPos, _cellSize.x);
                SetChildAlongAxis(item, 1, yPos, _cellSize.y);
            }
        }

        public override void CalculateLayoutInputVertical()
        {
            
        }

        public override void SetLayoutHorizontal()
        {
            
        }

        public override void SetLayoutVertical()
        {
            
        }
    }
}