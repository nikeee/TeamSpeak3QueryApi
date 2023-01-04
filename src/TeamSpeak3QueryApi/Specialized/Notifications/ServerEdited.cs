using System;

namespace TeamSpeak3QueryApi.Net.Specialized.Notifications;

public class ServerEdited : Notification
{
    [QuerySerialize("reasonid")]
    public ReasonId Reason;

    [QuerySerialize("invokerid")]
    public int InvokerId;

    [QuerySerialize("invokername")]
    public string InvokerName;

    [QuerySerialize("invokeruid")]
    public string InvokerUid;

    [QuerySerialize("virtualserver_name")]
    public string VirtualServerName;

    [QuerySerialize("virtualserver_codec_encryption_mode")]
    public CodecEncryptionMode VirtualServerCodecEncryptionMode;

    [QuerySerialize("virtualserver_default_server_group")]
    public int VirtualServerDefaultServerGroup;

    [QuerySerialize("virtualserver_default_channel_group")]
    public int VirtualServerDefaultChannelGroup;

    [QuerySerialize("virtualserver_hostbanner_url")]
    public string HostbannerUrl;

    [QuerySerialize("virtualserver_hostbanner_gfx_url")]
    public string HostbannerGfxUrl;

    [QuerySerialize("virtualserver_hostbanner_gfx_interval")]
    public TimeSpan HostbannerGfxInterval;

    [QuerySerialize("virtualserver_priority_speaker_dimm_modificator")]
    public string PrioritySpeakerDimmModificator;

    [QuerySerialize("virtualserver_hostbutton_tooltip")]
    public string HostButtonTooltipText;

    [QuerySerialize("virtualserver_hostbutton_url")]
    public string HostButtonUrl;

    [QuerySerialize("virtualserver_hostbutton_gfx_url")]
    public string HostButtonGfxUrl;

    [QuerySerialize("virtualserver_name_phonetic")]
    public string VirtualServerPhoneticName;

    [QuerySerialize("virtualserver_icon_id")]
    public long VirtualServerIconId;

    [QuerySerialize("virtualserver_hostbanner_mode")]
    public HostBannerMode HostbannerMode;

    [QuerySerialize("virtualserver_channel_temp_delete_delay_default")]
    public TimeSpan TempChannelDefaultDeleteDelay;
}
