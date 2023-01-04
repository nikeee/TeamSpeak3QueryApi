using System;

namespace TeamSpeak3QueryApi.Net.Specialized;

[Flags]
public enum GetClientOptions
{
    Uid = 1 << 0,
    Away = 1 << 1,
    Voice = 1 << 2,
    Times = 1 << 3,
    Groups = 1 << 4,
    Info = 1 << 5,
    Icon = 1 << 6,
    Country = 1 << 7,
}
