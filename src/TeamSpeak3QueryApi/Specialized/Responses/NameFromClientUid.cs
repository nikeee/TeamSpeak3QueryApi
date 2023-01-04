namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class NameFromClientUid : Response
{
    [QuerySerialize("cluid")]
    public string ClientUid;

    [QuerySerialize("cldbid")]
    public int ClientDatabaseId;

    [QuerySerialize("name")]
    public string Nickname;
}
