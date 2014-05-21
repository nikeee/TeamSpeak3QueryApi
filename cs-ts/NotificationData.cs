using System.Diagnostics;

namespace CsTs
{
    public class NotificationData
    {
        public QueryResponse[] Payload { get; private set; }

        internal NotificationData(QueryResponse[] queryResponse)
        {
            Debug.Assert(queryResponse != null);
            Payload = queryResponse;
        }
    }
}
