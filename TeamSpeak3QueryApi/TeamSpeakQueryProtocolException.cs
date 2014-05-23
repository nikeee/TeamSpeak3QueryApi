using System;

namespace TeamSpeak3QueryApi.Net
{
    /// <summary>Represents errors that occur during queries caused by protcol violations.</summary>
    [Serializable]
    public class TeamSpeakQueryProtocolException : Exception
    { }
}
