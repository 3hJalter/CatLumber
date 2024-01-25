using System;
using UnityEngine;

namespace _Game._Scripts.Utilities
{
    public static class HUtilities
    {
        public static Vector3 Change(this Vector3 org, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? org.x, y ?? org.y, z ?? org.z);
        }

        public static Vector3Int Change(this Vector3Int org, int? x = null, int? y = null, int? z = null)
        {
            return new Vector3Int(x ?? org.x, y ?? org.y, z ?? org.z);
        }

        public static void DoAfterFrames(ref int frames, Action action, Action onWait = null)
        {
            if (frames > 0)
            {
                frames--;
                onWait?.Invoke();
            }
            else
            {
                action();
            }
        }

        public static void DoAfterSeconds(ref float time, Action action, Action onWait = null)
        {
            if (time > 0)
            {
                time -= Time.deltaTime;
                onWait?.Invoke();
            }
            else
            {
                action();
                time = 0;
            }
        }
    }
}
