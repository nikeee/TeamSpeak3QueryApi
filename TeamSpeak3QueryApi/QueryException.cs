using System;
using System.Runtime.Serialization;

namespace TeamSpeak3QueryApi
{
    /// <summary>Represents errors that occur during query execution.</summary>
    [Serializable]
    public class QueryException : Exception
    {
        /// <summary>Gets the returned error by the Query API host.</summary>
        /// <returns>The returned error.</returns>
        public QueryError Error { get; private set; }

        internal QueryException(QueryError error)
            : base("An error occurred during the query.")
        {
            Error = error;
        }

        /// <summary> When overridden in a derived class, sets the <see cref="System.Runtime.Serialization.SerializationInfo"/> with information about the exception.</summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/>that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="System.ArgumentNullException">The info parameter is a null reference (Nothing in Visual Basic).</exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Error", Error);
        }
    }
}
