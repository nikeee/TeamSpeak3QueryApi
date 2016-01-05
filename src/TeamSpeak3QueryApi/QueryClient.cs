using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TeamSpeak3QueryApi.Net
{
    /// <summary>Represents a client that can be used to access the TeamSpeak Query API on a remote server.</summary>
    public class QueryClient : IDisposable
    {
        /// <summary>Gets the remote host of the Query API client.</summary>
        /// <returns>The remote host of the Query API client.</returns>
        public string Host { get; private set; }

        /// <summary>Gets the remote port of the Query API client.</summary>
        /// <returns>The remote port of the Query API client.</returns>
        public short Port { get; private set; }

        public bool IsConnected { get; private set; }

        /// <summary>The default host which is used when no host is provided.</summary>
        public static readonly string DefaultHost = "localhost";

        /// <summary>The default port which is used when no port is provided.</summary>
        public static readonly short DefaultPort = 10011;

        private readonly TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;
        private NetworkStream _ns;
        private volatile bool _cancelTask;
        private readonly Queue<QueryCommand> _queue = new Queue<QueryCommand>();
        private readonly ConcurrentDictionary<string, List<Action<NotificationData>>> _subscriptions = new ConcurrentDictionary<string, List<Action<NotificationData>>>();

        #region Ctors

        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the <see cref="QueryClient.DefaultHost"/> and <see cref="QueryClient.DefaultPort"/>.</summary>
        public QueryClient()
            : this(DefaultHost, DefaultPort)
        { }

        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the provided host and the <see cref="QueryClient.DefaultPort"/>.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        public QueryClient(string hostName)
            : this(hostName, DefaultPort)
        { }
        /// <summary>Creates a new instance of <see cref="TeamSpeak3QueryApi.Net.QueryClient"/> using the provided host TCP port.</summary>
        /// <param name="hostName">The host name of the remote server.</param>
        /// <param name="port">The TCP port of the Query API server.</param>
        public QueryClient(string hostName, short port)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                throw new ArgumentNullException("hostName");
            if (port < 1)
                throw new ArgumentException("Invalid port.");

            Host = hostName;
            Port = port;
            IsConnected = false;
            _client = new TcpClient();
        }

        #endregion

        /// <summary>Connects to the Query API server.</summary>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async Task Connect()
        {
            await _client.ConnectAsync(Host, Port);
            if (!_client.Connected)
                throw new InvalidOperationException("Could not connect.");

            _ns = _client.GetStream();
            _reader = new StreamReader(_ns);
            _writer = new StreamWriter(_ns) { NewLine = "\n" };

            IsConnected = true;

            await _reader.ReadLineAsync();
            await _reader.ReadLineAsync(); // Ignore welcome message
            await _reader.ReadLineAsync();

            _cancelTask = false;
            ResponseProcessingLoop();
        }

        #region Send

        /// <summary>Sends a Query API command wihtout parameters to the server.</summary>
        /// <param name="cmd">The command.</param>
        /// <returns>An awaitable <see cref="T:System.Net.Threading.Tasks.Task{QueryResponseDictionary[]}"/>.</returns>
        public Task<QueryResponseDictionary[]> Send(string cmd)
        {
            return Send(cmd, null);
        }

        /// <summary>Sends a Query API command with parameters to the server.</summary>
        /// <param name="cmd">The command.</param>
        /// <param name="parameters">The parameters of the command.</param>
        /// <returns>An awaitable <see cref="T:System.Net.Threading.Tasks.Task{QueryResponseDictionary[]}"/>.</returns>
        public Task<QueryResponseDictionary[]> Send(string cmd, params Parameter[] parameters)
        {
            return Send(cmd, parameters, null);
        }

        /// <summary>Sends a Query API command with parameters and options to the server.</summary>
        /// <param name="cmd">The command.</param>
        /// <param name="parameters">The parameters of the command.</param>
        /// <param name="options">The options of the command.</param>
        /// <returns>An awaitable <see cref="T:System.Net.Threading.Tasks.Task{QueryResponseDictionary[]}"/>.</returns>
        public async Task<QueryResponseDictionary[]> Send(string cmd, Parameter[] parameters, string[] options)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                throw new ArgumentNullException("cmd"); //return Task.Run( () => throw new ArgumentNullException("cmd"));

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

            await CheckQueue();

            return await d.Task;
        }

        #endregion
        #region Subscriptions

        /// <summary>Subscribes to a notification. If the subscribed notification is received, the callback is getting executed.</summary>
        /// <param name="notificationName">The name of the notification (without the "notify" prefix).</param>
        /// <param name="callback">The callback to execute on occurrence.</param>
        public void Subscribe(string notificationName, Action<NotificationData> callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");
            if (string.IsNullOrWhiteSpace(notificationName))
                throw new ArgumentNullException("notificationName");
            notificationName = NormalizeNotificationName(notificationName);

            if (_subscriptions.ContainsKey(notificationName))
            {
                _subscriptions[notificationName].Add(callback);
            }
            else
            {
                _subscriptions[notificationName] = new List<Action<NotificationData>>() { callback };
            }
        }

        /// <summary>Unsubscribes all callbacks of a notification.</summary>
        /// <param name="notificationName">The name of the notification to unsubscribe (without the "notify" prefix).</param>
        public void Unsubscribe(string notificationName)
        {
            if (string.IsNullOrWhiteSpace(notificationName))
                throw new ArgumentNullException("notificationName");
            notificationName = NormalizeNotificationName(notificationName);

            if (!_subscriptions.ContainsKey(notificationName))
                return;
            _subscriptions[notificationName].Clear();
            _subscriptions[notificationName] = null;
            List<Action<NotificationData>> dummy;
            _subscriptions.TryRemove(notificationName, out dummy);
        }

        /// <summary>Unsubscribe a callback of a notification.</summary>
        /// <param name="notificationName">The name of the notification to unsubscribe (without the "notify" prefix).</param>
        /// <param name="callback">The callback to unsubscribe.</param>
        public void Unsubscribe(string notificationName, Action<NotificationData> callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");
            if (string.IsNullOrWhiteSpace(notificationName))
                throw new ArgumentNullException("notificationName");
            notificationName = NormalizeNotificationName(notificationName);

            if (!_subscriptions.ContainsKey(notificationName))
                return;
            _subscriptions[notificationName].Remove(callback);
        }

        private static string NormalizeNotificationName(string name)
        {
            return name.Trim().ToUpperInvariant();
        }

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

                        int intVal;
                        if (int.TryParse(value, out intVal))
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
                throw new ArgumentNullException("errorString");
            errorString = errorString.Remove(0, "error ".Length);

            var errParams = errorString.Split(' ');
            /*
             id=2568
             msg=insufficient\sclient\spermissions
             failed_permid=27
            */
            var parsedError = new QueryError() { FailedPermissionId = -1 };
            for (int i = 0; i < errParams.Length; ++i)
            {
                var errData = errParams[i].Split('=');
                /*
                 id
                 2568
                */
                string fieldName = errData[0].ToUpperInvariant();
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
                    var cb = cbs[i];
                    if (cb != null)
                        cb(notification.Data);
                }
            }
        }

        private void ResponseProcessingLoop()
        {
            Task.Run(async () =>
            {
                while (!_cancelTask)
                {
                    var line = await _reader.ReadLineAsync();
                    Trace.WriteLine(line);
                    if(string.IsNullOrWhiteSpace(line))
                        continue;
                    var s = line.Trim();
                    if (s.StartsWith("error", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Debug.Assert(_currentCommand != null);

                        var error = ParseError(s);
                        _currentCommand.Error = error;
                        InvokeResponse(_currentCommand);
                    }
                    else if (s.StartsWith("notify", StringComparison.InvariantCultureIgnoreCase))
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
            });
        }

        private QueryCommand _currentCommand;
        private async Task CheckQueue()
        {
            if (_queue.Count > 0)
            {
                _currentCommand = _queue.Dequeue();
                Trace.WriteLine(_currentCommand.SentText);
                await _writer.WriteLineAsync(_currentCommand.SentText);
                await _writer.FlushAsync();
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
                //TODO: Test this
                if (_client != null)
                    _client.Close();
                if (_ns != null)
                    _ns.Dispose();
                if (_reader != null)
                    _reader.Dispose();
                if (_writer != null)
                    _writer.Dispose();
            }
        }

        #endregion
    }
}
