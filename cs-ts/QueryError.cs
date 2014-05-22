namespace TeamSpeak3QueryApi
{
    public class QueryError
    {
        public int Id { get; internal set; }
        public string Message { get; internal set; }
        public int FailedPermissionId { get; internal set; }
    }
}
