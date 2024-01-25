using UnityEngine;

namespace VinhLB
{
    public abstract class TabPage : HMonoBehaviour
    {
        public virtual void Open()
        {
            gameObject.SetActive(true);
        }

        public virtual void Close()
        {
            gameObject.SetActive(false);
        }
    }
}