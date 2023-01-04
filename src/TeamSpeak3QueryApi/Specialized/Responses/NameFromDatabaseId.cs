namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class NameFromDatabaseId : Response
{
    [QuerySerialize("cluid")]
    public string ClientUid;

    [QuerySerialize("cldbid")]
    public int ClientDatabaseId;

    [QuerySerialize("name")]
    public string Nickname;
}
