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
        private QueryCommand _currentCommand;

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

        private async Task CheckResponse()
        {
            //TODO: Refactor to readline loop

            /*
            var line = await _reader.ReadLineAsync();

            if (string.IsNullOrEmpty(line))
                return;

            var s = line.Trim();

            QueryResponse[] responseItems;

            if (s.StartsWith("error", StringComparison.InvariantCultureIgnoreCase))
            {
                responseItems = ParseResponse(s.Remove(0, "error".Length).Trim());

                var firstRes = responseItems.First();

                if (responseItems.Length > 1)
                {
                    var l = responseItems.ToList();
                    l.Remove(firstRes);
                    responseItems = l.ToArray();
                }

                object errorId;
                firstRes.TryGetValue("id", out errorId);

                object errorMessage;
                firstRes.TryGetValue("msg", out errorMessage);

                //TODO: Permission

                var currentError = new QueryError()
                {
                    Id = (int)errorId,
                    Message = errorMessage as string
                };

                if (currentError.Id != 0)
                    _currentCommand.Error = currentError;
                else
                    _currentCommand.Error = null;

                if (_currentCommand.Defer != null)
                {
                    if (_currentCommand.Error != null && _currentCommand.Error.Id != 0)
                    {
                        _currentCommand.Defer.SetException(new QueryException(_currentCommand.Error));
                    }
                    else
                    {
                        //_currentCommand.Defer.SetResult(responseItems);
                    }
                }
            }
            else if (s.StartsWith("notify", StringComparison.InvariantCultureIgnoreCase))
            {
                // TODO
            }
            else if (_currentCommand != null)
            {
                _currentCommand.RawResponse = s;
                _currentCommand.Response = ParseResponse(s);
            }

            */
        }

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
        public int Id { get; set; }
        public string Message { get; set; }
        public int MissingPermission { get; set; }
    }

    class QueryCommand
    {
        public string Command { get; private set; }
        public string[] Options { get; private set; }
        public Dictionary<string, ParameterValue> Parameters { get; private set; }
        public string SentText { get; private set; }
        public TaskCompletionSource<QueryResponse> Defer { get; private set; }

        public string RawResponse { get; set; }
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
