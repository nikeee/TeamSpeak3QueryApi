namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class GetClientUniqueIdFromClientId : Response
    {
        [QuerySerialize("clid")]
        public int Id;

        [QuerySerialize("cluid")]
        public string UniqueIdentifier;

        [QuerySerialize("nickname")]
        public string NickName;
    }
}
