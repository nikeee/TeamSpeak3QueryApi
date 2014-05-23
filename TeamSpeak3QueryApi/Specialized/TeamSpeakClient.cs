using System;
using System.Collections.Generic;
using System.Linq;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;

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
}
