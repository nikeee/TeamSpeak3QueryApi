namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class GetClientIds : Response
{
    [QuerySerialize("clid")]
    public int ClientId;

    [QuerySerialize("cluid")]
    public string ClientUid;

    [QuerySerialize("name")]
    public string Nickname;
}
