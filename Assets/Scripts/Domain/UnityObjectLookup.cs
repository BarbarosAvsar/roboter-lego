using UnityEngine;

namespace RoboterLego.Domain
{
    public static class UnityObjectLookup
    {
        public static T FindFirst<T>() where T : Object
        {
#if UNITY_2022_2_OR_NEWER
            var items = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return items != null && items.Length > 0 ? items[0] : null;
#else
#pragma warning disable 618
            return Object.FindObjectOfType<T>();
#pragma warning restore 618
#endif
        }

        public static T[] FindAll<T>() where T : Object
        {
#if UNITY_2022_2_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
#pragma warning disable 618
            return Object.FindObjectsOfType<T>();
#pragma warning restore 618
#endif
        }
    }
}
