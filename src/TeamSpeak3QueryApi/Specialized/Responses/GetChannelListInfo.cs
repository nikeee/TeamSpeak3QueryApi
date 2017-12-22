using System;

namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class GetChannelListInfo : Response
    {
        [QuerySerialize("cid")]
        public int Id;

        [QuerySerialize("pid")]
        public int ParentChannelId;

        [QuerySerialize("channel_order")]
        public int Order;

        [QuerySerialize("channel_name")]
        public string Name;

        [QuerySerialize("channel_topic")]
        public string Topic;

        [QuerySerialize("channel_flag_default")]
        public bool IsDefaultChannel;

        [QuerySerialize("channel_flag_password")]
        public bool HasPassword;

        [QuerySerialize("channel_flag_permanent")]
        public bool IsPermanent;

        [QuerySerialize("channel_flag_semi_permanent")]
        public bool IsSemiPermanent;

        [QuerySerialize("channel_codec")]
        public Codec Codec;

        [QuerySerialize("channel_codec_quality")]
        public int CodecQuality;

        [QuerySerialize("channel_needed_talk_power")]
        public int NeededTalkPower;

        [QuerySerialize("channel_icon_id")]
        public long IconId;

        [QuerySerialize("seconds_empty")]
        public TimeSpan DurationEmpty;

        [QuerySerialize("total_clients_family")]
        public int TotalFamilyClients;

        [QuerySerialize("channel_maxclients")]
        public int MaxClients;

        [QuerySerialize("channel_maxfamilyclients")]
        public int MaxFamilyClients;

        [QuerySerialize("total_clients")]
        public int TotalClients;

        [QuerySerialize("channel_needed_subscribe_power")]
        public int NeededSubscribePower;
    }
}
