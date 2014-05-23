namespace TeamSpeak3QueryApi.Net.Specialized.Notifications
{
    public class ChannelPasswordChanged : Notify
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }
}
