using System;

namespace TeamSpeak3QueryApi.Net.Enums
{
    [Flags]
    public enum GetChannelOptions
    {
        Topic = 1 << 0,
        Flags = 1 << 1,
        Voice = 1 << 2,
        Limits = 1 << 3,
        Icon = 1 << 4,
    }
}
