using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VinhLB
{
    public static class Extensions
    {
        #region Transform
        public static void SetLayer(this Transform transform, int layer, bool recursive = false)
        {
            transform.gameObject.layer = layer;

            if (recursive)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    transform.GetChild(i).SetLayer(layer, true);
                }
            }
        }

        public static void ClearChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }
        #endregion

        #region List
        public static void Shift<T>(this List<T> list, int amount)
        {
            List<T> tempList = list.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                int newValueIndex = (i + amount) % list.Count;
                list[i] = tempList[newValueIndex];
            }
        }
        #endregion
    }
}