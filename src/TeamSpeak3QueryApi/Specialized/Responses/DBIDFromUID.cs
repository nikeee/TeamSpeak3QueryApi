namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class DBIDFromUID : Response
    {
        [QuerySerialize("cluid")]
        public string cluid;

        [QuerySerialize("cldbid")]
        public int cldbid;

    }
}
