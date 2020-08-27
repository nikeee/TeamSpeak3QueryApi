namespace TeamSpeak3QueryApi.Net.Query.Notifications
{
    public class ChannelPasswordChanged : Notification
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }
}
