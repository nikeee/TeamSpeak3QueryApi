using System;

namespace TeamSpeak3QueryApi.Net.Specialized.Notifications;

public class ClientEnterView : Notification
{
    [QuerySerialize("cfid")]
    public int SourceChannelId; // (Quellchannel; „0“ beim Betreten des Servers)

    [QuerySerialize("ctid")]
    public int TargetChannelId;

    [QuerySerialize("reasonid")]
    public ReasonId Reason;

    [QuerySerialize("clid")]
    public int Id;

    [QuerySerialize("client_unique_identifier")]
    public string Uid;

    [QuerySerialize("client_nickname")]
    public string NickName;

    [QuerySerialize("client_input_muted")]
    public bool IsInputMuted;

    [QuerySerialize("client_output_muted")]
    public bool IsOutputMuted;

    [QuerySerialize("client_outputonly_muted")]
    public bool IsOutputOnlyMuted;

    [QuerySerialize("client_input_hardware")]
    public bool IsInputHardware;

    [QuerySerialize("client_output_hardware")]
    public bool IsClientOutputHardware;

    [QuerySerialize("client_meta_data")]
    public string Metadata; // (Query-Clients können hier mit clientupdate für sich selbst etwas speichern)

    [QuerySerialize("client_is_recording")]
    public bool IsRecording;

    [QuerySerialize("client_database_id")]
    public int DatabaseId;

    [QuerySerialize("client_channel_group_id")]
    public int ChannelGroupId;

    [QuerySerialize("client_servergroups")]
    public string ServerGroups; // ??

    [QuerySerialize("client_away")]
    public bool IsAway;

    [QuerySerialize("client_away_message")]
    public string AwayMessage;

    [QuerySerialize("client_type")]
    public ClientType Type; // („1“ für Query, „0“ für Voice)

    [QuerySerialize("client_flag_avatar")]
    public string AvatarFlag;// ??

    [QuerySerialize("client_talk_power")]
    public int TalkPower;

    [QuerySerialize("client_talk_request")]
    public int RequestedTalkPower;

    [QuerySerialize("client_talk_request_msg")]
    public string TalkPowerRequestMessage;

    [QuerySerialize("client_description")]
    public string Description;

    [QuerySerialize("client_is_talker")]
    public bool IsTalker;

    [QuerySerialize("client_is_priority_speaker")]
    public bool IsHighPrioritySpeaker;

    [Obsolete]
    [QuerySerialize("client_unread_messages")]
    public int UnreadMessages; // (hier immer noch vorhanden, obwohl es aus clientinfo längst gelöscht wurde)

    [QuerySerialize("client_nickname_phonetic")]
    public string PhoneticName;

    [QuerySerialize("client_needed_serverquery_view_power")]
    public bool NeededServerQueryViewPower; // (Änderung für Voice-Nutzer während einer Sitzung manchmal nicht rückwirkend, public funktioniert; jedoch bei Gruppenwechsel)

    [QuerySerialize("client_icon_id")]
    public long IconId;

    [QuerySerialize("client_is_channel_commander")]
    public bool IsChannelCommander;

    [QuerySerialize("client_country")]
    public string CountryCode;

    [QuerySerialize("client_channel_group_inherited_channel_id")]
    public int InheritedChannelGroupFromChannelId;

    [QuerySerialize("client_badges")]
    public string Badges; // (leer bei Query- und zu alten Clients, sonst in sich selbst parametrisierter String)
}
