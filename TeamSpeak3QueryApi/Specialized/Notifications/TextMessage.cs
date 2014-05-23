namespace TeamSpeak3QueryApi.Net.Specialized.Notifications
{
    public class TextMessage : InfokerInformation
    {
        [QuerySerialize("targetmode")]
        public MessageTarget TargetMode;

        [QuerySerialize("msg")]
        public string Message;

        [QuerySerialize("target")]
        public int TargetClientId; // (clid des Empfängers; Parameter nur bei textprivate vorhanden)
    }
}
