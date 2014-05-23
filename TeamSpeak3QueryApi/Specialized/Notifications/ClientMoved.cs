namespace TeamSpeak3QueryApi.Net.Specialized.Notifications
{
    public class ClientMoved : InfokerInformation
    {
        [QuerySerialize("ctid")]
        public int TargetChannel;

        [QuerySerialize("clid")]
        public int[] ClientIds;
    }
}
