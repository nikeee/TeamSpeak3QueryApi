using System;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    [Flags]
    public enum GetClientOptions
    {
        Uid,
        Away,
        Voice,
        Times,
        Groups,
        Info,
        Icon,
        Country
    }
}
