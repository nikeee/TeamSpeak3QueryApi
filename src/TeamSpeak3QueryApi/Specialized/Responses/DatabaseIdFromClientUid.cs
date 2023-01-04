namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class DatabaseIdFromClientUid : Response
{
    [QuerySerialize("cluid")]
    public string ClientUid;

    [QuerySerialize("cldbid")]
    public int ClientDatabaseId;

}
