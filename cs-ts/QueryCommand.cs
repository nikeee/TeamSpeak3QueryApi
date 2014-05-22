using System.Threading.Tasks;

namespace CsTs
{
    internal class QueryCommand
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
}
