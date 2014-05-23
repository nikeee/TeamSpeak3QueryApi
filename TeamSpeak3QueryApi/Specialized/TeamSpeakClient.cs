using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    public class TeamSpeakClient
    {
        private QueryClient _client;

        public TeamSpeakClient(QueryClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            _client = client;
            if (!_client.IsConnected)
                _client.Connect();
        }

        public void Subscribe(NotificationType notification, Action<NotificationData> callback)
        {
            _client.Subscribe(notification.ToString(), callback);
        }
        public void Unsubscripe(NotificationType notification)
        {
            _client.Unsubscribe(notification.ToString());
        }
        public void Unsubscribe(NotificationType notification, Action<NotificationData> callback)
        {
            _client.Unsubscribe(notification.ToString(), callback);
        }
    }

    // http://redeemer.biz/medien/artikel/ts3-query-notify/
    public enum NotificationType
    {
        // Server/Client notifications
        ClientEnterView,
        ClientLeftView,

        // Server notifications
        ServerEdited,

        // Client notifications
        ChannelDescriptionChanged,
        ChannelPasswordChanged,
        ChannelMoved,
        ChannelEdited,
        ChannelCreated,
        ChannelDeleted,
        ClientMoved,
        TextMessage,
        TokenUsed
    }
}
