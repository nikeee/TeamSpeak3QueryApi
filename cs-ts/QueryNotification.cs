using System.Diagnostics;

namespace CsTs
{
    internal class QueryNotification
    {
        public string Name { get; set; }
        public NotificationData Data { get; set; }
        public QueryNotification(string name, NotificationData data)
        {
            Debug.Assert(name != null);
            Name = name;
            Data = data;
        }
    }
}
