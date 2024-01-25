using UnityEngine.UI;

namespace HControls
{
    public class HEmptyGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
