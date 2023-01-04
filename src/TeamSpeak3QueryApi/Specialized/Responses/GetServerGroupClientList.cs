namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class GetServerGroupClientList : Response
{
    [QuerySerialize("cldbid")]
    public int ClientDatabaseId;
}
