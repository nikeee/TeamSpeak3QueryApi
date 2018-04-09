namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    // TODO: Rename
    public class GetServerGroups : Response
    {
        [QuerySerialize("clid")]
        public int Id;

        [QuerySerialize("cid")]
        public int ChannelId;

        [QuerySerialize("client_database_id")]
        public int DatabaseId;

        [QuerySerialize("client_nickname")]
        public string NickName;

        [QuerySerialize("client_type")]
        public ClientType Type;
    }
}
