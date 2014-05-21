using System;

namespace CsTs
{
    public class QueryException : Exception
    {
        public QueryError Error { get; private set; }

        public QueryException(QueryError error)
        {
            Error = error;
        }
    }
}
