using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class GetClientDetailedInfo : Response
    {
        [QuerySerialize("cid")]
        public int ChannelId;

        [QuerySerialize("client_unique_identifier")]
        public string UniqueIdentifier;

        [QuerySerialize("client_nickname")]
        public string NickName;

        [QuerySerialize("client_login_name")]
        public string ServerQueryName;

        [QuerySerialize("client_database_id")]
        public int DatabaseId;

        [QuerySerialize("client_type")]
        public ClientType Type;

        [QuerySerialize("client_version")]
        public string Version;

        [QuerySerialize("connection_client_ip")]
        public string ConnectionIp;

        [QuerySerialize("client_platform")]
        public string Plattform;

        [QuerySerialize("client_description")]
        public string Description;

        [QuerySerialize("client_input_muted")]
        public bool InputMuted;

        [QuerySerialize("client_output_muted")]
        public bool OutputMuted;

        [QuerySerialize("client_outputonly_muted")]
        public bool OutputOnlyMuted;

        [QuerySerialize("client_is_recording")]
        public bool IsRecording;

        [QuerySerialize("client_servergroups")]
        private string _serverGroupsStr;
        public List<int> ServerGroupIds
        {
            get {
                if(string.IsNullOrWhiteSpace(_serverGroupsStr))
                    return new List<int>();

                return _serverGroupsStr.Split(',').Select(grp => int.Parse(grp)).ToList();
            }
            private set { }
        }

        [QuerySerialize("client_channel_group_id")]
        private string _channelGroupsStr; 
        public List<int> ChannelGroupsIds
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_channelGroupsStr))
                    return new List<int>();

                return _channelGroupsStr.Split(',').Select(grp => int.Parse(grp)).ToList();
            }
            private set { }
        }

        [QuerySerialize("client_created")]
        public DateTime Created;
        
        [QuerySerialize("client_lastconnected")]
        public DateTime LastConnected;
        
        [QuerySerialize("client_totalconnections")]
        public int TotalConnectionCount;
        
        [QuerySerialize("client_away")]
        public bool Away;
        
        [QuerySerialize("client_away_message")]
        public string AwayMessage;
        
        [QuerySerialize("connection_connected_time")]
        private long _connectionTime; //Because it is in ms instead if s defined in Typecaster
        public TimeSpan ConnectionTime => TimeSpan.FromMilliseconds(_connectionTime);

        [QuerySerialize("client_idle_time")]
        private long _idleTime; //Because it is in ms instead if s defined in Typecaster
        public TimeSpan IdleTime => TimeSpan.FromMilliseconds(_idleTime);
    }
}
