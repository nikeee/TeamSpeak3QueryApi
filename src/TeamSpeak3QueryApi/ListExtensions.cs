using System.Collections.Generic;

namespace TeamSpeak3QueryApi.Net;

internal static class ListExtensions
{
    public static void MoveToBottom<T>(this IList<T> list, T obj)
    {
        list.Remove(obj);
        list.Add(obj);
    }
}
