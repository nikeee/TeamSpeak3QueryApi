namespace TeamSpeak3QueryApi.Net.Specialized.Notifications;

public class TokenUsed : InvokerInformation
{
    [QuerySerialize("clid")]
    public int ClientId;

    [QuerySerialize("cldbid")]
    public int ClientDatabaseId;

    [QuerySerialize("cluid")]
    public string ClientUid;

    [QuerySerialize("token")]
    public string UsedToken;

    [QuerySerialize("tokencustomset")]
    public string TokenCustomSet;

    [QuerySerialize("token1")]
    public string Token1;

    [QuerySerialize("token2")]
    public string Token2;
}
