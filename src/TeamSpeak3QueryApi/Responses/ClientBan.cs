namespace TeamSpeak3QueryApi.Net.Responses
{
    public class ClientBan : Response
    {
        [QuerySerialize("banid")]
        public int Id;
    }
}
