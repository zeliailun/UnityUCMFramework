using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public static class ListGlobals
    {
        public static List<T> CopyToNewList<T>(this List<T> list1)
        {
            if (list1 is null) return null;
            List<T> list2 = new(list1.Count);
            list2.AddRange(list1);
            return list2;
        }


        public static void Shuffle<T>(this List<T> list)
        {
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, n);
                (list[randomIndex], list[i]) = (list[i], list[randomIndex]);
            }
        }

        public static bool IsValid<T>(this List<T> list)
        {
            return list != null && list.Count > 0;
        }
    }
}