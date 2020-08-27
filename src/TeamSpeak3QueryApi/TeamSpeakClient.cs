using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.FileTransfer;
using TeamSpeak3QueryApi.Net.Query;
using TeamSpeak3QueryApi.Net.Enums;
using TeamSpeak3QueryApi.Net.Extensions;
using TeamSpeak3QueryApi.Net.Query.Parameters;
using TeamSpeak3QueryApi.Net.Query.Responses;
using TeamSpeak3QueryApi.Net.Query.Enums;
using TeamSpeak3QueryApi.Net.Query.Notifications;

namespace TeamSpeak3QueryApi.Net
{
    public class TeamSpeakClient : IDisposable
    {
        public QueryClient Client { get; }

        // TODO: Migrate to ValueTuples
        private readonly List<Tuple<NotificationType, object, Action<NotificationData>>> _callbacks = new List<Tuple<NotificationType, object, Action<NotificationData>>>();

        private readonly FileTransferClient _fileTransferClient;

        #region Ctors

        /// <summary>Creates a new instance of <see cref="TeamSpeakClient"/> using the <see cref="QueryClient.DefaultHost"/> and <see cref="QueryClient.DefaultPort"/>.</summary>
        public TeamSpeakClient()
            : this(QueryClient.DefaultHost, TelnetQueryClient.DefaultPort, Protocol.Telnet)
        { }

        /// <summary>Creates a new instance of <see cref="TeamSpeakClient"/> using the provided host TCP port.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        /// <param name="port">The TCP port of the Query API server.</param>
        public TeamSpeakClient(string hostName, int port = TelnetQueryClient.DefaultPort, Protocol type = Protocol.Telnet)
        {
            switch (type)
            {
                case Protocol.Telnet:
                    Client = new TelnetQueryClient(hostName, port);
                    break;
                case Protocol.SSH:
                    Client = new SshQueryClient(hostName, port);
                    break;
            }
            _fileTransferClient = new FileTransferClient(hostName);
        }

        #endregion

        public Task ConnectAsync() => Client.ConnectAsync();
        public void Connect(string username, string password) => Client.Connect(username, password);


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

        public Task LoginAsync(string userName, string password)
        {
            if (Client.ConnectionType == Protocol.SSH)
                throw new InvalidOperationException("Login is not needed in ssh protocol");

            return Client.SendAsync("login", new Parameter("client_login_name", userName), new Parameter("client_login_password", password));
        }

        public Task LogoutAsync() => Client.SendAsync("logout");

        public Task UseServerAsync(int serverId)
        {
            return Client.SendAsync("use", new Parameter("sid", serverId.ToString(CultureInfo.InvariantCulture)));
        }

        public async Task<WhoAmI> WhoAmIAsync()
        {
            var res = await Client.SendAsync("whoami").ConfigureAwait(false);
            var proxied = DataProxy.SerializeGeneric<WhoAmI>(res);
            return proxied.FirstOrDefault();
        }

        #region Notification Methods

        public Task RegisterChannelNotificationAsync(int channelId) => RegisterNotificationAsync(NotificationEventTarget.Channel, channelId);
        public Task RegisterAllChannelNotificationAsync() => RegisterNotificationAsync(NotificationEventTarget.Channel, 0);
        public Task RegisterServerNotificationAsync() => RegisterNotificationAsync(NotificationEventTarget.Server, -1);
        public Task RegisterTextServerNotificationAsync() => RegisterNotificationAsync(NotificationEventTarget.TextServer, -1);
        public Task RegisterTextChannelNotificationAsync() => RegisterNotificationAsync(NotificationEventTarget.TextChannel, -1);
        public Task RegisterTextPrivateNotificationAsync() => RegisterNotificationAsync(NotificationEventTarget.TextPrivate, -1);
        private Task RegisterNotificationAsync(NotificationEventTarget target, int channelId)
        {
            var ev = new Parameter("event", target.ToString().ToLowerInvariant());
            if (target == NotificationEventTarget.Channel)
                return Client.SendAsync("servernotifyregister", ev, new Parameter("id", channelId));
            return Client.SendAsync("servernotifyregister", ev);
        }

        #endregion

        #region Client Methods

        #region MoveClient

        public Task MoveClientAsync(int clientId, int targetChannelId) => MoveClientAsync(new[] { clientId }, targetChannelId);
        public Task MoveClientAsync(int clientId, int targetChannelId, string channelPassword) => MoveClientAsync(new[] { clientId }, targetChannelId, channelPassword);

        public Task MoveClientAsync(IEnumerable<GetClientInfo> clients, int targetChannelId)
        {
            var clIds = clients.Select(c => c.Id).ToArray();
            return MoveClientAsync(clIds, targetChannelId);
        }
        public Task MoveClientAsync(IEnumerable<GetClientInfo> clients, int targetChannelId, string channelPassword)
        {
            var clIds = clients.Select(c => c.Id).ToArray();
            return MoveClientAsync(clIds, targetChannelId, channelPassword);
        }

        public Task MoveClientAsync(IList<int> clientIds, int targetChannelId)
        {
            return Client.SendAsync("clientmove",
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i)).ToArray()),
                new Parameter("cid", targetChannelId));
        }
        public Task MoveClientAsync(IList<int> clientIds, int targetChannelId, string channelPassword)
        {
            return Client.SendAsync("clientmove",
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i)).ToArray()),
                new Parameter("cid", targetChannelId),
                new Parameter("cpw", channelPassword));
        }

        #endregion
        #region KickClient

        public Task KickClientAsync(int clientId, KickOrigin from) => KickClientAsync(new[] { clientId }, from);
        public Task KickClientAsync(int clientId, KickOrigin from, string reasonMessage) => KickClientAsync(new[] { clientId }, from, reasonMessage);
        public Task KickClientAsync(GetClientInfo client, KickOrigin from) => KickClientAsync(client.Id, from);
        public Task KickClientAsync(IEnumerable<GetClientInfo> clients, KickOrigin from, string v)
        {
            var clIds = clients.Select(c => c.Id).ToArray();
            return KickClientAsync(clIds, from);
        }
        public Task KickClientAsync(IList<int> clientIds, KickOrigin from)
        {
            return Client.SendAsync("clientkick",
                new Parameter("reasonid", (int)from),
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i)).ToArray()));
        }
        public Task KickClientAsync(IList<int> clientIds, KickOrigin from, string reasonMessage)
        {
            return Client.SendAsync("clientkick",
                new Parameter("reasonid", (int)from),
                new Parameter("reasonmsg", reasonMessage),
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i)).ToArray()));
        }

        #endregion
        #region BanClient

        public Task<IReadOnlyList<ClientBan>> BanClientAsync(GetClientInfo client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return BanClientAsync(client.Id);
        }
        public Task<IReadOnlyList<ClientBan>> BanClientAsync(GetClientInfo client, TimeSpan duration)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return BanClientAsync(client.Id, duration);
        }
        public Task<IReadOnlyList<ClientBan>> BanClientAsync(GetClientInfo client, TimeSpan duration, string reason)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return BanClientAsync(client.Id, duration, reason);
        }

        public async Task<IReadOnlyList<ClientBan>> BanClientAsync(int clientId)
        {
            var res = await Client.SendAsync("banclient",
                new Parameter("clid", clientId))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<ClientBan>(res);
        }
        public async Task<IReadOnlyList<ClientBan>> BanClientAsync(int clientId, TimeSpan duration)
        {
            var res = await Client.SendAsync("banclient",
                new Parameter("clid", clientId),
                new Parameter("time", (int)Math.Ceiling(duration.TotalSeconds)))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<ClientBan>(res);
        }
        public async Task<IReadOnlyList<ClientBan>> BanClientAsync(int clientId, TimeSpan duration, string reason)
        {
            var res = await Client.SendAsync("banclient",
                new Parameter("clid", clientId),
                new Parameter("time", (int)Math.Ceiling(duration.TotalSeconds)),
                new Parameter("banreason", reason ?? string.Empty))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<ClientBan>(res);
        }

        #endregion
        #region GetClients

        public async Task<IReadOnlyList<GetClientInfo>> GetClientsAsync()
        {
            var res = await Client.SendAsync("clientlist").ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetClientInfo>(res);
        }

        public async Task<IReadOnlyList<GetClientInfo>> GetClientsAsync(GetClientOptions options)
        {
            var optionList = options.GetFlagsName();
            var res = await Client.SendAsync("clientlist", null, optionList.ToArray()).ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetClientInfo>(res);
        }

        public Task<GetClientDetailedInfo> GetClientInfoAsync(GetClientInfo client) => GetClientInfoAsync(client.Id);

        public async Task<GetClientDetailedInfo> GetClientInfoAsync(int clientId)
        {
            var res = await Client.SendAsync("clientinfo",
                new Parameter("clid", clientId))
                .ConfigureAwait(false);

            return DataProxy.SerializeGeneric<GetClientDetailedInfo>(res).FirstOrDefault();
        }

        public async Task<IReadOnlyList<GetDbClientInfo>> GetDbClientsAsync(int start = 0, long duration = 9999999999) // Duration must be that long cause without this parameters it won't work. Bug from Teamspeak...
        {
            var res = await Client.SendAsync("clientdblist",
                new Parameter("start", start),
                new Parameter("duration", duration))
                .ConfigureAwait(false);

            return DataProxy.SerializeGeneric<GetDbClientInfo>(res);
        }

        #endregion

        #region GetServerGroups

        public async Task<IReadOnlyList<GetServerGroup>> GetServerGroupsAsync(int clientDatabaseId)
        {
            var res = await Client.SendAsync("servergroupsbyclientid", new Parameter("cldbid", clientDatabaseId)).ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetServerGroup>(res);
        }

        public Task<IReadOnlyList<GetServerGroup>> GetServerGroupsAsync(GetClientInfo clientInfo) => GetServerGroupsAsync(clientInfo.DatabaseId);

        public Task<IReadOnlyList<GetServerGroup>> GetServerGroupsAsync(WhoAmI clientInfo) => GetServerGroupsAsync(clientInfo.DatabaseId);

        #endregion

        #region AddServerGroup

        #region One User

        public Task AddServerGroupAsync(int serverGroupId, int clientDatabaseId) => AddServerGroupAsync(serverGroupId, new int[] { clientDatabaseId });

        public Task AddServerGroupAsync(int serverGroupId, GetClientInfo clientInfo) => AddServerGroupAsync(serverGroupId, clientInfo.DatabaseId);

        public Task AddServerGroupAsync(GetServerGroup serverGroup, int clientDatabaseId) => AddServerGroupAsync(serverGroup.Id, clientDatabaseId);

        public Task AddServerGroupAsync(GetServerGroup serverGroup, GetClientInfo clientInfo) => AddServerGroupAsync(serverGroup.Id, clientInfo.DatabaseId);

        #endregion

        #region Multiple Users

        public Task AddServerGroupAsync(int serverGroupId, IEnumerable<GetClientInfo> clientInfo) => AddServerGroupAsync(serverGroupId, clientInfo.Select(info => info.DatabaseId));

        public Task AddServerGroupAsync(GetServerGroup serverGroup, IEnumerable<int> clientDatabaseIds) => AddServerGroupAsync(serverGroup.Id, clientDatabaseIds);

        public Task AddServerGroupAsync(GetServerGroup serverGroup, IEnumerable<GetClientInfo> clientInfo) => AddServerGroupAsync(serverGroup.Id, clientInfo.Select(info => info.DatabaseId));

        public Task AddServerGroupAsync(int serverGroupId, IEnumerable<int> clientDatabaseIds)
        {
            return Client.SendAsync("servergroupaddclient",
                new Parameter("sgid", serverGroupId),
                new Parameter("cldbid", clientDatabaseIds.Select(id => new ParameterValue(id)).ToArray()));
        }

        #endregion

        #endregion

        #region RemoveServerGroup

        #region One User

        public Task RemoveServerGroupAsync(int serverGroupId, int clientDatabaseId) => RemoveServerGroupAsync(serverGroupId, new int[] { clientDatabaseId });

        public Task RemoveServerGroupAsync(int serverGroupId, GetClientInfo clientInfo) => RemoveServerGroupAsync(serverGroupId, clientInfo.DatabaseId);

        public Task RemoveServerGroupAsync(GetServerGroup serverGroup, int clientDatabaseId) => RemoveServerGroupAsync(serverGroup.Id, clientDatabaseId);

        public Task RemoveServerGroupAsync(GetServerGroup serverGroup, GetClientInfo clientInfo) => RemoveServerGroupAsync(serverGroup.Id, clientInfo.DatabaseId);

        #endregion

        #region Multiple Users

        public Task RemoveServerGroupAsync(int serverGroupId, IEnumerable<GetClientInfo> clientInfo) => RemoveServerGroupAsync(serverGroupId, clientInfo.Select(info => info.DatabaseId));

        public Task RemoveServerGroupAsync(GetServerGroup serverGroup, IEnumerable<int> clientDatabaseIds) => RemoveServerGroupAsync(serverGroup.Id, clientDatabaseIds);

        public Task RemoveServerGroupAsync(GetServerGroup serverGroup, IEnumerable<GetClientInfo> clientInfo) => RemoveServerGroupAsync(serverGroup.Id, clientInfo.Select(info => info.DatabaseId));

        public Task RemoveServerGroupAsync(int serverGroupId, IEnumerable<int> clientDatabaseIds)
        {
            return Client.SendAsync("servergroupdelclient",
                new Parameter("sgid", serverGroupId),
                new Parameter("cldbid", clientDatabaseIds.Select(id => new ParameterValue(id)).ToArray()));
        }

        #endregion

        #endregion

        #endregion

        #region Channel Methods

        #region GetChannels

        public async Task<IReadOnlyList<GetChannelListInfo>> GetChannelsAsync()
        {
            var res = await Client.SendAsync("channellist").ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetChannelListInfo>(res);
        }

        public async Task<IReadOnlyList<GetChannelListInfo>> GetChannelsAsync(GetChannelOptions options)
        {
            var optionList = options.GetFlagsName();
            var res = await Client.SendAsync("channellist", null, optionList.ToArray()).ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetChannelListInfo>(res);
        }

        #endregion
        #region GetChannelInfo

        public Task<GetChannelInfo> GetChannelInfoAsync(GetChannelListInfo channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            return GetChannelInfoAsync(channel.Id);
        }

        public async Task<GetChannelInfo> GetChannelInfoAsync(int channelId)
        {
            var res = await Client.SendAsync("channelinfo",
                new Parameter("cid", channelId))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetChannelInfo>(res).FirstOrDefault();
        }

        #endregion
        #region FindChannel

        public async Task<IReadOnlyCollection<FoundChannel>> FindChannelAsync()
        {
            var res = await Client.SendAsync("channelfind").ConfigureAwait(false);
            return DataProxy.SerializeGeneric<FoundChannel>(res);
        }
        public async Task<IReadOnlyCollection<FoundChannel>> FindChannelAsync(string pattern)
        {
            var res = await Client.SendAsync("channelfind",
                new Parameter("pattern", pattern ?? string.Empty))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<FoundChannel>(res);
        }

        #endregion
        #region MoveChannel

        public Task MoveChannelAsync(GetChannelListInfo channel, GetChannelListInfo parent)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            return MoveChannelAsync(channel.Id, parent.Id);
        }
        public Task MoveChannelAsync(GetChannelListInfo channel, GetChannelListInfo parent, int order)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            return MoveChannelAsync(channel.Id, parent.Id, order);
        }

        public Task MoveChannelAsync(int channelId, int parentChannelId)
        {
            return Client.SendAsync("channelmove",
                new Parameter("cid", channelId),
                new Parameter("cpid", parentChannelId));
        }
        public Task MoveChannelAsync(int channelId, int parentChannelId, int order)
        {
            return Client.SendAsync("channelmove",
                new Parameter("cid", channelId),
                new Parameter("cpid", parentChannelId),
                new Parameter("order", order));
        }

        #endregion
        #region CreateChannel

        // Region setting properties not supported yet

        public async Task<CreatedChannel> CreateChannelAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var res = await Client.SendAsync("channelcreate",
                new Parameter("channel_name", name))
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<CreatedChannel>(res).FirstOrDefault();
        }

        public async Task<CreatedChannel> CreateChannelAsync(string name, EditChannelInfo channel)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var addParameters = new List<Parameter>
            {
                new Parameter("channel_name", name),
            };

            if (channel.Topic != null) { addParameters.Add(new Parameter("channel_topic", channel.Topic)); }
            if (channel.Description != null) { addParameters.Add(new Parameter("channel_description", channel.Description)); }
            if (channel.Password != null) { addParameters.Add(new Parameter("channel_password", channel.Password)); }
            if (channel.Codec != null) { addParameters.Add(new Parameter("channel_codec", (int)channel.Codec)); }
            if (channel.CodecQuality != null) { addParameters.Add(new Parameter("channel_codec_quality", channel.CodecQuality)); }
            if (channel.MaxClients != null) { addParameters.Add(new Parameter("channel_maxclients", channel.MaxClients)); }
            if (channel.MaxFamilyClients != null) { addParameters.Add(new Parameter("channel_maxfamilyclients", channel.MaxFamilyClients)); }
            if (channel.Order != null) { addParameters.Add(new Parameter("channel_order", channel.Order)); }
            if (channel.IsPermanent != null) { addParameters.Add(new Parameter("channel_flag_permanent", channel.IsPermanent)); }
            if (channel.IsSemiPermanent != null) { addParameters.Add(new Parameter("channel_flag_semi_permanent", channel.IsSemiPermanent)); }
            if (channel.IsTemporary != null) { addParameters.Add(new Parameter("channel_flag_temporary", channel.IsTemporary)); }
            if (channel.IsDefaultChannel != null) { addParameters.Add(new Parameter("channel_flag_default", channel.IsDefaultChannel)); }
            if (channel.IsMaxClientsUnlimited != null) { addParameters.Add(new Parameter("channel_flag_maxclients_unlimited", channel.IsMaxClientsUnlimited)); }
            if (channel.IsMaxFamilyClientsUnlimited != null) { addParameters.Add(new Parameter("channel_flag_maxfamilyclients_unlimited", channel.IsMaxFamilyClientsUnlimited)); }
            if (channel.IsMaxFamilyClientsInherited != null) { addParameters.Add(new Parameter("channel_flag_maxfamilyclients_inherited", channel.IsMaxFamilyClientsInherited)); }
            if (channel.NeededTalkPower != null) { addParameters.Add(new Parameter("channel_needed_talk_power", channel.NeededTalkPower)); }
            if (channel.PhoneticName != null) { addParameters.Add(new Parameter("channel_name_phonetic", channel.PhoneticName)); }
            if (channel.IconId != null) { addParameters.Add(new Parameter("channel_icon_id", (int)channel.IconId)); }
            if (channel.IsCodecUnencrypted != null) { addParameters.Add(new Parameter("channel_codec_is_unencrypted", channel.IsCodecUnencrypted)); }
            if (channel.ParentChannelId != null) { addParameters.Add(new Parameter("cpid", channel.ParentChannelId)); }

            var res = await Client
                .SendAsync("channelcreate", addParameters.ToArray())
                .ConfigureAwait(false);
            return DataProxy.SerializeGeneric<CreatedChannel>(res).FirstOrDefault();
        }

        #endregion
        #region DeleteChannel

        public Task DeleteChannelAsync(GetChannelListInfo channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            return DeleteChannelAsync(channel.Id);
        }
        public Task DeleteChannelAsync(GetChannelListInfo channel, bool force)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            return DeleteChannelAsync(channel.Id, force);
        }

        public Task DeleteChannelAsync(int channelId)
        {
            return Client.SendAsync("channeldelete",
                new Parameter("cid", channelId));
        }
        public Task DeleteChannelAsync(int channelId, bool force)
        {
            return Client.SendAsync("channeldelete",
                new Parameter("cid", channelId),
                new Parameter("force", force));
        }

        #endregion
        #region EditChannel

        public Task EditChannelAsync(int channelId, EditChannelInfo channel)
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
            if (channel.ParentChannelId != null) { updateParameters.Add(new Parameter("cpid", channel.ParentChannelId)); }

            return Client.SendAsync("channeledit", updateParameters.ToArray());
        }

        #endregion
        #region ChannelAddPerm
        public Task ChannelAddPermAsync(int channelId, string permsId, int permValue)
        {
            return Client.SendAsync("channeladdperm",
                new Parameter("cid", channelId),
                new Parameter("permsid", permsId),
                new Parameter("permvalue", permValue));
        }
        #endregion

        #endregion

        #region Server Methods

        #region GetServers

        public async Task<IReadOnlyList<GetServerListInfo>> GetServersAsync()
        {
            var res = await Client.SendAsync("serverlist").ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetServerListInfo>(res);
        }

        public async Task<IReadOnlyList<GetServerListInfo>> GetServersAsync(GetServerOptions options)
        {
            var optionList = options.GetFlagsName();
            var res = await Client.SendAsync("serverlist", null, optionList.ToArray()).ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetServerListInfo>(res);
        }

        public async Task<IReadOnlyList<GetServerGroupListInfo>> GetServerGroupsAsync()
        {
            var res = await Client.SendAsync("servergrouplist").ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetServerGroupListInfo>(res);
        }

        public async Task<IReadOnlyList<GetServerGroupClientList>> GetServerGroupClientListAsync(int serverGroupDatabaseId)
        {
            var res = await Client.SendAsync("servergroupclientlist", new Parameter("sgid", serverGroupDatabaseId)).ConfigureAwait(false);
            return DataProxy.SerializeGeneric<GetServerGroupClientList>(res);
        }

        #endregion

        #endregion

        #region Message Methods

        #region SendTextMessage

        public Task SendMessageAsync(string message, GetServerListInfo targetServer)
        {
            if (targetServer == null)
                throw new ArgumentNullException(nameof(targetServer));
            return SendMessageAsync(message, MessageTarget.Server, targetServer.Id);
        }
        public Task SendMessageAsync(string message, GetChannelListInfo targetChannel)
        {
            if (targetChannel == null)
                throw new ArgumentNullException(nameof(targetChannel));
            return SendMessageAsync(message, MessageTarget.Channel, targetChannel.Id);
        }
        public Task SendMessageAsync(string message, GetClientInfo targetClient)
        {
            if (targetClient == null)
                throw new ArgumentNullException(nameof(targetClient));
            return SendMessageAsync(message, MessageTarget.Private, targetClient.Id);
        }
        public Task SendMessageAsync(string message, MessageTarget target, int targetId)
        {
            message = message ?? string.Empty;
            return Client.SendAsync("sendtextmessage",
                new Parameter("targetmode", (int)target),
                new Parameter("target", targetId),
                new Parameter("msg", message));
        }

        #endregion
        #region SendGlobalMessage

        public Task SendGlobalMessageAsync(string message)
        {
            return Client.SendAsync("gm",
                new Parameter("msg", message ?? string.Empty));
        }

        #endregion
        #region PokeClient

        public Task PokeClientAsync(GetClientInfo client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return PokeClientAsync(client.Id);
        }
        public Task PokeClientAsync(int clientId)
        {
            return PokeClientAsync(clientId, string.Empty);
        }

        public Task PokeClientAsync(GetClientInfo client, string message)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return PokeClientAsync(client.Id, message);
        }
        public Task PokeClientAsync(int clientId, string message)
        {
            return Client.SendAsync("clientpoke",
                new Parameter("msg", message ?? string.Empty),
                new Parameter("clid", clientId));
        }

        #endregion

        #region ChangeNickName
        public Task ChangeNickNameAsync(string nickName) => ChangeNickNameAsync(nickName, default);

        public Task ChangeNickNameAsync(string nickName, WhoAmI whoAmI)
        {
            if (whoAmI != null)
                whoAmI.NickName = nickName;
            return Client.SendAsync("clientupdate",
                new Parameter("client_nickname", nickName));
        }
        #endregion

        #endregion

        #region Filetransfer Methods

        #region CreateDirectory

        public Task CreateDirectoryAsync(int channelId, string dirPath) => CreateDirectoryAsync(channelId, string.Empty, dirPath);

        public Task CreateDirectoryAsync(int channelId, string channelPassword, string dirPath)
        {
            return Client.SendAsync("ftcreatedir",
                new Parameter("cid", channelId),
                new Parameter("cpw", channelPassword),
                new Parameter("dirname", NormalizePath(dirPath)));
        }

        #endregion

        #region DeleteFile

        public Task DeleteFileAsync(int channelId, string filePath) => DeleteFileAsync(channelId, string.Empty, new string[] { filePath });

        public Task DeleteFileAsync(int channelId, string channelPassword, string filePath) => DeleteFileAsync(channelId, channelPassword, new string[] { filePath });

        public Task DeleteFileAsync(int channelId, IEnumerable<string> filePaths) => DeleteFileAsync(channelId, string.Empty, filePaths);

        public Task DeleteFileAsync(int channelId, string channelPassword, IEnumerable<string> filePaths)
        {
            return Client.SendAsync("ftdeletefile",
                new Parameter("cid", channelId),
                new Parameter("cpw", channelPassword),
                new Parameter("name", filePaths.Select(path => new ParameterValue(NormalizePath(path))).ToArray()));
        }

        #endregion

        #region GetFileInfo

        public Task<GetFileInfo> GetFileInfoAsync(int channelId, string filePath) => GetFileInfoAsync(channelId, string.Empty, filePath);

        public async Task<GetFileInfo> GetFileInfoAsync(int channelId, string channelPassword, string filePath)
        {
            var res = await Client.SendAsync("ftgetfileinfo",
                new Parameter("cid", channelId),
                new Parameter("cpw", channelPassword),
                new Parameter("name", NormalizePath(filePath))).ConfigureAwait(false);

            return DataProxy.SerializeGeneric<GetFileInfo>(res).FirstOrDefault();
        }

        #endregion

        #region GetFileList

        public Task<IReadOnlyList<GetFiles>> GetFilesAsync(int channelId) => GetFilesAsync(channelId, string.Empty, "/");

        public Task<IReadOnlyList<GetFiles>> GetFilesAsync(int channelId, string dirPath) => GetFilesAsync(channelId, string.Empty, dirPath);

        public async Task<IReadOnlyList<GetFiles>> GetFilesAsync(int channelId, string channelPassword, string dirPath)
        {
            var res = await Client.SendAsync("ftgetfilelist",
                new Parameter("cid", channelId),
                new Parameter("cpw", channelPassword),
                new Parameter("path", NormalizePath(dirPath))).ConfigureAwait(false);

            return DataProxy.SerializeGeneric<GetFiles>(res);
        }

        #endregion

        #region MoveFile

        #region Same Channel

        public Task MoveFileAsync(int channelId, string oldFilePath, string newFilePath) => MoveFileAsync(channelId, string.Empty, oldFilePath, newFilePath);

        public Task MoveFileAsync(int channelId, string channelPassword, string oldFilePath, string newFilePath)
        {
            return Client.SendAsync("ftrenamefile",
                new Parameter("cid", channelId),
                new Parameter("cpw", channelPassword),
                new Parameter("oldname", NormalizePath(oldFilePath)),
                new Parameter("newname", NormalizePath(newFilePath)));
        }

        #endregion

        #region Other Channel

        public Task MoveFileAsync(int channelId, string oldFilePath, int targetChannelId, string newFilePath) => MoveFileAsync(channelId, string.Empty, oldFilePath, targetChannelId, string.Empty, newFilePath);

        public Task MoveFileAsync(int channelId, string channelPassword, string oldFilePath, int targetChannelId, string newFilePath) => MoveFileAsync(channelId, channelPassword, oldFilePath, targetChannelId, string.Empty, newFilePath);

        public Task MoveFileAsync(int channelId, string oldFilePath, int targetChannelId, string targetChannelPassword, string newFilePath) => MoveFileAsync(channelId, string.Empty, oldFilePath, targetChannelId, targetChannelPassword, newFilePath);

        public Task MoveFileAsync(int channelId, string channelPassword, string oldFilePath, int targetChannelId, string targetChannelPassword, string newFilePath)
        {
            return Client.SendAsync("ftrenamefile",
                new Parameter("cid", channelId),
                new Parameter("cpw", channelPassword),
                new Parameter("tcid", targetChannelId),
                new Parameter("tcpw", targetChannelPassword),
                new Parameter("oldname", NormalizePath(oldFilePath)),
                new Parameter("newname", NormalizePath(newFilePath)));
        }

        #endregion

        #endregion

        #region UploadFile

        public Task UploadFileAsync(int channelId, string filePath, byte[] data, bool overwrite = true, bool verify = true) => UploadFileAsync(channelId, string.Empty, filePath, data, overwrite, verify);

        public async Task UploadFileAsync(int channelId, string channelPassword, string filePath, byte[] data, bool overwrite = true, bool verify = true)
        {
            var res = await Client.SendAsync("ftinitupload",
                new Parameter("clientftfid", _fileTransferClient.GetFileTransferId()),
                new Parameter("cid", channelId),
                new Parameter("cpw", channelPassword),
                new Parameter("name", NormalizePath(filePath)),
                new Parameter("size", data.Length),
                new Parameter("overwrite", overwrite),
                new Parameter("resume", 0)).ConfigureAwait(false);

            var parsedRes = DataProxy.SerializeGeneric<InitUpload>(res).First();

            await _fileTransferClient.SendFileAsync(data, parsedRes.Port, parsedRes.FileTransferKey).ConfigureAwait(false);

            if (verify)
            {
                await VerifyUploadAsync(parsedRes.ServerFileTransferId).ConfigureAwait(false);
            }
        }

        public Task UploadFileAsync(int channelId, string filePath, Stream dataStream, long size, bool overwrite = true, bool verify = true) => UploadFileAsync(channelId, string.Empty, filePath, dataStream, size, overwrite, verify);

        public async Task UploadFileAsync(int channelId, string channelPassword, string filePath, Stream dataStream, long size, bool overwrite = true, bool verify = true)
        {
            var res = await Client.SendAsync("ftinitupload",
                new Parameter("clientftfid", _fileTransferClient.GetFileTransferId()),
                new Parameter("cid", channelId),
                new Parameter("cpw", channelPassword),
                new Parameter("name", NormalizePath(filePath)),
                new Parameter("size", size),
                new Parameter("overwrite", overwrite),
                new Parameter("resume", 0)).ConfigureAwait(false);

            var parsedRes = DataProxy.SerializeGeneric<InitUpload>(res).First();

            await _fileTransferClient.SendFileAsync(dataStream, parsedRes.Port, parsedRes.FileTransferKey).ConfigureAwait(false);

            if (verify)
            {
                await VerifyUploadAsync(parsedRes.ServerFileTransferId).ConfigureAwait(false);
            }
        }

        /// <summary>Waits until the server fully receives the file or throws an exception when the upload times out.</summary>
        private async Task VerifyUploadAsync(int serverFileTransferId)
        {
            long arrivedBytes = 0;
            var intervalMillis = 100;
            var timeoutMillis = 3000;
            var currentTimeoutMillis = intervalMillis * -1;

            while (true)
            {
                var transfers = await GetCurrentFileTransfersAsync();
                var currentTransfer = transfers.Where(transfer => transfer.ServerFileTransferId == serverFileTransferId).FirstOrDefault();

                if (currentTransfer == null)
                {
                    // Download finished
                    return;
                }

                if (currentTransfer.SizeDone == arrivedBytes)
                {
                    // No upload progress
                    currentTimeoutMillis += intervalMillis;

                    if (currentTimeoutMillis > timeoutMillis)
                    {
                        try
                        {
                            await StopFileTransferAsync(serverFileTransferId).ConfigureAwait(false);
                        }
                        catch
                        { }

                        throw new FileTransferException("File upload timed out.");
                    }
                }
                else
                {
                    // Upload progress
                    currentTimeoutMillis = 0;
                    arrivedBytes = currentTransfer.SizeDone;
                }

                await Task.Delay(intervalMillis).ConfigureAwait(false);
            }
        }

        #endregion

        #region DownloadFile

        public Task<Stream> DownloadFileAsync(int channelId, string filePath) => DownloadFileAsync(channelId, string.Empty, filePath);

        public async Task<Stream> DownloadFileAsync(int channelId, string channelPassword, string filePath)
        {
            var res = await Client.SendAsync("ftinitdownload",
                new Parameter("clientftfid", _fileTransferClient.GetFileTransferId()),
                new Parameter("cid", channelId),
                new Parameter("cpw", channelPassword),
                new Parameter("name", NormalizePath(filePath)),
                new Parameter("seekpos", 0)).ConfigureAwait(false);

            var parsedRes = DataProxy.SerializeGeneric<InitDownload>(res).First();

            if (parsedRes.Size > int.MaxValue)
            {
                throw new FileTransferException("The file is too big for a single byte array.");
            }

            return await _fileTransferClient.ReceiveFileAsync((int)parsedRes.Size, parsedRes.Port, parsedRes.FileTransferKey).ConfigureAwait(false);
        }

        #endregion

        #region GetCurrentFileTransfers

        public async Task<IReadOnlyList<GetCurrentFileTransfer>> GetCurrentFileTransfersAsync()
        {
            try
            {
                var res = await Client.SendAsync("ftlist").ConfigureAwait(false);

                return DataProxy.SerializeGeneric<GetCurrentFileTransfer>(res);
            }
            catch (QueryException ex)
            {
                if (ex.Error.Id == 1281)
                {
                    // For some reason this error occurs when there are no active file transfers
                    return new ReadOnlyCollection<GetCurrentFileTransfer>(new GetCurrentFileTransfer[0]);
                }
                else
                {
                    throw ex;
                }
            }
        }

        #endregion

        private Task StopFileTransferAsync(int serverFileTransferId, bool delete = true)
        {
            return Client.SendAsync("ftstop",
                new Parameter("serverftfid", serverFileTransferId),
                new Parameter("delete", delete));
        }

        private string NormalizePath(string path)
        {
            // Replace a sequence of backslashes with one forward slash
            var result = Regex.Replace(path, @"\\+", "/");

            // Make sure that the path starts with a slash
            if (!result.StartsWith("/"))
            {
                result = "/" + result;
            }

            return result;
        }

        #endregion

        #endregion

        #region IDisposable support

        /// <summary>Finalizes the object.</summary>
        ~TeamSpeakClient()
        {
            Dispose(false);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <param name="disposing">A value indicating whether the object is disposing or finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Client?.Dispose();
            }
        }

        #endregion
    }
}
