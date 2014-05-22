using System;

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
    }
}
