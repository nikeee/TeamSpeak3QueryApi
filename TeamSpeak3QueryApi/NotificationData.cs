using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace TeamSpeak3QueryApi
{
    public class NotificationData
    {
        public IReadOnlyList<QueryResponseDictionary> Payload { get; private set; }

        internal NotificationData(QueryResponseDictionary[] queryResponseDictionary)
        {
            Debug.Assert(queryResponseDictionary != null);
            Payload = new ReadOnlyCollection<QueryResponseDictionary>(queryResponseDictionary);
        }
    }
}
