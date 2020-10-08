namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class GetClientIds : Response
    {
        [QuerySerialize("clid")]
        public int Id;

        [QuerySerialize("cluid")]
        public string UniqueIdentifier;

        [QuerySerialize("name")]
        public string NickName;
    }
}
