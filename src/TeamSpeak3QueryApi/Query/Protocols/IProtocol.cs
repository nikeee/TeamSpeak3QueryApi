using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeamSpeak3QueryApi.Net.Query.Protocols
{
    public interface IProtocol
    {
        Task<CancellationTokenSource> ConnectAsync();
        CancellationTokenSource Connect(string username, string password);
    }
}
