namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class GetServerListInfo : Response
    {
        // TODO
        [QuerySerialize("server_id")] // guessed
        public int Id;
    }
}
