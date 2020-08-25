namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class ClientBan : Response
    {
        [QuerySerialize("banid")]
        public int Id;
    }
}
