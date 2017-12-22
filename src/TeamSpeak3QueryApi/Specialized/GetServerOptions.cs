using System;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    [Flags]
    public enum GetServerOptions
    {
        Uid = 1 << 0,
        All = 1 << 1,
        Short = 1 << 2,
        OnlyOffline = 1 << 3,
    }
}
