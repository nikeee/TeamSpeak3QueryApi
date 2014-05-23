namespace TeamSpeak3QueryApi.Net.Specialized.Notifications
{
    public abstract class InfokerInformation : Notification
    {
        [QuerySerialize("reasonid")]
        public ReasonId Reason;

        [QuerySerialize("invokerid")]
        public int InvokerId;

        [QuerySerialize("invokername")]
        public string InvokerName;

        [QuerySerialize("invokeruid")]
        public string InvokerUid;
    }
}
