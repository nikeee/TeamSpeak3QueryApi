namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class NAMEFromUID : Response
    {
        [QuerySerialize("cluid")]
        public string cluid;

        [QuerySerialize("cldbid")]
        public int cldbid;

        [QuerySerialize("name")]
        public string Nickname;
    }
}
