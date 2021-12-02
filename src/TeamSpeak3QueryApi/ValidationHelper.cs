namespace TeamSpeak3QueryApi.Net;

internal class ValidationHelper
{
    /// <summary>
    /// on false, API should throw new ArgumentOutOfRangeException("port");
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public static bool ValidateTcpPort(int port) => port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort;
}
