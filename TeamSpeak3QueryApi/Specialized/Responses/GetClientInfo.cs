namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class GetClientsInfo : Response
    {
        [QuerySerialize("clid")]
        public int ClientId;

        [QuerySerialize("cid")]
        public int ChannelId;

        [QuerySerialize("client_database_id")]
        public int ClientDatabaseId;

        [QuerySerialize("client_nickname")]
        public string ClientNickName;

        [QuerySerialize("client_type")]
        public ClientType Type;
    }
}
