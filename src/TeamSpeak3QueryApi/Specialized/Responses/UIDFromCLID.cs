namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class UIDFromCLID : Response
    {
        [QuerySerialize("clid")]
        public int clid;

        [QuerySerialize("cluid")]
        public string cluid;

        [QuerySerialize("nickname")]
        public string Nickname;
    }
}
