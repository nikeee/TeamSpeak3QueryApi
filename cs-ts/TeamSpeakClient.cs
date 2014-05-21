using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            _writer = new StreamWriter(_ns) { NewLine = "\n" };

            await _reader.ReadLineAsync();
            await _reader.ReadLineAsync(); // Ignore welcome message
            await _reader.ReadLineAsync();

            ResponseProcessingLoop();
        }

        private Queue<QueryCommand> _queue = new Queue<QueryCommand>();

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
                    .Append(p.Value.GetParameterLine());
            }

            var d = new TaskCompletionSource<QueryResponse[]>();

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
                    else
                    {
                        r[arg] = null;
                    }
                }
                return r;
            });

            return response.ToArray();
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
                        parsedError.Message = errData.Length > 1 ? errData[1].TeamSpeakUnescape() : null;
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
            // TODO
        }

        private Task ResponseProcessingLoop()
        {
            //TODO: Refactor to readline loop
            return Task.Run(async () =>
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
                        var not = ParseNotification(s);
                        InvokeNotification(not);
                    }
                    else
                    {
                        if (_currentCommand == null)
                            throw new DivideByZeroException("wut");
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

    public interface IParameterValue
    {
        string GetParameterLine();
    }

    public class ParameterValueArray : IParameterValue
    {
        private ParameterValue[] _arr;

        public ParameterValueArray()
            : this(null)
        { }
        public ParameterValueArray(ParameterValue[] arr)
        {
            _arr = arr;
        }

        public string GetParameterLine()
        {
            if (_arr == null)
                return string.Empty;
            var strs = _arr.Select(kv => kv.GetParameterLine());
            return string.Join("|", strs);
        }

        public static implicit operator ParameterValueArray(ParameterValue[] fromParameters)
        {
            return new ParameterValueArray(fromParameters);
        }

        public override string ToString()
        {
            if (_arr == null)
                return "Params: null";
            return string.Concat("Param: [", string.Join(", ", _arr.Select(s => s.ToString())), "]");
        }
    }

    public class ParameterValue : IParameterValue
    {
        //public string Key { get; set; }
        public string Value { get; set; }

        public ParameterValue()
            : this(null)
        { }

        public ParameterValue(string value)
        {
            Value = value;
        }

        public string GetParameterLine()
        {
            //var k = Key ?? "";
            var v = Value ?? "";
            //return k.TeamSpeakEscape() + "=" + v.TeamSpeakEscape();
            return v.TeamSpeakEscape();
        }
        //public static implicit operator Parameter(string fromParameter)
        //{
        //    return new Parameter(fromParameter);
        //}
        public static implicit operator ParameterValue(string fromParameter)
        {
            return new ParameterValue(fromParameter);
        }
        public static implicit operator ParameterValue(int fromParameter)
        {
            return new ParameterValue(fromParameter.ToString());
        }

        public override string ToString()
        {
            return string.Concat("Param: ", Value ?? "null");
        }
    }

    public class Parameter
    {
        public string Name { get; set; }
        public IParameterValue Value { get; set; }

        public Parameter(string name, ParameterValue value)
            : this(name, value, true)
        { }
        public Parameter(string name, ParameterValueArray values)
            : this(name, values, true)
        { }

        public Parameter(string name, IParameterValue value, bool overloadFix)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");
            Name = name;
            Value = value;
        }

        public static implicit operator Parameter(string[] fromArray)
        {
            Debug.Assert(fromArray != null);

            if(fromArray.Length == 0)
                throw new ArgumentException("Invalid parameters");

            var name = fromArray[0];
            if(fromArray.Length == 2)
                return new Parameter(name, new ParameterValue(fromArray[1]));

            var values = new ParameterValue[fromArray.Length - 1];
            for (int i = 1; i < fromArray.Length; ++i)
                values[i - 1] = new ParameterValue(fromArray[i]);
            return new Parameter(name, new ParameterValueArray(values));
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
        public Parameter[] Parameters { get; private set; }
        public string SentText { get; private set; }
        public TaskCompletionSource<QueryResponse[]> Defer { get; private set; }

        public string RawResponse { get; set; }
        public QueryResponse[] Response { get; set; }
        public QueryError Error { get; set; }

        public QueryCommand(string cmd, Parameter[] parameters, string[] options, TaskCompletionSource<QueryResponse[]> defer, string sentText)
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
