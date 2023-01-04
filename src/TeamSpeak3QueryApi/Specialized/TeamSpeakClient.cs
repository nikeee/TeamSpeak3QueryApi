using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamSpeak3QueryApi.Net.Specialized.Responses;

namespace TeamSpeak3QueryApi.Net.Specialized;

public class TeamSpeakClient : IDisposable
{
    public QueryClient Client { get; }

    private readonly Dictionary<NotificationType, Dictionary<object, Action<NotificationData>>> _callbackMap = new();

    private readonly FileTransferClient _fileTransferClient;
    private readonly CancellationTokenSource _keepAliveCancellationTokenSource = new CancellationTokenSource();
    private static readonly TimeSpan _maxClientIdleTime = TimeSpan.FromMinutes(5);

    //We're allowing it to be null just incase the user won't spefify a time.
    public TimeSpan? KeepAliveInterval { get; }

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
    /// <param name="keepAliveInterval">The TimeSpan used to use on the internal keep alive wait. Pass null to disable KeepAlive</param>
    public TeamSpeakClient(string hostName, int port, TimeSpan? keepAliveInterval = null)
    {
        KeepAliveInterval = keepAliveInterval;
        Client = new QueryClient(hostName, port);
        _fileTransferClient = new FileTransferClient(hostName);
    }

    #endregion

    public Task Connect()
    {
        if (KeepAliveInterval.HasValue)
            _ = KeepAliveLoop(); // Keep the KeepAliveLoop in the background

        return Client.Connect();
    }

    private async Task<Task> KeepAliveLoop()
    {
        var interval = KeepAliveInterval;
        if (interval == null)
            return Task.CompletedTask;

        while (Client.IsConnected)
        {
            var currentIdleTime = TimeSpan.FromMilliseconds(Client.Idle.ElapsedMilliseconds);

            // If we're idle for more then 5 minutes we should send a whoami
            if (currentIdleTime > _maxClientIdleTime)
            {
                await WhoAmI();
            }
            else
            {
                try
                {
                    await Task.Delay(interval.Value, _keepAliveCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    return Task.CompletedTask;
                }
            }
        }

        return Task.CompletedTask;
    }

    #region Subscriptions

    public void Subscribe<T>(Action<IReadOnlyCollection<T>> callback)
        where T : Notification
    {
        var notification = GetNotificationType<T>();

        void dataProxiedCallback(NotificationData data) => callback(DataProxy.SerializeGeneric<T>(data.Payload));

        var callbacksOfNotification = _callbackMap.GetOrAddNew(notification, new Dictionary<object, Action<NotificationData>>());

        callbacksOfNotification[callback] = dataProxiedCallback;

        Client.Subscribe(notification.ToString(), dataProxiedCallback);
    }


    /// <returns>true if the element is successfully found and removed; otherwise, false. </returns>
    public bool Unsubscribe<T>()
        where T : Notification
    {
        var notification = GetNotificationType<T>();

        var res = _callbackMap.Remove(notification);

        Client.Unsubscribe(notification.ToString());

        return res;
    }

    /// <returns>true if the element is successfully found and removed; otherwise, false. </returns>
    public bool Unsubscribe<T>(Action<IReadOnlyCollection<T>> callback)
        where T : Notification
    {
        var notification = GetNotificationType<T>();

        if (!_callbackMap.TryGetValue(notification, out var callbacks))
            return false;

        if (!callbacks.TryGetValue(callback, out var proxiedCallback))
            return false;

        var res = callbacks.Remove(callback);
        System.Diagnostics.Debug.Assert(res);

        Client.Unsubscribe(notification.ToString(), proxiedCallback);

        return res;
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

    public Task Logout()
    {
        _keepAliveCancellationTokenSource.Cancel();
        return Client.Send("logout");
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

    public Task MoveClient(IEnumerable<GetClientInfo> clients, int targetChannelId)
    {
        var clIds = clients.Select(c => c.Id).ToArray();
        return MoveClient(clIds, targetChannelId);
    }
    public Task MoveClient(IEnumerable<GetClientInfo> clients, int targetChannelId, string channelPassword)
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
    public Task KickClient(GetClientInfo client, KickOrigin from) => KickClient(client.Id, from);
    public Task KickClient(IEnumerable<GetClientInfo> clients, KickOrigin from)
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

    public Task<IReadOnlyList<ClientBan>> BanClient(GetClientInfo client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        return BanClient(client.Id);
    }
    public Task<IReadOnlyList<ClientBan>> BanClient(GetClientInfo client, TimeSpan duration)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        return BanClient(client.Id, duration);
    }
    public Task<IReadOnlyList<ClientBan>> BanClient(GetClientInfo client, TimeSpan duration, string reason)
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

    public async Task<IReadOnlyList<GetClientInfo>> GetClients()
    {
        var res = await Client.Send("clientlist").ConfigureAwait(false);
        return DataProxy.SerializeGeneric<GetClientInfo>(res);
    }

    public async Task<IReadOnlyList<GetClientInfo>> GetClients(GetClientOptions options)
    {
        var optionList = options.GetFlagsName();
        var res = await Client.Send("clientlist", null, optionList.ToArray()).ConfigureAwait(false);
        return DataProxy.SerializeGeneric<GetClientInfo>(res);
    }

    public Task<GetClientDetailedInfo> GetClientInfo(GetClientInfo client) => GetClientInfo(client.Id);

    public async Task<GetClientDetailedInfo> GetClientInfo(int clientId)
    {
        var res = await Client.Send("clientinfo",
            new Parameter("clid", clientId))
            .ConfigureAwait(false);

        return DataProxy.SerializeGeneric<GetClientDetailedInfo>(res).FirstOrDefault();
    }

    public async Task<GetClientIds> GetClientIds(string cluid)
    {
        var res = await Client.Send("clientgetids",
            new Parameter("cluid", cluid))
            .ConfigureAwait(false);
        return DataProxy.SerializeGeneric<GetClientIds>(res).FirstOrDefault();
    }

    public async Task<DatabaseIdFromClientUid> DatabaseIdFromClientUid(string cluid)
    {
        var res = await Client.Send("clientgetdbidfromuid",
            new Parameter("cluid", cluid))
            .ConfigureAwait(false);
        return DataProxy.SerializeGeneric<DatabaseIdFromClientUid>(res).FirstOrDefault();
    }

    public async Task<NameFromDatabaseId> NameFromDatabaseId(int cldbid)
    {
        var res = await Client.Send("clientgetnamefromdbid",
            new Parameter("cldbid", cldbid))
            .ConfigureAwait(false);
        return DataProxy.SerializeGeneric<NameFromDatabaseId>(res).FirstOrDefault();
    }

    public async Task<NameFromClientUid> NameFromClientUid(string cluid)
    {
        var res = await Client.Send("clientgetnamefromuid",
            new Parameter("cluid", cluid))
            .ConfigureAwait(false);
        return DataProxy.SerializeGeneric<NameFromClientUid>(res).FirstOrDefault();
    }

    public async Task<ClientUidFromClientId> ClientUidFromClientId(int clid)
    {
        var res = await Client.Send("clientgetuidfromclid",
            new Parameter("clid", clid))
            .ConfigureAwait(false);
        return DataProxy.SerializeGeneric<ClientUidFromClientId>(res).FirstOrDefault();
    }

    #endregion

    #region GetServerGroups

    public async Task<IReadOnlyList<GetServerGroup>> GetServerGroups(int clientDatabaseId)
    {
        var res = await Client.Send("servergroupsbyclientid", new Parameter("cldbid", clientDatabaseId)).ConfigureAwait(false);
        return DataProxy.SerializeGeneric<GetServerGroup>(res);
    }

    public Task<IReadOnlyList<GetServerGroup>> GetServerGroups(GetClientInfo clientInfo) => GetServerGroups(clientInfo.DatabaseId);

    public Task<IReadOnlyList<GetServerGroup>> GetServerGroups(WhoAmI clientInfo) => GetServerGroups(clientInfo.DatabaseId);

    #endregion

    #region AddServerGroup

    #region One User

    public Task AddServerGroup(int serverGroupId, int clientDatabaseId) => AddServerGroup(serverGroupId, new int[] { clientDatabaseId });

    public Task AddServerGroup(int serverGroupId, GetClientInfo clientInfo) => AddServerGroup(serverGroupId, clientInfo.DatabaseId);

    public Task AddServerGroup(GetServerGroup serverGroup, int clientDatabaseId) => AddServerGroup(serverGroup.Id, clientDatabaseId);

    public Task AddServerGroup(GetServerGroup serverGroup, GetClientInfo clientInfo) => AddServerGroup(serverGroup.Id, clientInfo.DatabaseId);

    #endregion

    #region Multiple Users

    public Task AddServerGroup(int serverGroupId, IEnumerable<GetClientInfo> clientInfo) => AddServerGroup(serverGroupId, clientInfo.Select(info => info.DatabaseId));

    public Task AddServerGroup(GetServerGroup serverGroup, IEnumerable<int> clientDatabaseIds) => AddServerGroup(serverGroup.Id, clientDatabaseIds);

    public Task AddServerGroup(GetServerGroup serverGroup, IEnumerable<GetClientInfo> clientInfo) => AddServerGroup(serverGroup.Id, clientInfo.Select(info => info.DatabaseId));

    public Task AddServerGroup(int serverGroupId, IEnumerable<int> clientDatabaseIds)
    {
        return Client.Send("servergroupaddclient",
            new Parameter("sgid", serverGroupId),
            new Parameter("cldbid", clientDatabaseIds.Select(id => new ParameterValue(id)).ToArray()));
    }

    #endregion

    #endregion

    #region RemoveServerGroup

    #region One User

    public Task RemoveServerGroup(int serverGroupId, int clientDatabaseId) => RemoveServerGroup(serverGroupId, new int[] { clientDatabaseId });

    public Task RemoveServerGroup(int serverGroupId, GetClientInfo clientInfo) => RemoveServerGroup(serverGroupId, clientInfo.DatabaseId);

    public Task RemoveServerGroup(GetServerGroup serverGroup, int clientDatabaseId) => RemoveServerGroup(serverGroup.Id, clientDatabaseId);

    public Task RemoveServerGroup(GetServerGroup serverGroup, GetClientInfo clientInfo) => RemoveServerGroup(serverGroup.Id, clientInfo.DatabaseId);

    #endregion

    #region Multiple Users

    public Task RemoveServerGroup(int serverGroupId, IEnumerable<GetClientInfo> clientInfo) => RemoveServerGroup(serverGroupId, clientInfo.Select(info => info.DatabaseId));

    public Task RemoveServerGroup(GetServerGroup serverGroup, IEnumerable<int> clientDatabaseIds) => RemoveServerGroup(serverGroup.Id, clientDatabaseIds);

    public Task RemoveServerGroup(GetServerGroup serverGroup, IEnumerable<GetClientInfo> clientInfo) => RemoveServerGroup(serverGroup.Id, clientInfo.Select(info => info.DatabaseId));

    public Task RemoveServerGroup(int serverGroupId, IEnumerable<int> clientDatabaseIds)
    {
        return Client.Send("servergroupdelclient",
            new Parameter("sgid", serverGroupId),
            new Parameter("cldbid", clientDatabaseIds.Select(id => new ParameterValue(id)).ToArray()));
    }

    #endregion

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

    public async Task<IReadOnlyCollection<EditChannelInfo>> EditChannel(int channelid, ChannelEdit editChannel, string value)
    {
        var res = await Client.Send("channeledit",
            new Parameter("cid", channelid),
            new Parameter(editChannel.ToString(), value))
            .ConfigureAwait(false);
        return DataProxy.SerializeGeneric<EditChannelInfo>(res);
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

    public async Task<IReadOnlyList<GetServerGroupListInfo>> GetServerGroups()
    {
        var res = await Client.Send("servergrouplist").ConfigureAwait(false);
        return DataProxy.SerializeGeneric<GetServerGroupListInfo>(res);
    }

    public async Task<IReadOnlyList<GetServerGroupClientList>> GetServerGroupClientList(int serverGroupDatabaseId)
    {
        var res = await Client.Send("servergroupclientlist", new Parameter("sgid", serverGroupDatabaseId)).ConfigureAwait(false);
        return DataProxy.SerializeGeneric<GetServerGroupClientList>(res);
    }

    #endregion
    #region ServerEdit
    public async Task<IReadOnlyCollection<ServerEditResponse>> ServerEdit(ServerEdit serverEdit, string value)
    {
        var res = await Client.Send("serveredit",
            new Parameter(serverEdit.ToString(), value))
            .ConfigureAwait(false);
        return DataProxy.SerializeGeneric<ServerEditResponse>(res);
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
    public Task SendMessage(string message, GetClientInfo targetClient)
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

    public Task PokeClient(GetClientInfo client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        return PokeClient(client.Id);
    }
    public Task PokeClient(int clientId)
    {
        return PokeClient(clientId, string.Empty);
    }

    public Task PokeClient(GetClientInfo client, string message)
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

    #region Filetransfer Methods

    #region CreateDirectory

    public Task CreateDirectory(int channelId, string dirPath) => CreateDirectory(channelId, string.Empty, dirPath);

    public Task CreateDirectory(int channelId, string channelPassword, string dirPath)
    {
        return Client.Send("ftcreatedir",
            new Parameter("cid", channelId),
            new Parameter("cpw", channelPassword),
            new Parameter("dirname", NormalizePath(dirPath)));
    }

    #endregion

    #region DeleteFile

    public Task DeleteFile(int channelId, string filePath) => DeleteFile(channelId, string.Empty, new string[] { filePath });

    public Task DeleteFile(int channelId, string channelPassword, string filePath) => DeleteFile(channelId, channelPassword, new string[] { filePath });

    public Task DeleteFile(int channelId, IEnumerable<string> filePaths) => DeleteFile(channelId, string.Empty, filePaths);

    public Task DeleteFile(int channelId, string channelPassword, IEnumerable<string> filePaths)
    {
        return Client.Send("ftdeletefile",
            new Parameter("cid", channelId),
            new Parameter("cpw", channelPassword),
            new Parameter("name", filePaths.Select(path => new ParameterValue(NormalizePath(path))).ToArray()));
    }

    #endregion

    #region GetFileInfo

    public Task<GetFileInfo> GetFileInfo(int channelId, string filePath) => GetFileInfo(channelId, string.Empty, filePath);

    public async Task<GetFileInfo> GetFileInfo(int channelId, string channelPassword, string filePath)
    {
        var res = await Client.Send("ftgetfileinfo",
            new Parameter("cid", channelId),
            new Parameter("cpw", channelPassword),
            new Parameter("name", NormalizePath(filePath))).ConfigureAwait(false);

        return DataProxy.SerializeGeneric<GetFileInfo>(res).FirstOrDefault();
    }

    #endregion

    #region GetFileList

    public Task<IReadOnlyList<GetFiles>> GetFiles(int channelId) => GetFiles(channelId, string.Empty, "/");

    public Task<IReadOnlyList<GetFiles>> GetFiles(int channelId, string dirPath) => GetFiles(channelId, string.Empty, dirPath);

    public async Task<IReadOnlyList<GetFiles>> GetFiles(int channelId, string channelPassword, string dirPath)
    {
        var res = await Client.Send("ftgetfilelist",
            new Parameter("cid", channelId),
            new Parameter("cpw", channelPassword),
            new Parameter("path", NormalizePath(dirPath))).ConfigureAwait(false);

        return DataProxy.SerializeGeneric<GetFiles>(res);
    }

    #endregion

    #region MoveFile

    #region Same Channel

    public Task MoveFile(int channelId, string oldFilePath, string newFilePath) => MoveFile(channelId, string.Empty, oldFilePath, newFilePath);

    public Task MoveFile(int channelId, string channelPassword, string oldFilePath, string newFilePath)
    {
        return Client.Send("ftrenamefile",
            new Parameter("cid", channelId),
            new Parameter("cpw", channelPassword),
            new Parameter("oldname", NormalizePath(oldFilePath)),
            new Parameter("newname", NormalizePath(newFilePath)));
    }

    #endregion

    #region Other Channel

    public Task MoveFile(int channelId, string oldFilePath, int targetChannelId, string newFilePath) => MoveFile(channelId, string.Empty, oldFilePath, targetChannelId, string.Empty, newFilePath);

    public Task MoveFile(int channelId, string channelPassword, string oldFilePath, int targetChannelId, string newFilePath) => MoveFile(channelId, channelPassword, oldFilePath, targetChannelId, string.Empty, newFilePath);

    public Task MoveFile(int channelId, string oldFilePath, int targetChannelId, string targetChannelPassword, string newFilePath) => MoveFile(channelId, string.Empty, oldFilePath, targetChannelId, targetChannelPassword, newFilePath);

    public Task MoveFile(int channelId, string channelPassword, string oldFilePath, int targetChannelId, string targetChannelPassword, string newFilePath)
    {
        return Client.Send("ftrenamefile",
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

    public Task UploadFile(int channelId, string filePath, byte[] data, bool overwrite = true, bool verify = true) => UploadFile(channelId, string.Empty, filePath, data, overwrite, verify);

    public async Task UploadFile(int channelId, string channelPassword, string filePath, byte[] data, bool overwrite = true, bool verify = true)
    {
        var res = await Client.Send("ftinitupload",
            new Parameter("clientftfid", _fileTransferClient.GetFileTransferId()),
            new Parameter("cid", channelId),
            new Parameter("cpw", channelPassword),
            new Parameter("name", NormalizePath(filePath)),
            new Parameter("size", data.Length),
            new Parameter("overwrite", overwrite),
            new Parameter("resume", 0)).ConfigureAwait(false);

        var parsedRes = DataProxy.SerializeGeneric<InitUpload>(res).First();

        await _fileTransferClient.SendFile(data, parsedRes.Port, parsedRes.FileTransferKey).ConfigureAwait(false);

        if (verify)
        {
            await VerifyUpload(parsedRes.ServerFileTransferId).ConfigureAwait(false);
        }
    }

    public Task UploadFile(int channelId, string filePath, Stream dataStream, long size, bool overwrite = true, bool verify = true) => UploadFile(channelId, string.Empty, filePath, dataStream, size, overwrite, verify);

    public async Task UploadFile(int channelId, string channelPassword, string filePath, Stream dataStream, long size, bool overwrite = true, bool verify = true)
    {
        var res = await Client.Send("ftinitupload",
            new Parameter("clientftfid", _fileTransferClient.GetFileTransferId()),
            new Parameter("cid", channelId),
            new Parameter("cpw", channelPassword),
            new Parameter("name", NormalizePath(filePath)),
            new Parameter("size", size),
            new Parameter("overwrite", overwrite),
            new Parameter("resume", 0)).ConfigureAwait(false);

        var parsedRes = DataProxy.SerializeGeneric<InitUpload>(res).First();

        await _fileTransferClient.SendFile(dataStream, parsedRes.Port, parsedRes.FileTransferKey).ConfigureAwait(false);

        if (verify)
        {
            await VerifyUpload(parsedRes.ServerFileTransferId).ConfigureAwait(false);
        }
    }

    /// <summary>Waits until the server fully receives the file or throws an exception when the upload times out.</summary>
    private async Task VerifyUpload(int serverFileTransferId)
    {
        long arrivedBytes = 0;
        var intervalMillis = 100;
        var timeoutMillis = 3000;
        var currentTimeoutMillis = intervalMillis * -1;

        while (true)
        {
            var transfers = await GetCurrentFileTransfers();
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
                        await StopFileTransfer(serverFileTransferId).ConfigureAwait(false);
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

    public Task<Stream> DownloadFile(int channelId, string filePath) => DownloadFile(channelId, string.Empty, filePath);

    public async Task<Stream> DownloadFile(int channelId, string channelPassword, string filePath)
    {
        var res = await Client.Send("ftinitdownload",
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

        return await _fileTransferClient.ReceiveFile((int)parsedRes.Size, parsedRes.Port, parsedRes.FileTransferKey).ConfigureAwait(false);
    }

    #endregion

    #region GetCurrentFileTransfers

    public async Task<IReadOnlyList<GetCurrentFileTransfer>> GetCurrentFileTransfers()
    {
        try
        {
            var res = await Client.Send("ftlist").ConfigureAwait(false);

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

    private Task StopFileTransfer(int serverFileTransferId, bool delete = true)
    {
        return Client.Send("ftstop",
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
            _keepAliveCancellationTokenSource?.Cancel();
            Client?.Dispose();
        }
    }

    #endregion
}
