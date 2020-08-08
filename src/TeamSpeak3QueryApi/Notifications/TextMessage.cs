using TeamSpeak3QueryApi.Net.Enums;

namespace TeamSpeak3QueryApi.Net.Notifications
{
    public class TextMessage : InvokerInformation
    {
        [QuerySerialize("targetmode")]
        public MessageTarget TargetMode;

        [QuerySerialize("msg")]
        public string Message;

        [QuerySerialize("target")]
        public int TargetClientId; // (clid des Empfängers; Parameter nur bei textprivate vorhanden)
    }
}
