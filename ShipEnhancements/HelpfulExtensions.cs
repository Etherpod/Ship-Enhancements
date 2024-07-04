using System;
using System.Collections.Generic;
using System.Linq;

namespace ShipEnhancements;

public static class HelpfulExtensions
{
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action) =>
        source.ForEach((value, _) => action.Invoke(value));

    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        var values = source as T[] ?? source.ToArray();
        for (var i = 0; i < values.Length; ++i)
        {
            action.Invoke(values[i], i);
        }
        return values;
    }
}
