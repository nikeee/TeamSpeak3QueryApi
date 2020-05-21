namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class GetClientIds : Response
    {
        [QuerySerialize("clid")]
        public int clid;

        [QuerySerialize("cluid")]
        public string cluid;

        [QuerySerialize("name")]
        public string Nickname;
    }
}
