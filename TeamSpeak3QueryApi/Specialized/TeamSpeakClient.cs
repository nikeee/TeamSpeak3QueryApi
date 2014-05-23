using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    public class TeamSpeakClient
    {
        private readonly QueryClient _client;

        public TeamSpeakClient(QueryClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (!client.IsConnected)
                throw new ArgumentException("client is not connected.");
            _client = client;
        }

        private readonly List<Tuple<NotificationType, object, Action<NotificationData>>> _callbacks = new List<Tuple<NotificationType, object, Action<NotificationData>>>();
        public void Subscribe<T>(NotificationType notification, Action<IReadOnlyCollection<T>> callback)
            where T : Notify
        {
            Action<NotificationData> cb = data => callback(NotificationDataProxy.SerializeGeneric<T>(data));

            _callbacks.Add(Tuple.Create(notification, callback as object, cb));
            _client.Subscribe(notification.ToString(), cb);
        }
        public void Unsubscribe(NotificationType notification)
        {
            var cbts = _callbacks.Where(tp => tp.Item1 == notification).ToList();
            cbts.ForEach(k => _callbacks.Remove(k));
            _client.Unsubscribe(notification.ToString());
        }
        public void Unsubscribe<T>(NotificationType notification, Action<IReadOnlyCollection<T>> callback)
            where T : Notify
        {
            var cbt = _callbacks.SingleOrDefault(t => t.Item1 == notification && t.Item2 == callback as object);
            if (cbt != null)
                _client.Unsubscribe(notification.ToString(), cbt.Item3);
        }
    }

    public abstract class Notify
    {

    }

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
        public int ClientIconId;

        [QuerySerialize("client_is_channel_commander")]
        public bool IsClientChannelCommander;

        [QuerySerialize("client_country")]
        public string ClientCountryCode;

        [QuerySerialize("client_channel_group_inherited_channel_id")]
        public int ClientInheritedChannelGroupFromChannelId;

        [QuerySerialize("client_badges")]
        public string ClientBadges; // (leer bei Query- und zu alten Clients, sonst in sich selbst parametrisierter String)
    }
}
