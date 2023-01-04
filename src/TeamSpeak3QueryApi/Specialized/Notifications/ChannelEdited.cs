using System;

namespace TeamSpeak3QueryApi.Net.Specialized.Notifications;

public class ChannelEdited : InvokerInformation
{
    [QuerySerialize("cid")]
    public int ChannelId;

    [QuerySerialize("channel_name")]
    public string Name;

    [QuerySerialize("channel_topic")]
    public string Topic;

    [QuerySerialize("channel_codec")]
    public Codec Codec;

    [QuerySerialize("channel_codec_quality")]
    public int CodecQuality;

    [QuerySerialize("channel_maxclients")]
    public int MaxClients;

    [QuerySerialize("channel_maxfamilyclients")]
    public int MaxFamilyClients;

    [QuerySerialize("channel_order")]
    public int Order;

    [QuerySerialize("channel_flag_permanent")]
    public bool IsPermanent;

    [QuerySerialize("channel_flag_semi_permanent")]
    public bool IsSemiPermanent;

    [QuerySerialize("channel_flag_default")]
    public bool IsDefaultChannel;

    [QuerySerialize("channel_flag_password")]
    public bool HasPassword;

    [QuerySerialize("channel_codec_latency_factor")]
    public int CodecLatencyFactor;

    [QuerySerialize("channel_codec_is_unencrypted")]
    public bool IsUnencrypted;

    [QuerySerialize("channel_delete_delay")]
    public TimeSpan DeleteDelay;

    [QuerySerialize("channel_flag_maxclients_unlimited")]
    public bool CanHasUnlimitedClients;

    [QuerySerialize("channel_flag_maxfamilyclients_unlimited")]
    public bool CanHasUnlimitedFamilyClients;

    [QuerySerialize("channel_flag_maxfamilyclients_inherited")]
    public bool IsMaxFamilyClientsInherited;

    [QuerySerialize("channel_needed_talk_power")]
    public int NeededTalkPower;

    [QuerySerialize("channel_name_phonetic")]
    public string PhoneticChannelName;

    [QuerySerialize("channel_icon_id")]
    public string IconId;
}
