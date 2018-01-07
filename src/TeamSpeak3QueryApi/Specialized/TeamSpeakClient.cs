using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamSpeak3QueryApi.Net.Specialized.Responses;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    public class TeamSpeakClient
    {
        public QueryClient Client { get; }

        // TODO: Migrate to ValueTuples
        private readonly List<Tuple<NotificationType, object, Action<NotificationData>>> _callbacks = new List<Tuple<NotificationType, object, Action<NotificationData>>>();

        #region Ctors

        /// <summary>Creates a new instance of <see cref="TeamSpeakClient"/> using the <see cref="QueryClient.DefaultHost"/> and <see cref="QueryClient.DefaultPort"/>.</summary>
        public TeamSpeakClient()
            : this(QueryClient.DefaultHost, QueryClient.DefaultPort)
        { }

        /// <summary>Creates a new instance of <see cref="TeamSpeakClient"/> using the provided host and the <see cref="QueryClient.DefaultPort"/>.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        public TeamSpeakClient(string hostName)
            : this(hostName, QueryClient.DefaultPort)
        { }

        /// <summary>Creates a new instance of <see cref="TeamSpeakClient"/> using the provided host TCP port.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        /// <param name="port">The TCP port of the Query API server.</param>
        public TeamSpeakClient(string hostName, int port) => Client = new QueryClient(hostName, port);

        #endregion

        public Task Connect() => Client.Connect();

        #region Subscriptions

        public void Subscribe<T>(Action<IReadOnlyCollection<T>> callback)
            where T : Notification
        {
            var notification = GetNotificationType<T>();

            Action<NotificationData> cb = data => callback(DataProxy.SerializeGeneric<T>(data.Payload));

            _callbacks.Add(Tuple.Create(notification, callback as object, cb));
            Client.Subscribe(notification.ToString(), cb);
        }
        public void Unsubscribe<T>()
            where T : Notification
        {
            var notification = GetNotificationType<T>();
            var cbts = _callbacks.Where(tp => tp.Item1 == notification).ToList();
            cbts.ForEach(k => _callbacks.Remove(k));
            Client.Unsubscribe(notification.ToString());
        }
        public void Unsubscribe<T>(Action<IReadOnlyCollection<T>> callback)
            where T : Notification
        {
            var notification = GetNotificationType<T>();
            var cbt = _callbacks.SingleOrDefault(t => t.Item1 == notification && t.Item2 == callback as object);
            if (cbt != null)
                Client.Unsubscribe(notification.ToString(), cbt.Item3);
        }

        private static NotificationType GetNotificationType<T>()
        {
            if (!Enum.TryParse(typeof(T).Name, out NotificationType notification)) // This may violate the generic pattern. May change this later.
                throw new ArgumentException("The specified generic parameter is not a supported NotificationType."); // For this time, we only support class-internal types which are listed in NotificationType
            return notification;
        }

        #endregion
        #region Implented api methods

        public Task Login(string userName, string password)
        {
            return Client.Send("login", new Parameter("client_login_name", userName), new Parameter("client_login_password", password));
        }

        public Task UseServer(int serverId)
        {
            return Client.Send("use", new Parameter("sid", serverId.ToString(CultureInfo.InvariantCulture)));
        }

        public async Task<WhoAmI> WhoAmI()
        {
            var res = await Client.Send("whoami").ConfigureAwait(false);
            var proxied = DataProxy.SerializeGeneric<WhoAmI>(res);
            return proxied.FirstOrDefault();
        }

        #region Notification Methods

        public Task RegisterChannelNotification(int channelId) => RegisterNotification(NotificationEventTarget.Channel, channelId);
        public Task RegisterServerNotification() => RegisterNotification(NotificationEventTarget.Server, -1);
        public Task RegisterTextServerNotification() => RegisterNotification(NotificationEventTarget.TextServer, -1);
        public Task RegisterTextChannelNotification() => RegisterNotification(NotificationEventTarget.TextChannel, -1);
        public Task RegisterTextPrivateNotification() => RegisterNotification(NotificationEventTarget.TextPrivate, -1);
        private Task RegisterNotification(NotificationEventTarget target, int channelId)
        {
            var ev = new Parameter("event", target.ToString().ToLowerInvariant());
            if (target == NotificationEventTarget.Channel)
                return Client.Send("servernotifyregister", ev, new Parameter("id", channelId));
            return Client.Send("servernotifyregister", ev);
        }

        #endregion

        #region Client Methods

        #region MoveClient

        public Task MoveClient(int clientId, int targetChannelId) => MoveClient(new[] { clientId }, targetChannelId);
        public Task MoveClient(int clientId, int targetChannelId, string channelPassword) => MoveClient(new[] { clientId }, targetChannelId, channelPassword);

        public Task MoveClient(IEnumerable<GetClientsInfo> clients, int targetChannelId)
        {
            var clIds = clients.Select(c => c.Id).ToArray();
            return MoveClient(clIds, targetChannelId);
        }
        public Task MoveClient(IEnumerable<GetClientsInfo> clients, int targetChannelId, string channelPassword)
        {
            var clIds = clients.Select(c => c.Id).ToArray();
            return MoveClient(clIds, targetChannelId, channelPassword);
        }

        public Task MoveClient(IList<int> clientIds, int targetChannelId)
        {
            return Client.Send("clientmove",
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i)).ToArray()),
                new Parameter("cid", targetChannelId));
        }
        public Task MoveClient(IList<int> clientIds, int targetChannelId, string channelPassword)
        {
            return Client.Send("clientmove",
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i)).ToArray()),
                new Parameter("cid", targetChannelId),
                new Parameter("cpw", channelPassword));
        }

        #endregion
        #region KickClient

        public Task KickClient(int clientId, KickOrigin from) => KickClient(new[] { clientId }, from);
        public Task KickClient(int clientId, KickOrigin from, string reasonMessage) => KickClient(new[] { clientId }, from, reasonMessage);
        public Task KickClient(GetClientsInfo client, KickOrigin from) => KickClient(client.Id, from);
        public Task KickClient(IEnumerable<GetClientsInfo> clients, KickOrigin from)
        {
            var clIds = clients.Select(c => c.Id).ToArray();
            return KickClient(clIds, from);
        }
        public Task KickClient(IList<int> clientIds, KickOrigin from)
        {
            return Client.Send("clientkick",
                new Parameter("reasonid", (int)from),
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i)).ToArray()));
        }
        public Task KickClient(IList<int> clientIds, KickOrigin from, string reasonMessage)
        {
            return Client.Send("clientkick",
                new Parameter("reasonid", (int)from),
                new Parameter("reasonmsg", reasonMessage),
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i)).ToArray()));
        }

        #endregion
        #region BanClient

        public Task<IReadOnlyList<ClientBan>> BanClient(GetClientsInfo client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return BanClient(client.Id);
        }
        public Task<IReadOnlyList<ClientBan>> BanClient(GetClientsInfo client, TimeSpan duration)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return BanClient(client.Id, duration);
        }
        public Task<IReadOnlyList<ClientBan>> BanClient(GetClientsInfo client, TimeSpan duration, string reason)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return BanClient(client.Id, duration, reason);
        }

        public async Task<IReadOnlyList<ClientBan>> BanClient(int clientId)
        {
            var res = await Client.Send("banclient",
                new Parameter("clid", clientId))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<ClientBan>(res);
        }
        public async Task<IReadOnlyList<ClientBan>> BanClient(int clientId, TimeSpan duration)
        {
            var res = await Client.Send("banclient",
                new Parameter("clid", clientId),
                new Parameter("time", (int)Math.Ceiling(duration.TotalSeconds)))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<ClientBan>(res);
        }
        public async Task<IReadOnlyList<ClientBan>> BanClient(int clientId, TimeSpan duration, string reason)
        {
            var res = await Client.Send("banclient",
                new Parameter("clid", clientId),
                new Parameter("time", (int)Math.Ceiling(duration.TotalSeconds)),
                new Parameter("banreason", reason ?? string.Empty))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<ClientBan>(res);
        }

        #endregion
        #region GetClients

        public async Task<IReadOnlyList<GetClientsInfo>> GetClients()
        {
            var res = await Client.Send("clientlist").ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetClientsInfo>(res);
        }

        public async Task<IReadOnlyList<GetClientsInfo>> GetClients(GetClientOptions options)
        {
            var optionList = options.GetFlagsName();
            var res = await Client.Send("clientlist", null, optionList.ToArray()).ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetClientsInfo>(res);
        }

        #endregion

        #endregion

        #region Channel Methods

        #region GetChannels

        public async Task<IReadOnlyList<GetChannelListInfo>> GetChannels()
        {
            var res = await Client.Send("channellist").ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetChannelListInfo>(res);
        }

        public async Task<IReadOnlyList<GetChannelListInfo>> GetChannels(GetChannelOptions options)
        {
            var optionList = options.GetFlagsName();
            var res = await Client.Send("channellist", null, optionList.ToArray()).ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetChannelListInfo>(res);
        }

        #endregion
        #region GetChannelInfo

        public Task<GetChannelInfo> GetChannelInfo(GetChannelListInfo channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            return GetChannelInfo(channel.Id);
        }

        public async Task<GetChannelInfo> GetChannelInfo(int channelId)
        {
            var res = await Client.Send("channelinfo",
                new Parameter("cid", channelId))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetChannelInfo>(res).FirstOrDefault();
        }

        #endregion
        #region FindChannel

        public async Task<IReadOnlyCollection<FoundChannel>> FindChannel()
        {
            var res = await Client.Send("channelfind").ConfigureAwait(false);
            return DataProxy.SerializeGeneric<FoundChannel>(res);
        }
        public async Task<IReadOnlyCollection<FoundChannel>> FindChannel(string pattern)
        {
            var res = await Client.Send("channelfind",
                new Parameter("pattern", pattern ?? string.Empty))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<FoundChannel>(res);
        }

        #endregion
        #region MoveChannel

        public Task MoveChannel(GetChannelListInfo channel, GetChannelListInfo parent)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            return MoveChannel(channel.Id, parent.Id);
        }
        public Task MoveChannel(GetChannelListInfo channel, GetChannelListInfo parent, int order)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            return MoveChannel(channel.Id, parent.Id, order);
        }

        public Task MoveChannel(int channelId, int parentChannelId)
        {
            return Client.Send("channelmove",
                new Parameter("cid", channelId),
                new Parameter("cpid", parentChannelId));
        }
        public Task MoveChannel(int channelId, int parentChannelId, int order)
        {
            return Client.Send("channelmove",
                new Parameter("cid", channelId),
                new Parameter("cpid", parentChannelId),
                new Parameter("order", order));
        }

        #endregion
        #region CreateChannel

        // Region setting properties not supported yet

        public async Task<CreatedChannel> CreateChannel(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var res = await Client.Send("channelcreate",
                new Parameter("channel_name", name))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<CreatedChannel>(res).FirstOrDefault();
        }

        #endregion
        #region DeleteChannel

        public Task DeleteChannel(GetChannelListInfo channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            return DeleteChannel(channel.Id);
        }
        public Task DeleteChannel(GetChannelListInfo channel, bool force)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            return DeleteChannel(channel.Id, force);
        }

        public Task DeleteChannel(int channelId)
        {
            return Client.Send("channeldelete",
                new Parameter("cid", channelId));
        }
        public Task DeleteChannel(int channelId, bool force)
        {
            return Client.Send("channeldelete",
                new Parameter("cid", channelId),
                new Parameter("force", force));
        }

        #endregion
        #region EditChannel

        public Task EditChannel(int channelId, EditChannelInfo channel)
        {
            var updateParameters = new List<Parameter>
            {
                new Parameter("cid", channelId),
            };

            if (channel.Name != null) { updateParameters.Add(new Parameter("channel_name", channel.Name)); }
            if (channel.Topic != null) { updateParameters.Add(new Parameter("channel_topic", channel.Topic)); }
            if (channel.Description != null) { updateParameters.Add(new Parameter("channel_description", channel.Description)); }
            if (channel.Password != null) { updateParameters.Add(new Parameter("channel_password", channel.Password)); }
            if (channel.Codec != null) { updateParameters.Add(new Parameter("channel_codec", (int)channel.Codec)); }
            if (channel.CodecQuality != null) { updateParameters.Add(new Parameter("channel_codec_quality", channel.CodecQuality)); }
            if (channel.MaxClients != null) { updateParameters.Add(new Parameter("channel_maxclients", channel.MaxClients)); }
            if (channel.MaxFamilyClients != null) { updateParameters.Add(new Parameter("channel_maxfamilyclients", channel.MaxFamilyClients)); }
            if (channel.Order != null) { updateParameters.Add(new Parameter("channel_order", channel.Order)); }
            if (channel.IsPermanent != null) { updateParameters.Add(new Parameter("channel_flag_permanent", channel.IsPermanent)); }
            if (channel.IsSemiPermanent != null) { updateParameters.Add(new Parameter("channel_flag_semi_permanent", channel.IsSemiPermanent)); }
            if (channel.IsTemporary != null) { updateParameters.Add(new Parameter("channel_flag_temporary", channel.IsTemporary)); }
            if (channel.IsDefaultChannel != null) { updateParameters.Add(new Parameter("channel_flag_default", channel.IsDefaultChannel)); }
            if (channel.IsMaxClientsUnlimited != null) { updateParameters.Add(new Parameter("channel_flag_maxclients_unlimited", channel.IsMaxClientsUnlimited)); }
            if (channel.IsMaxFamilyClientsUnlimited != null) { updateParameters.Add(new Parameter("channel_flag_maxfamilyclients_unlimited", channel.IsMaxFamilyClientsUnlimited)); }
            if (channel.IsMaxFamilyClientsInherited != null) { updateParameters.Add(new Parameter("channel_flag_maxfamilyclients_inherited", channel.IsMaxFamilyClientsInherited)); }
            if (channel.NeededTalkPower != null) { updateParameters.Add(new Parameter("channel_needed_talk_power", channel.NeededTalkPower)); }
            if (channel.PhoneticName != null) { updateParameters.Add(new Parameter("channel_name_phonetic", channel.PhoneticName)); }
            if (channel.IconId != null) { updateParameters.Add(new Parameter("channel_icon_id", (int)channel.IconId)); }
            if (channel.IsCodecUnencrypted != null) { updateParameters.Add(new Parameter("channel_codec_is_unencrypted", channel.IsCodecUnencrypted)); }
            if (channel.ParentChannelId != null) { updateParameters.Add(new Parameter("channel_cpid", channel.ParentChannelId)); }

            return Client.Send("channeledit", updateParameters.ToArray());
        }

        #endregion
        #region ChannelAddPerm
        public Task ChannelAddPerm(int channelId, string permsId, int permValue)
        {
            return Client.Send("channeladdperm",
                new Parameter("cid", channelId),
                new Parameter("permsid", permsId),
                new Parameter("permvalue", permValue));
        }
        #endregion

        #endregion

        #region Server Methods

        #region GetServers

        public async Task<IReadOnlyList<GetServerListInfo>> GetServers()
        {
            var res = await Client.Send("serverlist").ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetServerListInfo>(res);
        }

        public async Task<IReadOnlyList<GetServerListInfo>> GetServers(GetServerOptions options)
        {
            var optionList = options.GetFlagsName();
            var res = await Client.Send("serverlist", null, optionList.ToArray()).ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetServerListInfo>(res);
        }

        #endregion

        #endregion

        #region Message Methods

        #region SendTextMessage

        public Task SendMessage(string message, GetServerListInfo targetServer)
        {
            if (targetServer == null)
                throw new ArgumentNullException(nameof(targetServer));
            return SendMessage(message, MessageTarget.Server, targetServer.Id);
        }
        public Task SendMessage(string message, GetChannelListInfo targetChannel)
        {
            if (targetChannel == null)
                throw new ArgumentNullException(nameof(targetChannel));
            return SendMessage(message, MessageTarget.Channel, targetChannel.Id);
        }
        public Task SendMessage(string message, GetClientsInfo targetClient)
        {
            if (targetClient == null)
                throw new ArgumentNullException(nameof(targetClient));
            return SendMessage(message, MessageTarget.Private, targetClient.Id);
        }
        public Task SendMessage(string message, MessageTarget target, int targetId)
        {
            message = message ?? string.Empty;
            return Client.Send("sendtextmessage",
                new Parameter("targetmode", (int)target),
                new Parameter("target", targetId),
                new Parameter("msg", message));
        }

        #endregion
        #region SendGlobalMessage

        public Task SendGlobalMessage(string message)
        {
            return Client.Send("gm",
                new Parameter("msg", message ?? string.Empty));
        }

        #endregion
        #region PokeClient

        public Task PokeClient(GetClientsInfo client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return PokeClient(client.Id);
        }
        public Task PokeClient(int clientId)
        {
            return PokeClient(clientId, string.Empty);
        }

        public Task PokeClient(GetClientsInfo client, string message)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return PokeClient(client.Id, message);
        }
        public Task PokeClient(int clientId, string message)
        {
            return Client.Send("clientpoke",
                new Parameter("msg", message ?? string.Empty),
                new Parameter("clid", clientId));
        }

        #endregion

        #region ChangeNickName
        public Task ChangeNickName(string nickName) => ChangeNickName(nickName, default);

        public Task ChangeNickName(string nickName, WhoAmI whoAmI)
        {
            if (whoAmI != null)
                whoAmI.NickName = nickName;
            return Client.Send("clientupdate",
                new Parameter("client_nickname", nickName));
        }
        #endregion

        #endregion

        #endregion
    }
}
