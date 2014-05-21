using System;
using System.Collections.Generic;
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

        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;
        private NetworkStream _ns;
        private volatile bool _cancelTask = false;

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
            _writer = new StreamWriter(_ns);

            await _reader.ReadLineAsync();
            await _reader.ReadLineAsync(); // Ignore welcome message
            await _reader.ReadLineAsync();
        }

        private Queue<QueryCommand> _queue = new Queue<QueryCommand>();

        public async Task<QueryResponse> Send(string cmd, Dictionary<string, ParameterValue> parameters, string[] options)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                throw new ArgumentNullException("cmd"); //return Task.Run( () => throw new ArgumentNullException("cmd"));

            options = options ?? new string[0];
            parameters = parameters ?? new Dictionary<string, ParameterValue>();

            var toSend = new StringBuilder(cmd.TeamSpeakEscape());
            for (int i = 0; i < options.Length; ++i)
                toSend.Append(" -").Append(options[i].TeamSpeakEscape());

            foreach (var p in parameters)
            {
                toSend.Append(' ')
                    .Append(p.Key.TeamSpeakEscape())
                    .Append('=')
                    .Append(p.Value.GetParameterLine());
            }

            var d = new TaskCompletionSource<QueryResponse>();

            var newItem = new QueryCommand(cmd, parameters, options, d, toSend.ToString());

            _queue.Enqueue(newItem);

            await CheckQueue();

            return await d.Task;
        }

        private QueryResponse[] ParseResponse(string rawResponse)
        {
            var records = rawResponse.Split('|');
            var response = records.Select<string, QueryResponse>(s =>
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
                    r[arg] = "";
                }
                return r;
            });

            var enumeratedResponse = response.ToArray();
            return enumeratedResponse;
        }
        private QueryError ParseError(string errorString)
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
                        parsedError.Id = errData.Length > 1 ? int.Parse(errData[1]) : -1;
                        continue;
                    case "MSG":
                        parsedError.Message = errData.Length > 1 ? errData[1].TeamSpeakUnescape() : "";
                        continue;
                    case "FAILED_PERMID":
                        parsedError.FailedPermissionId = errData.Length > 1 ? int.Parse(errData[1]) : -1;
                        continue;
                    default:
                        throw new TeamSpeakQueryProtocolException();
                }
            }
            return parsedError;
        }

        private QueryNotification ParseNotification(string notificationString)
        {
            throw new NotImplementedException();
        }

        private void InvokeResponse(QueryCommand forCommand)
        {

        }

        private void InvokeNotification(QueryNotification notification)
        {

        }

        private async Task CheckResponse()
        {
            //TODO: Refactor to readline loop

            while (!_cancelTask)
            {
                var line = await _reader.ReadLineAsync();
                var s = line.Trim();
                if (s.StartsWith("error", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (_currentCommand == null)
                        throw new DivideByZeroException("wut1");
                    var error = ParseError(s);
                    _currentCommand.Error = error;
                    InvokeResponse(_currentCommand);
                    continue;
                }
                else if (s.StartsWith("notify", StringComparison.InvariantCultureIgnoreCase))
                {
                    var not = ParseNotification(s);
                    InvokeNotification(not);
                    continue;
                }
                else
                {
                    if (_currentCommand == null)
                        throw new DivideByZeroException("wut");

                    _currentCommand.RawResponse = s;
                    _currentCommand.Response = ParseResponse(s);
                    continue;
                }
            }
        }

        private QueryCommand _currentCommand;
        private Task CheckQueue()
        {
            if (_queue.Count > 0)
            {
                _currentCommand = _queue.Dequeue();
                return _writer.WriteLineAsync(_currentCommand.SentText);
            }
            return null;
        }
    }

    public class QueryException : Exception
    {
        public QueryError Error { get; private set; }

        public QueryException(QueryError error)
        {
            Error = error;
        }
    }

    public class TeamSpeakQueryProtocolException : Exception
    {

    }

    public interface ParameterValue
    {
        string GetParameterLine();
    }

    public class ParameterArray : ParameterValue
    {
        private Parameter[] _arr;

        public string GetParameterLine()
        {
            if (_arr == null)
                return string.Empty;
            var strs = _arr.Select(kv => kv.GetParameterLine());
            return string.Join("|", strs);
        }
    }

    public class Parameter : ParameterValue
    {
        //public string Key { get; set; }
        public string Value { get; set; }

        public string GetParameterLine()
        {
            //var k = Key ?? "";
            var v = Value ?? "";
            //return k.TeamSpeakEscape() + "=" + v.TeamSpeakEscape();
            return v.TeamSpeakEscape();
        }
    }

    public class QueryError
    {
        public int Id { get; internal set; }
        public string Message { get; internal set; }
        public int FailedPermissionId { get; internal set; }
    }

    public class QueryNotification
    {

    }

    class QueryCommand
    {
        public string Command { get; private set; }
        public string[] Options { get; private set; }
        public Dictionary<string, ParameterValue> Parameters { get; private set; }
        public string SentText { get; private set; }
        public TaskCompletionSource<QueryResponse> Defer { get; private set; }

        public string RawResponse { get; set; }
        public QueryResponse[] Response { get; set; }
        public QueryError Error { get; set; }

        public QueryCommand(string cmd, Dictionary<string, ParameterValue> parameters, string[] options, TaskCompletionSource<QueryResponse> defer, string sentText)
        {
            Command = cmd;
            Parameters = parameters;
            Options = options;
            Defer = defer;
            SentText = sentText;
        }
    }

    public class QueryResponse : Dictionary<string, object>
    {

    }
}
