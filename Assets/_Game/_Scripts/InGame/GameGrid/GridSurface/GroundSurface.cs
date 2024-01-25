using _Game.Data;
using _Game.Managers;
using UnityEngine;
using VinhLB;

namespace _Game.GameGrid.GridSurface
{
    public class GroundSurface : GridSurface
    {
        [SerializeField] private MeshRenderer grassRenderer;
        [SerializeField] protected MeshRenderer groundMeshRenderer;
        
        public MaterialEnum groundMaterialEnum = MaterialEnum.None;
        
        public override void OnInit(Direction rotateDirection = Direction.Forward, MaterialEnum materialEnum = MaterialEnum.None)
        {
            transform.localRotation = Quaternion.Euler(0, BuildingUnitData.GetRotationAngle(rotateDirection), 0);
            // Change material in mesh renderer
            if (groundMeshRenderer == null) return;
            groundMaterialEnum = materialEnum;
            groundMeshRenderer.material = DataManager.Ins.GetSurfaceMaterial(materialEnum);
            if (groundMaterialEnum is MaterialEnum.None || grassRenderer == null) return;
            grassRenderer.material = DataManager.Ins.GetGrassMaterial(materialEnum);
        }
        
        [ContextMenu("Set Material to Ground")]
        public void SetMaterialToGround()
        {
            if (groundMeshRenderer == null) return;
            groundMeshRenderer.material = DataManager.Ins.GetSurfaceMaterial(groundMaterialEnum);
        }
    }
}
