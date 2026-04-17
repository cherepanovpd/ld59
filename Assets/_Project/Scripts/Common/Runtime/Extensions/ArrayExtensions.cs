using UnityEngine;

namespace Common.Runtime.Extensions
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Возвращает случайный элемент из массива
        /// </summary>
        public static T GetRandom<T>(this T[] array)
        {
            if (array == null || array.Length == 0)
            {
                Debug.LogWarning("Попытка получить случайный элемент из пустого массива!");
                return default(T);
            }
        
            return array[Random.Range(0, array.Length)];
        }
    
        /// <summary>
        /// Возвращает случайный элемент из списка
        /// </summary>
        public static T GetRandom<T>(this System.Collections.Generic.List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                Debug.LogWarning("Попытка получить случайный элемент из пустого списка!");
                return default(T);
            }
        
            return list[Random.Range(0, list.Count)];
        }
    }
}