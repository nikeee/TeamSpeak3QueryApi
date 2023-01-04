namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class WhoAmI : Response
{
    [QuerySerialize("virtualserver_status")]
    public string VirtualServerStatus; // Status of the virtual server (online | virtual online | offline | booting up | shutting down| ...

    [QuerySerialize("virtualserver_id")]
    public int VirtualServerId;

    [QuerySerialize("virtualserver_unique_identifier")]
    public string VirtualServerUid;

    [QuerySerialize("virtualserver_port")]
    public short VirtualServerPort;

    [QuerySerialize("client_id")]
    public int ClientId;

    [QuerySerialize("client_channel_id")]
    public int ChannelId;

    [QuerySerialize("client_nickname")]
    public string NickName;

    [QuerySerialize("client_database_id")]
    public int DatabaseId;

    [QuerySerialize("client_login_name")]
    public string LoginName;

    [QuerySerialize("client_unique_identifier")]
    public string Uid;

    [QuerySerialize("client_origin_server_id")]
    public int OriginServerId;
}
