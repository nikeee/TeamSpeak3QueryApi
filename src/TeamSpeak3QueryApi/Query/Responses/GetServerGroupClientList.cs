namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class GetServerGroupClientList : Response
    {
        [QuerySerialize("cldbid")]
        public int ClientDatabaseId;
    }
}
