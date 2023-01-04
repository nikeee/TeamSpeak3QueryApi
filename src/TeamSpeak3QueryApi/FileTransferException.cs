using System;
using System.Collections.Generic;
using System.Text;

namespace TeamSpeak3QueryApi.Net;

/// <summary>Represents errors that occur during file transfers.</summary>
[Serializable]
public class FileTransferException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="T:TeamSpeak3QueryApi.Net.FileTransferException"/> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public FileTransferException(string message)
        : this(message, null)
    { }

    /// <summary>Initializes a new instance of the <see cref="T:TeamSpeak3QueryApi.Net.FileTransferException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="langword">Nothing</see> in Visual Basic) if no inner exception is specified.</param>
    public FileTransferException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
