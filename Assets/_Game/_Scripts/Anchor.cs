using _Game.GameGrid.Unit;
using UnityEngine;

public class Anchor : HMonoBehaviour
{
    public void ChangeAnchorPos(GridUnit gridUnitI, Direction direction)
    {
        Vector3Int unitSize = gridUnitI.Size;
        CenterXZAnchorFromUnit();
        float xOffset = (float)unitSize.x / 2 * Constants.CELL_SIZE;
        float zOffset = (float)unitSize.z / 2 * Constants.CELL_SIZE;
        Vector2Int dirVector = Constants.DirVector[direction];
        Tf.position += new Vector3(dirVector.x * xOffset, 0, dirVector.y * zOffset);
        return;

        void CenterXZAnchorFromUnit()
        {
            Vector3 mainCellWorldPos = gridUnitI.MainCell.WorldPos;
            float xPos = 0.5f * (unitSize.x - 1) * Constants.CELL_SIZE;
            float zPos = 0.5f * (unitSize.z - 1) * Constants.CELL_SIZE;
            Tf.position = new Vector3(mainCellWorldPos.x + xPos, Tf.position.y, mainCellWorldPos.z + zPos);
        }
    }
}
