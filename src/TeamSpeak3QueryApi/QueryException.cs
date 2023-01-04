using System;
using System.Runtime.Serialization;

namespace TeamSpeak3QueryApi.Net;

/// <summary>Represents errors that occur during query execution.</summary>
[Serializable]
public class QueryException : Exception
{
    /// <summary>Gets the returned error by the Query API host.</summary>
    /// <returns>The returned error.</returns>
    public QueryError Error { get; }

    /// <summary>Initializes a new instance of the <see cref="T:TeamSpeak3QueryApi.Net.QueryException"/> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public QueryException(string message)
        : this(message, null)
    { }

    /// <summary>Initializes a new instance of the <see cref="T:TeamSpeak3QueryApi.Net.QueryException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="langword">Nothing</see> in Visual Basic) if no inner exception is specified.</param>
    public QueryException(string message, Exception innerException)
        : base(message, innerException)
    { }

    /// <summary>Initializes a new instance of the <see cref="T:TeamSpeak3QueryApi.Net.QueryException"/> class with a specified error returned by the Query API.</summary>
    /// <param name="error">The <see cref="T:TeamSpeak3QueryApi.Net.QueryError"/> that was returned by the Query API.</param>
    public QueryException(QueryError error)
        : this("An error occurred during the query.")
    {
        Error = error;
    }
}
