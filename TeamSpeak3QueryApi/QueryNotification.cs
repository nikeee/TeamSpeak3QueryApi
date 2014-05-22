using System.Diagnostics;

namespace TeamSpeak3QueryApi
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
