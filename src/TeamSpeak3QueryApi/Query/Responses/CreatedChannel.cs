namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class CreatedChannel : Response
    {
        [QuerySerialize("cid")]
        public int Id;
    }
}
