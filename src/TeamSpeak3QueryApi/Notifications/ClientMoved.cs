namespace TeamSpeak3QueryApi.Net.Notifications
{
    public class ClientMoved : InvokerInformation
    {
        [QuerySerialize("ctid")]
        public int TargetChannel;

        [QuerySerialize("clid")]
        public int[] ClientIds;
    }
}
