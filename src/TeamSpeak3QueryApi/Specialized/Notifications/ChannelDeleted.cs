namespace TeamSpeak3QueryApi.Net.Specialized.Notifications
{
    public class ChannelDeleted : InvokerInformation
    {
        // invokerid („0“ bei Löschung eines temporären Channels durch den Server
        [QuerySerialize("cid")]
        public int ChannelId;
    }
}
