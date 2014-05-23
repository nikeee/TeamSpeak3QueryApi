namespace TeamSpeak3QueryApi.Net.Specialized.Notifications
{
    public class ChannelEdited : InfokerInformation
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }
}
