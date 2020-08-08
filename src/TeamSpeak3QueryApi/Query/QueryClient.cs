using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
using TeamSpeak3QueryApi.Net.Enums;
using TeamSpeak3QueryApi.Net.Extensions;
using TeamSpeak3QueryApi.Net.Notifications;
using TeamSpeak3QueryApi.Net.Parameters;

namespace TeamSpeak3QueryApi.Net.Query
{
    /// <summary>Represents a client that can be used to access the TeamSpeak Query API on a remote server.</summary>
    public class QueryClient : IDisposable
    {
        /// <summary>
        /// Events for handling in the correct moment
        /// </summary>
        public EventHandler OnConnectionLost;
        public EventHandler OnConnected;
        public EventHandler OnDisconnected;

        /// <summary>Gets the remote host of the Query API client.</summary>
        /// <returns>The remote host of the Query API client.</returns>
        public string Host { get; }

        /// <summary>Gets the remote port of the Query API client.</summary>
        /// <returns>The remote port of the Query API client.</returns>
        public int Port { get; }

        public TeamspeakConnectionType ConnectionType { get; set; }

        public bool IsConnected { get; private set; }

        /// <summary>The default host which is used when no host is provided.</summary>
        public const string DefaultHost = "localhost";

        /// <summary>The default port which is used when no port is provided.</summary>
        public const short DefaultPort = 10022;

        public TcpClient Client { get; }
        private StreamReader _reader;
        private StreamWriter _writer;
        private NetworkStream _ns;
        private CancellationTokenSource _cts;
        private readonly Queue<QueryCommand> _queue = new Queue<QueryCommand>();
        private readonly ConcurrentDictionary<string, List<Action<NotificationData>>> _subscriptions = new ConcurrentDictionary<string, List<Action<NotificationData>>>();

        SshClient _sshClient;
        ShellStream _shell;
        string username;

        #region Ctors

        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the <see cref="QueryClient.DefaultHost"/> and <see cref="QueryClient.DefaultPort"/>.</summary>
        public QueryClient(TeamspeakConnectionType type)
            : this(DefaultHost, DefaultPort, type)
        { }

        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the provided host and the <see cref="QueryClient.DefaultPort"/>.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        public QueryClient(string hostName, TeamspeakConnectionType type)
            : this(hostName, DefaultPort, type)
        { }
        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the provided host TCP port.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        /// <param name="port">The TCP port of the Query API server.</param>
        public QueryClient(string hostName, int port, TeamspeakConnectionType type)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                throw new ArgumentNullException(nameof(hostName));
            if (!ValidationHelper.ValidateTcpPort(port))
                throw new ArgumentOutOfRangeException(nameof(port));

            Host = hostName;
            Port = port;
            ConnectionType = type;
            IsConnected = false;
            Client = new TcpClient();
        }

        #endregion

        /// <summary>Connects to the Query API server.</summary>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async Task<CancellationTokenSource> ConnectAsync()
        {
            if(ConnectionType != TeamspeakConnectionType.Telnet)
                throw new InvalidOperationException("ConnectAsync Method without parameters can only be used with telnet Query. Please use ConnectSsh method.");

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

        public CancellationTokenSource ConnectSsh(string username, string password)
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

        public void Disconnect()
        {
            if (_cts == null)
                return;

            OnDisconnected?.Invoke(this, EventArgs.Empty);
            _cts.Cancel();
        }

        #region Send

        /// <summary>Sends a Query API command wihtout parameters to the server.</summary>
        /// <param name="cmd">The command.</param>
        /// <returns>An awaitable <see cref="T:System.Net.Threading.Tasks.Task{QueryResponseDictionary[]}"/>.</returns>
        public Task<QueryResponseDictionary[]> SendAsync(string cmd) => SendAsync(cmd, null);

        /// <summary>Sends a Query API command with parameters to the server.</summary>
        /// <param name="cmd">The command.</param>
        /// <param name="parameters">The parameters of the command.</param>
        /// <returns>An awaitable <see cref="T:System.Net.Threading.Tasks.Task{QueryResponseDictionary[]}"/>.</returns>
        public Task<QueryResponseDictionary[]> SendAsync(string cmd, params Parameter[] parameters) => SendAsync(cmd, parameters, null);

        /// <summary>Sends a Query API command with parameters and options to the server.</summary>
        /// <param name="cmd">The command.</param>
        /// <param name="parameters">The parameters of the command.</param>
        /// <param name="options">The options of the command.</param>
        /// <returns>An awaitable <see cref="T:System.Net.Threading.Tasks.Task{QueryResponseDictionary[]}"/>.</returns>
        public async Task<QueryResponseDictionary[]> SendAsync(string cmd, Parameter[] parameters, string[] options)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                throw new ArgumentNullException(nameof(cmd)); //return Task.Run( () => throw new ArgumentNullException("cmd"));

            options = options ?? new string[0];
            var ps = parameters == null ? new List<Parameter>() : new List<Parameter>(parameters);

            var toSend = new StringBuilder(cmd.TeamSpeakEscape());
            for (int i = 0; i < options.Length; ++i)
                toSend.Append(" -").Append(options[i].ToLowerInvariant().TeamSpeakEscape());

            // Parameter arrays should be the last parameters in the list
            var lastParamArray = ps.SingleOrDefault(p => p.Value is ParameterValueArray);
            if (lastParamArray != null)
            {
                ps.MoveToBottom(lastParamArray);
            }

            foreach (var p in ps)
                toSend.Append(' ').Append(p.GetEscapedRepresentation());

            var d = new TaskCompletionSource<QueryResponseDictionary[]>();

            var newItem = new QueryCommand(cmd, ps.AsReadOnly(), options, d, toSend.ToString());
            
            _queue.Enqueue(newItem);

            await CheckQueueAsync().ConfigureAwait(false);
            return await d.Task.ConfigureAwait(false);
        }

        #endregion
        #region Subscriptions

        /// <summary>Subscribes to a notification. If the subscribed notification is received, the callback is getting executed.</summary>
        /// <param name="notificationName">The name of the notification (without the "notify" prefix).</param>
        /// <param name="callback">The callback to execute on occurrence.</param>
        public void Subscribe(string notificationName, Action<NotificationData> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            if (string.IsNullOrWhiteSpace(notificationName))
                throw new ArgumentNullException(nameof(notificationName));
            notificationName = NormalizeNotificationName(notificationName);

            if (_subscriptions.ContainsKey(notificationName))
            {
                _subscriptions[notificationName].Add(callback);
            }
            else
            {
                _subscriptions[notificationName] = new List<Action<NotificationData>> { callback };
            }
        }

        /// <summary>Unsubscribes all callbacks of a notification.</summary>
        /// <param name="notificationName">The name of the notification to unsubscribe (without the "notify" prefix).</param>
        public void Unsubscribe(string notificationName)
        {
            if (string.IsNullOrWhiteSpace(notificationName))
                throw new ArgumentNullException(nameof(notificationName));
            notificationName = NormalizeNotificationName(notificationName);

            if (!_subscriptions.ContainsKey(notificationName))
                return;

            _subscriptions[notificationName].Clear();
            _subscriptions[notificationName] = null;
            _subscriptions.TryRemove(notificationName, out var _); // TODO: Revisit this
        }

        /// <summary>Unsubscribe a callback of a notification.</summary>
        /// <param name="notificationName">The name of the notification to unsubscribe (without the "notify" prefix).</param>
        /// <param name="callback">The callback to unsubscribe.</param>
        public void Unsubscribe(string notificationName, Action<NotificationData> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            if (string.IsNullOrWhiteSpace(notificationName))
                throw new ArgumentNullException(nameof(notificationName));
            notificationName = NormalizeNotificationName(notificationName);

            if (!_subscriptions.ContainsKey(notificationName))
                return;
            _subscriptions[notificationName].Remove(callback);
        }

        private static string NormalizeNotificationName(string name) => name.Trim().ToUpperInvariant();

        #endregion
        #region Parsing

        private static QueryResponseDictionary[] ParseResponse(string rawResponse)
        {
            var records = rawResponse.Split('|');
            var response = records.Select(s =>
            {
                var args = s.Split(' ');
                var r = new QueryResponseDictionary();
                foreach (var arg in args)
                {
                    if (arg.Contains('='))
                    {
                        var eqIndex = arg.IndexOf('=');

                        var key = arg.Substring(0, eqIndex).TeamSpeakUnescape();
                        var value = arg.Remove(0, eqIndex + 1);

                        if (int.TryParse(value, out var intVal))
                            r[key] = intVal;
                        else
                            r[key] = value;
                    }
                    else
                    {
                        r[arg] = null;
                    }
                }
                return r;
            });

            return response.ToArray();
        }

        private static QueryError ParseError(string errorString)
        {
            // Ex:
            // error id=2568 msg=insufficient\sclient\spermissions failed_permid=27
            if (errorString == null)
                throw new ArgumentNullException(nameof(errorString));
            errorString = errorString.Remove(0, "error ".Length);

            var errParams = errorString.Split(' ');
            /*
             id=2568
             msg=insufficient\sclient\spermissions
             failed_permid=27
            */
            var parsedError = new QueryError { FailedPermissionId = -1 };
            for (int i = 0; i < errParams.Length; ++i)
            {
                var errData = errParams[i].Split('=');
                /*
                 id
                 2568
                */
                var fieldName = errData[0].ToUpperInvariant();
                switch (fieldName)
                {
                    case "ID":
                        parsedError.Id = errData.Length > 1 ? int.Parse(errData[1], NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.CurrentCulture) : -1;
                        continue;
                    case "MSG":
                        parsedError.Message = errData.Length > 1 ? errData[1].TeamSpeakUnescape() : null;
                        continue;
                    case "FAILED_PERMID":
                        parsedError.FailedPermissionId = errData.Length > 1 ? int.Parse(errData[1], NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.CurrentCulture) : -1;
                        continue;
                    case "EXTRA_MSG":
                        parsedError.ExtraMessage = errData.Length > 1 ? errData[1].TeamSpeakUnescape() : null;
                        continue;
                    default:
                        throw new QueryProtocolException();
                }
            }
            return parsedError;
        }

        private static QueryNotification ParseNotification(string notificationString)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(notificationString));

            var index = notificationString.IndexOf(' ');

            Debug.Assert(index > -1);

            var notificationName = notificationString.Remove(index);
            Debug.Assert(!string.IsNullOrWhiteSpace(notificationName));

            var payload = notificationString.Substring(notificationName.Length + 1);

            var qRes = ParseResponse(payload); // Not tested
            var notData = new NotificationData(qRes);
            return new QueryNotification(notificationName, notData);
        }

        private static void InvokeResponse(QueryCommand forCommand)
        {
            Debug.Assert(forCommand != null);
            Debug.Assert(forCommand.Defer != null);
            Debug.Assert(forCommand.Error != null);

            if (forCommand.Error.Id != 0)
            {
                forCommand.Defer.TrySetException(new QueryException(forCommand.Error));
            }
            else
            {
                forCommand.Defer.TrySetResult(forCommand.ResponseDictionary);
            }
        }

        #endregion
        #region Invocation

        private void InvokeNotification(QueryNotification notification)
        {
            Debug.Assert(notification != null);
            Debug.Assert(notification.Name != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(notification.Name));

            if (_subscriptions.Count == 0)
                return; // going short here

            var notName = NormalizeNotificationName(notification.Name);
            if (_subscriptions.ContainsKey(notName))
            {
                var cbs = _subscriptions[notName];
                for (int i = 0; i < cbs.Count; ++i)
                {
                    cbs[i]?.Invoke(notification.Data);
                }
            }
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
                        if(ConnectionType == TeamspeakConnectionType.SSH)
                        {
                            line = _shell.ReadLine();
                        }
                        else
                        {
                            line = await _reader.ReadLineAsync().WithCancellation(cts.Token).ConfigureAwait(false);
                        }
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

        private QueryCommand _currentCommand;
        private async Task CheckQueueAsync()
        {
            if (_queue.Count > 0)
            {
                _currentCommand = _queue.Dequeue();
                Debug.WriteLine($"{ConnectionType}: {_currentCommand.SentText}");
                await _writer.WriteLineAsync((ConnectionType == TeamspeakConnectionType.Telnet ? _currentCommand.SentText : _currentCommand.SentText + "\n")).ConfigureAwait(false);
                await _writer.FlushAsync().ConfigureAwait(false);
            }
        }

        #endregion
        #region IDisposable support

        /// <summary>Finalizes the object.</summary>
        ~QueryClient()
        {
            Dispose(false);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <param name="disposing">A value indicating whether the object is disposing or finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                Client?.Dispose();
                _ns?.Dispose();
                _reader?.Dispose();
                _writer?.Dispose();
                _shell?.Dispose();
                _sshClient?.Dispose();
            }
        }

        #endregion
    }
}
