namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class ClientBan : Response
{
    [QuerySerialize("banid")]
    public int Id;
}
