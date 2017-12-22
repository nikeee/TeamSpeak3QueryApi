using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace TeamSpeak3QueryApi.Net
{
    /// <summary>Provides data that was retrieved by a notification.</summary>
    public class NotificationData
    {
        /// <summary>The payload of the retrieved data.</summary>
        /// <returns>The payload of the retrieved data.</returns>
        public IReadOnlyList<QueryResponseDictionary> Payload { get; }

        internal NotificationData(QueryResponseDictionary[] queryResponseDictionary)
        {
            Debug.Assert(queryResponseDictionary != null);
            Payload = new ReadOnlyCollection<QueryResponseDictionary>(queryResponseDictionary);
        }
    }
}
