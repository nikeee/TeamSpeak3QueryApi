using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Enums;
using TeamSpeak3QueryApi.Net.Extensions;

namespace TeamSpeak3QueryApi.Net.Query.Protocols
{
    public class TelnetProtocol : QueryClient
    {
        /// <summary>The default port which is used when no port is provided.</summary>
        public const short DefaultPort = 10011;

        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the <see cref="QueryClient.DefaultHost"/> and <see cref="QueryClient.DefaultPort"/>.</summary>
        public TelnetProtocol()
            : this(DefaultHost, DefaultPort)
        { }

        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the provided host and the <see cref="QueryClient.DefaultPort"/>.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        public TelnetProtocol(string hostName)
            : this(hostName, DefaultPort)
        { }
        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the provided host TCP port.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        /// <param name="port">The TCP port of the Query API server.</param>
        public TelnetProtocol(string hostName, int port)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                throw new ArgumentNullException(nameof(hostName));
            if (!ValidationHelper.ValidateTcpPort(port))
                throw new ArgumentOutOfRangeException(nameof(port));

            Host = hostName;
            Port = port;
            ConnectionType = Protocol.Telnet;
            IsConnected = false;
            Client = new TcpClient();
        }

        /// <summary>Connects to the Query API server.</summary>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public override CancellationTokenSource Connect(string username, string password)
        {
            throw new InvalidOperationException("Connect Method is not supported in telnet query. Please use the ssh query.");
        }

        /// <summary>Connects to the Query API server.</summary>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public override async Task<CancellationTokenSource> ConnectAsync()
        {
            if (ConnectionType != Protocol.Telnet)
                throw new InvalidOperationException("ConnectAsync Method without parameters can only be used with telnet Query. Please use Connect method.");

            await Client.ConnectAsync(Host, Port).ConfigureAwait(false);
            if (!Client.Connected)
                throw new InvalidOperationException("Could not connect.");

            _ns = Client.GetStream();
            _reader = new StreamReader(_ns);
            _writer = new StreamWriter(_ns) { NewLine = "\n" };

            IsConnected = true;
            OnConnected?.Invoke(this, EventArgs.Empty);


            var headline = await _reader.ReadLineAsync().ConfigureAwait(false);
            if (headline != "TS3")
            {
                throw new QueryProtocolException("Telnet Query isn't a valid Teamspeak Query");
            }
            await _reader.ReadLineAsync().ConfigureAwait(false); // Ignore welcome message
            await _reader.ReadLineAsync().ConfigureAwait(false);

            return ResponseProcessingLoop();
        }

        private CancellationTokenSource ResponseProcessingLoop()
        {
            var cts = _cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    string line = null;
                    try
                    {
                        line = await _reader.ReadLineAsync().WithCancellation(cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (line == null)
                    {
                        cts.Cancel();
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(line) || string.IsNullOrEmpty(line))
                        continue;

                    var s = line.Trim();
                    Debug.WriteLine(line);

                    if (s.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Assert(_currentCommand != null);

                        var error = ParseError(s);
                        _currentCommand.Error = error;
                        InvokeResponse(_currentCommand);
                    }
                    else if (s.StartsWith("notify", StringComparison.OrdinalIgnoreCase))
                    {
                        s = s.Remove(0, "notify".Length);
                        var not = ParseNotification(s);
                        InvokeNotification(not);
                    }
                    else
                    {
                        Debug.Assert(_currentCommand != null);
                        _currentCommand.RawResponse = s;
                        _currentCommand.ResponseDictionary = ParseResponse(s);
                    }
                }

                IsConnected = false;
                OnConnectionLost?.Invoke(this, EventArgs.Empty);
                OnDisconnected?.Invoke(this, EventArgs.Empty);

            });
            return cts;
        }
    }
}
