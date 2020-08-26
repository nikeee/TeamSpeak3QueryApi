using System.Diagnostics;
using TeamSpeak3QueryApi.Net.Notifications;

namespace TeamSpeak3QueryApi.Net.Query
{
    public class QueryNotification
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
