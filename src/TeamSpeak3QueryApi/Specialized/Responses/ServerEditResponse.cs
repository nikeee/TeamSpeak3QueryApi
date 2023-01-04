namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class ServerEditResponse : Response
{
    [QuerySerialize("msg")]
    public string Message;

    [QuerySerialize("error id")]
    public int ErrorId;
}
