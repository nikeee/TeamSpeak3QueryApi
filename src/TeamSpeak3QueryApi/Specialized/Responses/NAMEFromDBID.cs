namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class NAMEFromDBID : Response
    {
        [QuerySerialize("cluid")]
        public string cluid;

        [QuerySerialize("cldbid")]
        public int cldbid;

        [QuerySerialize("name")]
        public string Nickname;
    }
}
