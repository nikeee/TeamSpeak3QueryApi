using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeamSpeak3QueryApi.Net;

internal class QueryCommand
{
    public string Command { get; }
    public string[] Options { get; }
    public IReadOnlyCollection<Parameter> Parameters { get; }
    public string SentText { get; }
    public TaskCompletionSource<QueryResponseDictionary[]> Defer { get; }

    public string RawResponse { get; set; }
    public QueryResponseDictionary[] ResponseDictionary { get; set; }
    public QueryError Error { get; set; }

    public QueryCommand(string cmd, IReadOnlyCollection<Parameter> parameters, string[] options, TaskCompletionSource<QueryResponseDictionary[]> defer, string sentText)
    {
        Command = cmd;
        Parameters = parameters;
        Options = options;
        Defer = defer;
        SentText = sentText;
    }
}
