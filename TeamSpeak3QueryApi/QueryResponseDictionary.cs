using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TeamSpeak3QueryApi
{
    /// <summary>Represents the data of a query response.</summary>
    [Serializable]
    public class QueryResponseDictionary : Dictionary<string, object>
    {
        /// <summary>Creates a new instance of <see cref="QueryResponseDictionary"/>.</summary>
        public QueryResponseDictionary()
        { }

        /// <summary>Initializes a new instance of the <see cref="QueryResponseDictionary"/> with serialized data.</summary>
        protected QueryResponseDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
