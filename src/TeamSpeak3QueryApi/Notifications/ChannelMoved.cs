namespace TeamSpeak3QueryApi.Net.Notifications
{
    public class ChannelMoved : InvokerInformation
    {
        [QuerySerialize("cid")]
        public int ChannelId;

        [QuerySerialize("cpid")]
        public int ParentChannelId;

        [QuerySerialize("order")]
        public int Order;
    }
}
