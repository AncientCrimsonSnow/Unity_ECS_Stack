using UnityEngine;

namespace Project
{
    public static class Extensions
    {
        public static void Shuffle<T> (T[] array)
        {
            for (var i = 0; i < array.Length; i++) {
                var rnd = Random.Range(0, array.Length);
                (array[rnd], array[i]) = (array[i], array[rnd]);
            }
        }
    }
}