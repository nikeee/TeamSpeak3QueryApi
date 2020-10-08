namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class GetNameFromClientUniqueId : Response
    {
        [QuerySerialize("cluid")]
        public string UniqueIdentifier;

        [QuerySerialize("cldbid")]
        public int DatabaseId;

        [QuerySerialize("name")]
        public string NickName;
    }
}
