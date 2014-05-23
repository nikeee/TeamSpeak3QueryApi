using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    public class TeamSpeakClient
    {
        private readonly QueryClient _client;
        public QueryClient Client { get { return _client; } }

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
}
