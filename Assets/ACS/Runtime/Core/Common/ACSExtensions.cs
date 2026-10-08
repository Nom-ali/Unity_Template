using System.Collections.Generic;
using UnityEngine;

namespace ACS
{
    public delegate void SimpleCallback();
    public delegate void SimpleBoolCallback(bool value);

    public static class ACSExtensions
    {
        public static Vector3 SetZ(this Vector3 value, float z)
        {
            value.z = z;
            return value;
        }

        public static Vector3 Divide(this Vector3 value, Vector3 divisor)
        {
            value.x = divisor.x != 0 ? value.x / divisor.x : 0f;
            value.y = divisor.y != 0 ? value.y / divisor.y : 0f;
            value.z = divisor.z != 0 ? value.z / divisor.z : 0f;
            return value;
        }

        public static Color SetAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static bool IsInRange<T>(this IList<T> list, int index)
        {
            return list != null && index >= 0 && index < list.Count;
        }
    }
}


