using System;
using System.Linq;
using System.Collections.Generic;

namespace TeamSpeak3QueryApi.Net.Specialized;

internal static class EnumExtensions
{
    public static IEnumerable<Enum> GetFlags(this Enum input)
    {
        foreach (Enum value in Enum.GetValues(input.GetType()))
            if (input.HasFlag(value))
                yield return value;
    }

    public static IEnumerable<string> GetFlagsName(this Enum input)
    {
        return input.GetFlags().Select(value => value.ToString().ToLowerInvariant());
    }
}
