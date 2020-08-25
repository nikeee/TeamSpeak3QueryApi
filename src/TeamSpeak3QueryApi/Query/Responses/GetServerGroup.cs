namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class GetServerGroup : Response
    {
        [QuerySerialize("sgid")]
        public int Id;

        [QuerySerialize("name")]
        public string Name;

        [QuerySerialize("cldbid")]
        public int ClientDatabaseId;
    }
}
