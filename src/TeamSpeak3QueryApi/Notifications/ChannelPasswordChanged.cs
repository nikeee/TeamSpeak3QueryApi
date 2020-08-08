namespace TeamSpeak3QueryApi.Net.Notifications
{
    public class ChannelPasswordChanged : Notification
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }
}
