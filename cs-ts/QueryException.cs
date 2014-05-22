using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace CsTs
{
    [Serializable]
    public class QueryException : Exception
    {
        public QueryError Error { get; private set; }

        public QueryException(QueryError error)
            : base("An error occurred during the query.")
        {
            Error = error;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if(info != null)
                info.AddValue("Error", Error);
            base.GetObjectData(info, context);
        }
    }
}
