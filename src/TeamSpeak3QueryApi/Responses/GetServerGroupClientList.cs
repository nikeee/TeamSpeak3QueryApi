namespace TeamSpeak3QueryApi.Net.Responses
{
    public class GetServerGroupClientList : Response
    {
        [QuerySerialize("cldbid")]
        public int ClientDatabaseId;
    }
}
