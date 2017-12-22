namespace TeamSpeak3QueryApi.Net.Specialized
{
    public enum ReasonId
    {
        SwitchedChannelOrJoinedServer = 0,
        MovedItem = 1,

        Timeout = 3,

        ChannelKick = 4,
        ServerKick = 5,
        Ban = 6,

        LeftServer = 8,
        ServerOrChannelEdited = 10,
        ServerShutDown = 11
    }
}
