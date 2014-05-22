using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TeamSpeak3QueryApi
{
    [Serializable]
    public class QueryResponseDictionary : Dictionary<string, object>
    {
        public QueryResponseDictionary()
        { }
        protected QueryResponseDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
