namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class ClientUidFromClientId : Response
{
    [QuerySerialize("clid")]
    public int ClientId;

    [QuerySerialize("cluid")]
    public string ClientUid;

    [QuerySerialize("nickname")]
    public string Nickname;
}
