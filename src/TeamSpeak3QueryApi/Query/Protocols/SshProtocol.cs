using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
using TeamSpeak3QueryApi.Net.Enums;
using TeamSpeak3QueryApi.Net.Extensions;

namespace TeamSpeak3QueryApi.Net.Query.Protocols
{
    public class SshProtocol : QueryClient
    {
        /// <summary>The default port which is used when no port is provided.</summary>
        public const short DefaultPort = 10022;

        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the <see cref="QueryClient.DefaultHost"/> and <see cref="QueryClient.DefaultPort"/>.</summary>
        public SshProtocol()
            : this(DefaultHost, DefaultPort)
        { }

        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the provided host and the <see cref="QueryClient.DefaultPort"/>.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        public SshProtocol(string hostName)
            : this(hostName, DefaultPort)
        { }
        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the provided host TCP port.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        /// <param name="port">The TCP port of the Query API server.</param>
        public SshProtocol(string hostName, int port)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                throw new ArgumentNullException(nameof(hostName));
            if (!ValidationHelper.ValidateTcpPort(port))
                throw new ArgumentOutOfRangeException(nameof(port));

            Host = hostName;
            Port = port;
            ConnectionType = Protocol.SSH;
            IsConnected = false;
            Client = new TcpClient();
        }

        /// <summary>Connects to the Query API server.</summary>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public override CancellationTokenSource Connect(string username, string password)
        {
            this.username = username;

            _sshClient = new SshClient(Host, Port, username, password);
            _sshClient.Connect();

            var terminalMode = new Dictionary<TerminalModes, uint>();
            terminalMode.Add(TerminalModes.ECHO, 53);

            _shell = _sshClient.CreateShellStream("", 0, 0, 0, 0, 4096);

            _reader = new StreamReader(_shell, Encoding.UTF8, true, 1024, true);
            _writer = new StreamWriter(_shell) { NewLine = "\n", AutoFlush = true };

            var headline = _shell.Expect("\r\n", new TimeSpan(0, 0, 3));
            if (!headline.Contains("TS3"))
            {
                throw new QueryProtocolException("Telnet Query isn't a valid Teamspeak Query");
            }
            _shell.Expect("\n", new TimeSpan(0, 0, 3)); // Ignore welcome message
            _shell.Expect("\n", new TimeSpan(0, 0, 3)); // Ignore welcome message

            return ResponseProcessingLoop();
        }

        /// <summary>Connects to the Query API server.</summary>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public override Task<CancellationTokenSource> ConnectAsync()
        {
            throw new InvalidOperationException("ConnectAsync Method is not supported in ssh query. Please use the telnet query.");
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
                        line = _shell.ReadLine();
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

                    if (string.IsNullOrWhiteSpace(line) || string.IsNullOrEmpty(line) || line.StartsWith(username))
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
