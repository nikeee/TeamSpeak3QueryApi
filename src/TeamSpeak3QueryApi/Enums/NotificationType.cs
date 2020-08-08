namespace TeamSpeak3QueryApi.Net.Enums
{
    ///<remarks>http://redeemer.biz/medien/artikel/ts3-query-notify/</remarks>
    public enum NotificationType
    {
        // Server/Channel notifications
        ClientEnterView,
        ClientLeftView,

        // Server notifications
        ServerEdited,

        // Channel notifications
        ChannelDescriptionChanged,
        ChannelPasswordChanged,
        ChannelMoved,
        ChannelEdited,
        ChannelCreated,
        ChannelDeleted,
        ClientMoved,
        TextMessage,
        TokenUsed
    }
}
