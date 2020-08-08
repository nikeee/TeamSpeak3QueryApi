namespace TeamSpeak3QueryApi.Net.Responses
{
    public class CreatedChannel : Response
    {
        [QuerySerialize("cid")]
        public int Id;
    }
}
