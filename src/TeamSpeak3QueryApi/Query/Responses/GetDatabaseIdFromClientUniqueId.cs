namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class GetDatabaseIdFromClientUniqueId : Response
    {
        [QuerySerialize("cluid")]
        public string UniqueIdentifier;

        [QuerySerialize("cldbid")]
        public int DatabaseId;
    }
}
