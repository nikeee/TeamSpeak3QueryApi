using System;
using System.Collections.Generic;

namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class GetDbClientInfo : Response
    {
        [QuerySerialize("cldbid")]
        public int DatabaseId;

        [QuerySerialize("client_nickname")]
        public string NickName;

        [QuerySerialize("client_unique_identifier")]
        public string UniqueIdentifier;

        [QuerySerialize("client_created")]
        public DateTime Created;

        [QuerySerialize("client_lastconnected")]
        public DateTime LastConnected;

        [QuerySerialize("client_totalconnections")]
        public int TotalConnectionCount;

        [QuerySerialize("client_description")]
        public string Description;

        [QuerySerialize("client_login_name")]
        public string LoginName;

        [QuerySerialize("client_lastip")]
        public string LastConnectionIp;
    }
}
