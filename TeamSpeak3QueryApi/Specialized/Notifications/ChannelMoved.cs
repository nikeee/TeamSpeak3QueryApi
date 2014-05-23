namespace TeamSpeak3QueryApi.Net.Specialized.Notifications
{
    public class ChannelMoved : InfokerInformation
    {
        [QuerySerialize("cid")]
        public int ChannelId;

        [QuerySerialize("cpid")]
        public int ParentChannelId;

        [QuerySerialize("order")]
        public int Order;
    }
}
