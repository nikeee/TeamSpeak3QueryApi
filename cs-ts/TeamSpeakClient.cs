using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CsTs
{
    public class TeamSpeakClient
    {
        public string Host { get; private set; }
        public short Port { get; private set; }

        public static readonly string DefaultHost = "localhost";
        public static readonly short DefaultPort = 10011;

        private readonly TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;
        private NetworkStream _ns;
        private volatile bool _cancelTask;

        public TeamSpeakClient()
            : this(DefaultHost, DefaultPort)
        { }
        public TeamSpeakClient(string hostName)
            : this(hostName, DefaultPort)
        { }
        public TeamSpeakClient(string hostName, short port)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                throw new ArgumentNullException("hostName");
            if (port < 1)
                throw new ArgumentException("Invalid port.");

            Host = hostName;
            Port = port;
            _client = new TcpClient();
        }

        public async Task Connect()
        {
            await _client.ConnectAsync(Host, Port);
            if (!_client.Connected)
                throw new InvalidOperationException("Could not connect.");

            _ns = _client.GetStream();
            _reader = new StreamReader(_ns);
            _writer = new StreamWriter(_ns) { NewLine = "\n" };

            await _reader.ReadLineAsync();
            await _reader.ReadLineAsync(); // Ignore welcome message
            await _reader.ReadLineAsync();

            _cancelTask = false;
            ResponseProcessingLoop();
        }

        public void Disconnect()
        {
            _cancelTask = true;
            _client.Close();
        }

        private readonly Queue<QueryCommand> _queue = new Queue<QueryCommand>();

        public Task<QueryResponse[]> Send(string cmd)
        {
            return Send(cmd, null);
        }
        public Task<QueryResponse[]> Send(string cmd, params Parameter[] parameters)
        {
            return Send(cmd, parameters, null);
        }
        public async Task<QueryResponse[]> Send(string cmd, Parameter[] parameters, string[] options)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                throw new ArgumentNullException("cmd"); //return Task.Run( () => throw new ArgumentNullException("cmd"));

            options = options ?? new string[0];
            parameters = parameters ?? new Parameter[0];

            var toSend = new StringBuilder(cmd.TeamSpeakEscape());
            for (int i = 0; i < options.Length; ++i)
                toSend.Append(" -").Append(options[i].TeamSpeakEscape());

            foreach (var p in parameters)
            {
                toSend.Append(' ')
                    .Append(p.Name.TeamSpeakEscape())
                    .Append('=')
                    .Append(p.Value.CreateParameterLine());
            }

            var d = new TaskCompletionSource<QueryResponse[]>();

            var newItem = new QueryCommand(cmd, parameters, options, d, toSend.ToString());

            _queue.Enqueue(newItem);

            await CheckQueue();

            return await d.Task;
        }

        private readonly ConcurrentDictionary<string, List<Action<NotificationData>>> _subscriptions = new ConcurrentDictionary<string, List<Action<NotificationData>>>();

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

        private static QueryResponse[] ParseResponse(string rawResponse)
        {
            var records = rawResponse.Split('|');
            var response = records.Select(s =>
            {
                var args = s.Split(' ');
                var r = new QueryResponse();
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
                        throw new TeamSpeakQueryProtocolException();
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
                forCommand.Defer.TrySetResult(forCommand.Response);
            }
        }

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
                        if (string.IsNullOrWhiteSpace(s))
                        {
                            _currentCommand.RawResponse = "";
                            _currentCommand.Response = new QueryResponse[0];
                        }
                        else
                        {
                            _currentCommand.RawResponse = s;
                            _currentCommand.Response = ParseResponse(s);
                        }
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
    }
}
