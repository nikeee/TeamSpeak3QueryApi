namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class GetNameFromClientDatabaseId : Response
    {
        [QuerySerialize("cluid")]
        public string UniqueIdentifier;

        [QuerySerialize("name")]
        public string NickName;

        [QuerySerialize("cldbid")]
        public int DatabaseId;
    }
}
