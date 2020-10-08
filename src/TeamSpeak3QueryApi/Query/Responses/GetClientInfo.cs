using System;
using System.Collections.Generic;
using TeamSpeak3QueryApi.Net.Query.Enums;

namespace TeamSpeak3QueryApi.Net.Query.Responses
{
    public class GetClientInfo : Response
    {
        [QuerySerialize("clid")]
        public int Id;

        [QuerySerialize("cid")]
        public int ChannelId;

        [QuerySerialize("client_database_id")]
        public int DatabaseId;

        [QuerySerialize("client_nickname")]
        public string NickName;

        [QuerySerialize("client_type")]
        public ClientType Type;

        [QuerySerialize("client_unique_identifier")]
        public string UniqueIdentifier;

        [QuerySerialize("client_version")]
        public string Version;

        [QuerySerialize("connection_client_ip")]
        public string ConnectionIp;

        [QuerySerialize("client_platform")]
        public string Plattform;

        [QuerySerialize("client_input_muted")]
        public bool InputMuted;

        [QuerySerialize("client_output_muted")]
        public bool OutputMuted;

        [QuerySerialize("client_is_recording")]
        public bool IsRecording;

        [QuerySerialize("client_servergroups")]
        public IReadOnlyList<int> ServerGroupIds;

        [QuerySerialize("client_channel_group_id")]
        public IReadOnlyList<int> ChannelGroupsIds;

        [QuerySerialize("client_created")]
        public DateTime Created;

        [QuerySerialize("client_lastconnected")]
        public DateTime LastConnected;

        [QuerySerialize("client_away")]
        public bool Away;

        [QuerySerialize("client_away_message")]
        public string AwayMessage;

        [QuerySerialize("client_idle_time")]
        private long _idleTime; //Because it is in ms instead if s defined in Typecaster
        public TimeSpan IdleTime => TimeSpan.FromMilliseconds(_idleTime);
    }
}
