namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class CreatedChannel : Response
    {
        [QuerySerialize("cid")]
        public int Id;
    }
}
