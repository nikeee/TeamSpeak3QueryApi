namespace TeamSpeak3QueryApi.Net.Specialized.Notifications
{
    public class ChannelDescriptionChanged : Notification
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }
}
