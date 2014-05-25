using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamSpeak3QueryApi.Net.Specialized.Responses;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    public class TeamSpeakClient
    {
        private readonly QueryClient _client;
        public QueryClient Client { get { return _client; } }

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
        public TeamSpeakClient(string hostName, short port)
        {
            _client = new QueryClient(hostName, port);
        }

        #endregion

        public Task Connect()
        {
            return _client.Connect();
        }

        #region Subscriptions

        public void Subscribe<T>(Action<IReadOnlyCollection<T>> callback)
            where T : Notification
        {
            var notification = GetNotificationType<T>();

            Action<NotificationData> cb = data => callback(DataProxy.SerializeGeneric<T>(data.Payload));

            _callbacks.Add(Tuple.Create(notification, callback as object, cb));
            _client.Subscribe(notification.ToString(), cb);
        }
        public void Unsubscribe<T>()
            where T : Notification
        {
            var notification = GetNotificationType<T>();
            var cbts = _callbacks.Where(tp => tp.Item1 == notification).ToList();
            cbts.ForEach(k => _callbacks.Remove(k));
            _client.Unsubscribe(notification.ToString());
        }
        public void Unsubscribe<T>(Action<IReadOnlyCollection<T>> callback)
            where T : Notification
        {
            var notification = GetNotificationType<T>();
            var cbt = _callbacks.SingleOrDefault(t => t.Item1 == notification && t.Item2 == callback as object);
            if (cbt != null)
                _client.Unsubscribe(notification.ToString(), cbt.Item3);
        }

        private static NotificationType GetNotificationType<T>()
        {
            NotificationType notification;
            if (!Enum.TryParse(typeof(T).Name, out notification)) // This may violate the generic pattern. May change this later.
                throw new ArgumentException("The specified generic parameter is not a supported NotificationType."); // For this time, we only support class-internal types which are listed in NotificationType
            return notification;
        }

        #endregion
        #region Implented api methods

        public Task Login(string userName, string password)
        {
            return _client.Send("login", new Parameter("client_login_name", userName), new Parameter("client_login_password", password));
        }

        public Task UseServer(int serverId)
        {
            return _client.Send("use", new Parameter("sid", serverId.ToString(CultureInfo.InvariantCulture)));
        }

        public async Task<WhoAmI> WhoAmI()
        {
            var res = await _client.Send("whoami");
            var proxied = DataProxy.SerializeGeneric<WhoAmI>(res);
            return proxied.FirstOrDefault();
        }

        #region Notification Methods

        public Task RegisterChannelNotification(int channelId)
        {
            return RegisterNotification(NotificationEventTarget.Channel, channelId);
        }
        public Task RegisterServerNotification()
        {
            return RegisterNotification(NotificationEventTarget.Server, -1);
        }
        public Task RegisterTextServerNotification()
        {
            return RegisterNotification(NotificationEventTarget.TextServer, -1);
        }
        public Task RegisterTextChannelNotification()
        {
            return RegisterNotification(NotificationEventTarget.TextChannel, -1);
        }
        public Task RegisterTextPrivateNotification()
        {
            return RegisterNotification(NotificationEventTarget.TextPrivate, -1);
        }
        private Task RegisterNotification(NotificationEventTarget target, int channelId)
        {
            var ev = new Parameter("event", target.ToString().ToLowerInvariant());
            if (target == NotificationEventTarget.Channel)
                return _client.Send("servernotifyregister", ev, new Parameter("id", channelId));
            return _client.Send("servernotifyregister", ev);
        }

        #endregion

        #region Client Methods

        #region MoveClient

        public Task MoveClient(int clientId, int targetChannelId)
        {
            return MoveClient(new[] { clientId }, targetChannelId);
        }
        public Task MoveClient(int clientId, int targetChannelId, string channelPassword)
        {
            return MoveClient(new[] { clientId }, targetChannelId, channelPassword);
        }

        public Task MoveClient(IEnumerable<GetClientsInfo> clients, int targetChannelId)
        {
            var clIds = clients.Select(c => c.ClientId).ToArray();
            return MoveClient(clIds, targetChannelId);
        }
        public Task MoveClient(IEnumerable<GetClientsInfo> clients, int targetChannelId, string channelPassword)
        {
            var clIds = clients.Select(c => c.ClientId).ToArray();
            return MoveClient(clIds, targetChannelId, channelPassword);
        }

        public Task MoveClient(IList<int> clientIds, int targetChannelId)
        {
            return _client.Send("clientmove",
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i)).ToArray()),
                new Parameter("cid", targetChannelId));
        }
        public Task MoveClient(IList<int> clientIds, int targetChannelId, string channelPassword)
        {
            return _client.Send("clientmove",
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i)).ToArray()),
                new Parameter("cid", targetChannelId),
                new Parameter("cpw", channelPassword));
        }

        #endregion
        #region KickClient

        public Task KickClient(int clientId, KickOrigin from)
        {
            return KickClient(new[] { clientId }, from);
        }
        public Task KickClient(int clientId, KickOrigin from, string reasonMessage)
        {
            return KickClient(new[] { clientId }, from, reasonMessage);
        }
        public Task KickClient(GetClientsInfo client, KickOrigin from)
        {
            return KickClient(client.ClientId, from);
        }
        public Task KickClient(IEnumerable<GetClientsInfo> clients, KickOrigin from)
        {
            var clIds = clients.Select(c => c.ClientId).ToArray();
            return KickClient(clIds, from);
        }
        public Task KickClient(IList<int> clientIds, KickOrigin from)
        {
            return _client.Send("clientkick",
                new Parameter("reasonid", (int)from),
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i.ToString(CultureInfo.InvariantCulture))).ToArray()));
        }
        public Task KickClient(IList<int> clientIds, KickOrigin from, string reasonMessage)
        {
            return _client.Send("clientkick",
                new Parameter("reasonid", (int)from),
                new Parameter("reasonmsg", reasonMessage),
                new Parameter("clid", clientIds.Select(i => new ParameterValue(i.ToString(CultureInfo.InvariantCulture))).ToArray()));
        }

        #endregion
        #region BanClient

        public Task<IReadOnlyList<ClientBan>> BanClient(GetClientsInfo client)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            return BanClient(client.ClientId);
        }
        public Task<IReadOnlyList<ClientBan>> BanClient(GetClientsInfo client, TimeSpan duration)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            return BanClient(client.ClientId, duration);
        }
        public Task<IReadOnlyList<ClientBan>> BanClient(GetClientsInfo client, TimeSpan duration, string reason)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            return BanClient(client.ClientId, duration, reason);
        }

        public async Task<IReadOnlyList<ClientBan>> BanClient(int clientId)
        {
            var res = await _client.Send("banclient",
                new Parameter("clid", clientId));
            return DataProxy.SerializeGeneric<ClientBan>(res);
        }
        public async Task<IReadOnlyList<ClientBan>> BanClient(int clientId, TimeSpan duration)
        {
            var res = await _client.Send("banclient",
                new Parameter("clid", clientId),
                new Parameter("time", (int)Math.Ceiling(duration.TotalSeconds)));
            return DataProxy.SerializeGeneric<ClientBan>(res);
        }
        public async Task<IReadOnlyList<ClientBan>> BanClient(int clientId, TimeSpan duration, string reason)
        {
            var res = await _client.Send("banclient",
                new Parameter("clid", clientId),
                new Parameter("time", (int)Math.Ceiling(duration.TotalSeconds)),
                new Parameter("banreason", reason ?? ""));
            return DataProxy.SerializeGeneric<ClientBan>(res);
        }

        #endregion
        #region GetClients

        public async Task<IReadOnlyList<GetClientsInfo>> GetClients()
        {
            var res = await _client.Send("clientlist");
            return DataProxy.SerializeGeneric<GetClientsInfo>(res);
        }

        public async Task<IReadOnlyList<GetClientsInfo>> GetClients(GetClientOptions options)
        {
            var optionList = options.GetFlagsAsList();
            var res = await _client.Send("clientlist", null, optionList.ToArray());
            return DataProxy.SerializeGeneric<GetClientsInfo>(res);
        }

        #endregion

        #endregion

        #region Channel Methods

        #region GetChannels

        public async Task<IReadOnlyList<GetChannelListInfo>> GetChannels()
        {
            var res = await _client.Send("channellist");
            return DataProxy.SerializeGeneric<GetChannelListInfo>(res);
        }

        public async Task<IReadOnlyList<GetChannelListInfo>> GetChannels(GetChannelOptions options)
        {
            var optionList = options.GetFlagsAsList();
            var res = await _client.Send("channellist", null, optionList.ToArray());
            return DataProxy.SerializeGeneric<GetChannelListInfo>(res);
        }

        #endregion
        #region GetChannelInfo

        public Task<GetChannelInfo> GetChannelInfo(GetChannelListInfo channel)
        {
            if (channel == null)
                throw new ArgumentNullException("channel");
            return GetChannelInfo(channel.Id);
        }

        public async Task<GetChannelInfo> GetChannelInfo(int channelId)
        {
            var res = await _client.Send("channelinfo",
                new Parameter("cid", channelId));
            return DataProxy.SerializeGeneric<GetChannelInfo>(res).FirstOrDefault();
        }

        #endregion
        #region FindChannel

        public async Task<IReadOnlyCollection<FoundChannel>> FindChannel()
        {
            var res = await _client.Send("channelfind");
            return DataProxy.SerializeGeneric<FoundChannel>(res);
        }
        public async Task<IReadOnlyCollection<FoundChannel>> FindChannel(string pattern)
        {
            var res = await _client.Send("channelfind",
                new Parameter("pattern", pattern ?? ""));
            return DataProxy.SerializeGeneric<FoundChannel>(res);
        }

        #endregion
        #region MoveChannel

        public Task MoveChannel(GetChannelListInfo channel, GetChannelListInfo parent)
        {
            if (channel == null)
                throw new ArgumentNullException("channel");
            if (parent == null)
                throw new ArgumentNullException("parent");
            return MoveChannel(channel.Id, parent.Id);
        }
        public Task MoveChannel(GetChannelListInfo channel, GetChannelListInfo parent, int order)
        {
            if (channel == null)
                throw new ArgumentNullException("channel");
            if (parent == null)
                throw new ArgumentNullException("parent");
            return MoveChannel(channel.Id, parent.Id, order);
        }

        public Task MoveChannel(int channelId, int parentChannelId)
        {
            return _client.Send("channelmove",
                new Parameter("cid", channelId),
                new Parameter("cpid", parentChannelId));
        }
        public Task MoveChannel(int channelId, int parentChannelId, int order)
        {
            return _client.Send("channelmove",
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
                throw new ArgumentNullException("name");

            var res = await _client.Send("channelcreate",
                new Parameter("channel_name", name));
            return DataProxy.SerializeGeneric<CreatedChannel>(res).FirstOrDefault();
        }

        #endregion
        #region DeleteChannel

        public Task DeleteChannel(GetChannelListInfo channel)
        {
            if (channel == null)
                throw new ArgumentNullException("channel");
            return DeleteChannel(channel.Id);
        }
        public Task DeleteChannel(GetChannelListInfo channel, bool force)
        {
            if (channel == null)
                throw new ArgumentNullException("channel");
            return DeleteChannel(channel.Id, force);
        }

        public Task DeleteChannel(int channelId)
        {
            return _client.Send("channeldelete",
                new Parameter("cid", channelId));
        }
        public Task DeleteChannel(int channelId, bool force)
        {
            return _client.Send("channeldelete",
                new Parameter("cid", channelId),
                new Parameter("force", force));
        }

        #endregion

        #endregion

        #region Server Methods

        #region GetServers

        public async Task<IReadOnlyList<GetServerListInfo>> GetServers()
        {
            var res = await _client.Send("serverlist");
            return DataProxy.SerializeGeneric<GetServerListInfo>(res);
        }

        public async Task<IReadOnlyList<GetServerListInfo>> GetServers(GetServerOptions options)
        {
            var optionList = options.GetFlagsAsList();
            var res = await _client.Send("serverlist", null, optionList.ToArray());
            return DataProxy.SerializeGeneric<GetServerListInfo>(res);
        }

        #endregion

        #endregion

        #region Message Methods

        #region SendTextMessage

        public Task SendMessage(string message, GetServerListInfo targetServer)
        {
            if (targetServer == null)
                throw new ArgumentNullException("targetServer");
            return SendMessage(message, MessageTarget.Server, targetServer.Id);
        }
        public Task SendMessage(string message, GetChannelListInfo targetChannel)
        {
            if (targetChannel == null)
                throw new ArgumentNullException("targetChannel");
            return SendMessage(message, MessageTarget.Channel, targetChannel.Id);
        }
        public Task SendMessage(string message, GetClientsInfo targetClient)
        {
            if (targetClient == null)
                throw new ArgumentNullException("targetClient");
            return SendMessage(message, MessageTarget.Private, targetClient.ClientId);
        }
        public Task SendMessage(string message, MessageTarget target, int targetId)
        {
            message = message ?? "";
            return _client.Send("sendtextmessage",
                new Parameter("targetmode", (int)target),
                new Parameter("target", targetId),
                new Parameter("msg", message));
        }

        #endregion
        #region SendGlobalMessage

        public Task SendGlobalMessage(string message)
        {
            return _client.Send("gm",
                new Parameter("msg", message ?? ""));
        }

        #endregion

        #endregion

        #endregion
    }
}
