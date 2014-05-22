using System.Collections.ObjectModel;
using System.Diagnostics;

namespace CsTs
{
    public class NotificationData
    {
        public ReadOnlyCollection<QueryResponse> Payload { get; private set; }

        internal NotificationData(QueryResponse[] queryResponse)
        {
            Debug.Assert(queryResponse != null);
            Payload = new ReadOnlyCollection<QueryResponse>(queryResponse);
        }
    }
}
