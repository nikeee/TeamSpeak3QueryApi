using System;
using System.Runtime.Serialization;

namespace TeamSpeak3QueryApi.Net
{
    /// <summary>Represents errors that occur during queries caused by protcol violations.</summary>
    [Serializable]
    public class TeamSpeakQueryProtocolException : Exception
    {
        public TeamSpeakQueryProtocolException()
            : this("An error occurred during the query.")
        { }

        public TeamSpeakQueryProtocolException(string message)
            : this(message, null)
        { }

        public TeamSpeakQueryProtocolException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected TeamSpeakQueryProtocolException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
