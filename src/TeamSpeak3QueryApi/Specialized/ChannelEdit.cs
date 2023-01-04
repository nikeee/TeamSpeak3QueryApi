using System;

namespace TeamSpeak3QueryApi.Net.Specialized;

[Flags]
public enum ChannelEdit
{
    channel_name,
    channel_topic,
    channel_description,
    channel_password,
    channel_codec,
    channel_codec_quality,
    channel_maxclients,
    channel_maxfamilyclients,
    channel_order,
    channel_flag_permanent,
    channel_flag_semi_permanent,
    channel_flag_temporary,
    channel_flag_default,
    channel_flag_maxclients_unlimited,
    channel_flag_maxfamilyclients_unlimited,
    channel_flag_maxfamilyclients_inherited,
    channel_needed_talk_power,
    channel_name_phonetic,
    channel_icon_id,
    channel_codec_is_unencrypted,
}
