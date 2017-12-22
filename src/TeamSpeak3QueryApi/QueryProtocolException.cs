using System;
using System.Runtime.Serialization;

namespace TeamSpeak3QueryApi.Net
{
    /// <summary>Represents errors that occur during queries caused by protcol violations.</summary>
    [Serializable]
    public class QueryProtocolException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="T:TeamSpeak3QueryApi.Net.QueryProtocolException"/> class.</summary>
        public QueryProtocolException()
            : this("An error occurred during the query.")
        { }

        /// <summary>Initializes a new instance of the <see cref="T:TeamSpeak3QueryApi.Net.QueryProtocolException"/> class with a specified error message.</summary>
        /// <param name="message">The message that describes the error.</param>
        public QueryProtocolException(string message)
            : this(message, null)
        { }

        /// <summary>Initializes a new instance of the <see cref="T:TeamSpeak3QueryApi.Net.QueryProtocolException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="langword">Nothing</see> in Visual Basic) if no inner exception is specified.</param>
        public QueryProtocolException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
