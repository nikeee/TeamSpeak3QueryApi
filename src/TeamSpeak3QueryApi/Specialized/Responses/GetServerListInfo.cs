using System;

namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class GetServerListInfo : Response
{
    [QuerySerialize("virtualserver_id")]
    public int Id;

    [QuerySerialize("virtualserver_port")]
    public int Port;

    [QuerySerialize("virtualserver_status")]
    public string Status;

    [QuerySerialize("virtualserver_clientsonline")]
    public int ClientsOnline;

    [QuerySerialize("virtualserver_queryclientsonline")]
    public int QueriesOnline;

    [QuerySerialize("virtualserver_maxclients")]
    public int MaxClients;

    [QuerySerialize("virtualserver_uptime")]
    public TimeSpan Uptime;

    [QuerySerialize("virtualserver_name")]
    public string Name;

    [QuerySerialize("virtualserver_autostart")]
    public bool Autostart;

    [QuerySerialize("virtualserver_machine_id")]
    public string MachineId;

    [QuerySerialize("virtualserver_unique_identifier")]
    public string Uid;
}
