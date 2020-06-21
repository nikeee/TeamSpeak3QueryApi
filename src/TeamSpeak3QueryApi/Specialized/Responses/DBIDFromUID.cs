namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class DBIDFromUID : Response
    {
        [QuerySerialize("cluid")]
        public string ClientUid;

        [QuerySerialize("cldbid")]
        public int ClientDatabaseId;

    }
}
