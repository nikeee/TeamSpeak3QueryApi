namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class ServerEditResponse : Response
    {
        [QuerySerialize("msg")]
        public string msg;

        [QuerySerialize("error id")]
        public int error_id;
    }
}
