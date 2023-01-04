namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class GetClientInfo : Response
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
