namespace TeamSpeak3QueryApi.Net.Notifications
{
    public class ChannelDescriptionChanged : Notification
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }
}
