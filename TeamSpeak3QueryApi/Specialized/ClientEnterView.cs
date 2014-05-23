using System;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    public class ClientEnterView : Notify
    {
        [QuerySerialize("cfid")]
        public int SourceChannelId; // (Quellchannel; „0“ beim Betreten des Servers)

        [QuerySerialize("ctid")]
        public int TargetChannelId;

        [QuerySerialize("reasonid")]
        public ReasonId Reason;

        [QuerySerialize("clid")]
        public int ClientId;

        [QuerySerialize("client_unique_identifier")]
        public string ClientUid;

        [QuerySerialize("client_nickname")]
        public string ClientNickName;

        [QuerySerialize("client_input_muted")]
        public bool IsClientInputMuted;

        [QuerySerialize("client_output_muted")]
        public bool IsClientOutputMuted;

        [QuerySerialize("client_outputonly_muted")]
        public bool IsClientOutputOnlyMuted;

        [QuerySerialize("client_input_hardware")]
        public bool IsClientInputHardware;

        [QuerySerialize("client_output_hardware")]
        public bool IsClientOutputHardware;

        [QuerySerialize("client_meta_data")]
        public string ClientMetadata; // (Query-Clients können hier mit clientupdate für sich selbst etwas speichern)

        [QuerySerialize("client_is_recording")]
        public bool IsClientRecording;

        [QuerySerialize("client_database_id")]
        public int ClientDatabaseId;

        [QuerySerialize("client_channel_group_id")]
        public int ClientChannelGroupId;

        [QuerySerialize("client_servergroups")]
        public string ClientServerGroups; // ??

        [QuerySerialize("client_away")]
        public bool IsClientAway;

        [QuerySerialize("client_away_message")]
        public string ClientAwayMessage;

        [QuerySerialize("client_type")]
        public ClientType Type; // („1“ für Query, „0“ für Voice)

        [QuerySerialize("client_flag_avatar")]
        public string ClientAvatarFlag;// ??

        [QuerySerialize("client_talk_power")]
        public int ClientTalkPower;

        [QuerySerialize("client_talk_request")]
        public int ClientRequestedTalkPower;

        [QuerySerialize("client_talk_request_msg")]
        public string ClientTaslkPowerRequestMessage;

        [QuerySerialize("client_description")]
        public string ClientDescription;

        [QuerySerialize("client_is_talker")]
        public bool IsClientTalker;

        [QuerySerialize("client_is_priority_speaker")]
        public bool IsClientHighPrioritySpeaker;

        [Obsolete]
        [QuerySerialize("client_unread_messages")]
        public int ClientUnreadMessages; // (hier immer noch vorhanden, obwohl es aus clientinfo längst gelöscht wurde)

        [QuerySerialize("client_nickname_phonetic")]
        public string ClientPhoneticName;

        [QuerySerialize("client_needed_serverquery_view_power")]
        public bool ClientNeededServerQueryViewPower; // (Änderung für Voice-Nutzer während einer Sitzung manchmal nicht rückwirkend, public funktioniert; jedoch bei Gruppenwechsel)

        [QuerySerialize("client_icon_id")]
        public long ClientIconId;

        [QuerySerialize("client_is_channel_commander")]
        public bool IsClientChannelCommander;

        [QuerySerialize("client_country")]
        public string ClientCountryCode;

        [QuerySerialize("client_channel_group_inherited_channel_id")]
        public int ClientInheritedChannelGroupFromChannelId;

        [QuerySerialize("client_badges")]
        public string ClientBadges; // (leer bei Query- und zu alten Clients, sonst in sich selbst parametrisierter String)
    }

    public class ChannelEdited : InfokerInformation
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }

    public class ChannelDescriptionChanged : Notify
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }

    public class ChannelPasswordChanged : Notify
    {
        [QuerySerialize("cid")]
        public int ChannelId;
    }

    public abstract class InfokerInformation : Notify
    {
        [QuerySerialize("reasonid")]
        public ReasonId Reason;

        [QuerySerialize("invokerid")]
        public int InvokerId;

        [QuerySerialize("invokername")]
        public string InvokerName;

        [QuerySerialize("invokeruid")]
        public string InvokerUid;
    }

    public class ChannelMoved : InfokerInformation
    {
        [QuerySerialize("cid")]
        public int ChannelId;

        [QuerySerialize("cpid")]
        public int ParentChannelId;

        [QuerySerialize("order")]
        public int Order;
    }
}
