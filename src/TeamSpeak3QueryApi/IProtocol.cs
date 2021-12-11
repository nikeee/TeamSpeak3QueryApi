using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeamSpeak3QueryApi.Net
{
    public interface IProtocol : IDisposable
    {
        bool IsConnected { get; }
        Task ConnectAsync(string host, ushort port, CancellationToken cancellationToken);
        Task<string> ReadLineAsync(CancellationToken cancellationToken);
        Task WriteLineAsync(string line, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
    }

    public class RawTcpProtocol : IProtocol
    {
        public TcpClient Client { get; }

        public bool IsConnected => Client.Connected;


        private StreamReader _reader;
        private StreamWriter _writer;
        private NetworkStream _ns;

        public RawTcpProtocol()
        {
            var client = new TcpClient();
            Client = client;
        }

        public Task ConnectAsync(string host, ushort port, CancellationToken cancellationToken)
        {
            _ns = Client.GetStream();
            _reader = new StreamReader(_ns);
            _writer = new StreamWriter(_ns) { NewLine = "\n" };

            throw new NotImplementedException();
        }

        public Task<string> ReadLineAsync(CancellationToken cancellationToken) => _reader.ReadLineAsync();
        public Task WriteLineAsync(string line, CancellationToken cancellationToken) => _writer.WriteLineAsync(line);
        public Task FlushAsync(CancellationToken cancellationToken) => _writer.FlushAsync();


        #region IDisposable support

        public void Dispose()
        {
            _ns?.Dispose();
            _reader?.Dispose();
            _writer?.Dispose();
            Client?.Dispose();
        }

        #endregion
    }
}
