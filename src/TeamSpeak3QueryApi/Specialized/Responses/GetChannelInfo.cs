using System;

namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class GetChannelInfo : Response
{
    [QuerySerialize("cid")]
    public int ChannelId;

    [QuerySerialize("pid")]
    public int ParentChannelId;

    [QuerySerialize("channel_name")]
    public string Name;

    [QuerySerialize("channel_topic")]
    public string Topic;

    [QuerySerialize("channel_description")]
    public string Description;

    [QuerySerialize("channel_password")]
    public string Password;

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

    [QuerySerialize("channel_flag_default")]
    public bool IsDefaultChannel;

    [QuerySerialize("channel_flag_password")]
    public bool HasPassword;

    [QuerySerialize("channel_flag_permanent")]
    public bool IsPermanent;

    [QuerySerialize("channel_flag_semi_permanent")]
    public bool IsSemiPermanent;

    [QuerySerialize("channel_flag_temporary")]
    public bool IsTemporary;

    [QuerySerialize("channel_codec_latency_factor")]
    public int CodecLatencyFactor;

    [QuerySerialize("channel_codec_is_unencrypted")]
    public bool IsCodecUnencrypted;

    [QuerySerialize("channel_security_salt")]
    public string SecuritySalt;

    [QuerySerialize("channel_delete_delay")]
    public TimeSpan DeleteDelay;

    [QuerySerialize("channel_flag_maxclients_unlimited")]
    public bool IsMaxClientsUnlimited;

    [QuerySerialize("channel_flag_maxfamilyclients_unlimited")]
    public bool IsMaxFamilyClientsUnlimited;

    [QuerySerialize("channel_flag_maxfamilyclients_inherited")]
    public bool IsMaxFamilyClientsInherited;

    [QuerySerialize("channel_filepath")]
    public string ChannelFilePath;

    [QuerySerialize("channel_needed_talk_power")]
    public int NeededTalkPower;

    [QuerySerialize("channel_forced_silence")]
    public bool ForcedSilence; // bool? dunno

    [QuerySerialize("channel_name_phonetic")]
    public string PhoneticName;

    [QuerySerialize("channel_icon_id")]
    public long IconId;

    [QuerySerialize("channel_flag_private")]
    public bool IsPrivate;

    [QuerySerialize("seconds_empty")]
    public TimeSpan SecondsEmpty;
}
