using System;
using System.Collections.Generic;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    internal static class EnumExtensions
    {
        public static IEnumerable<Enum> GetFlags(this Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }

        public static List<string> GetFlagsAsList(this Enum input)
        {
            var res = new List<string>();
            foreach (var value in input.GetFlags())
                res.Add(value.ToString().ToLowerInvariant());
            return res;
        }
    }
}
