namespace TeamSpeak3QueryApi.Net.Specialized.Notifications;

public class ChannelPasswordChanged : Notification
{
    [QuerySerialize("cid")]
    public int ChannelId;
}
