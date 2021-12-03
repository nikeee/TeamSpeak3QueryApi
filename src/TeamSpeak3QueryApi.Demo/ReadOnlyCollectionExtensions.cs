using System;
using System.Collections.Generic;

namespace TeamSpeak3QueryApi.Net.Demo;

internal static class ReadOnlyCollectionExtensions
{
    public static void ForEach<T>(this IReadOnlyCollection<T> collection, Action<T> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        foreach (var i in collection)
            action(i);
    }
}
