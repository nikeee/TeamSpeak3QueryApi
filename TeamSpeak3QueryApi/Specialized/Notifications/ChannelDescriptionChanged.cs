namespace TeamSpeak3QueryApi.Net.Specialized.Notifications
{
    public class ChannelDescriptionChanged : Notify
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }
}
