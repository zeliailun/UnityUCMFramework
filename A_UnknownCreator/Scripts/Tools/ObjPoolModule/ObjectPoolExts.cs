using UnityEngine;

namespace UnknownCreator.Modules
{
    public static class ObjectPoolExts
    {
        public static ObjectProxy<T> Proxy<T>(this T obj) where T : class
        {
            return new ObjectProxy<T>().Set(obj, null);
        }

        public static ObjectProxy<object> Proxy(this object obj)
        {
            return new ObjectProxy<object>().Set(obj, null);
        }

        public static ObjectProxy<GameObject> Proxy(this GameObject obj, string name)
        {
            return new ObjectProxy<GameObject>().Set(obj, name);
        }
    }
}