namespace TeamSpeak3QueryApi.Net.Query.Notifications
{
    public class ChannelDescriptionChanged : Notification
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }
}
