using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace TeamSpeak3QueryApi.Net
{
    public interface IProtocol : IDisposable
    {
        bool IsConnected { get; }
        bool RequiresAuthenticationOnConnect { get; }
        Task ConnectAsync(string host, ushort port, CancellationToken cancellationToken);
        Task ConnectAsync(string host, ushort port, string userName, string password, CancellationToken cancellationToken);
        Task<string> ReadLineAsync(CancellationToken cancellationToken);
        Task WriteLineAsync(string line, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
    }

    public class RawTcpProtocol : IProtocol
    {
        public TcpClient Client { get; }
        public bool IsConnected => Client.Connected;
        public bool RequiresAuthenticationOnConnect => false;

        private StreamReader? _reader;
        private StreamWriter? _writer;
        private NetworkStream? _ns;

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
            return Task.CompletedTask;
        }

        public Task ConnectAsync(string host, ushort port, string userName, string password, CancellationToken cancellationToken) => throw new NotSupportedException($"{nameof(RawTcpProtocol)} does not support credentials on connection creation.");

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

    public class SshProtocol : IProtocol
    {
        public SshClient? Client { get; private set; }
        public bool IsConnected => Client?.IsConnected ?? false;
        public bool RequiresAuthenticationOnConnect => true;

        private ShellStream? _shellStream;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private string _userName; // We intentionally don't hold the password in this class, as it is only used for initial connection creation

        public SshProtocol() { }

        public Task ConnectAsync(string host, ushort port, CancellationToken cancellationToken) => throw new NotSupportedException($"{nameof(SshProtocol)} needs credentials to establish a connection.");
        public Task ConnectAsync(string host, ushort port, string userName, string password, CancellationToken cancellationToken)
        {
            Client = new SshClient(host, port, userName, password);
            Client.Connect();

            _userName = userName;
            _shellStream = Client.CreateShellStream("", 0, 0, 0, 0, 4096);
            _reader = new StreamReader(_shellStream, Encoding.UTF8, true, 1024, true);
            _writer = new StreamWriter(_shellStream) { NewLine = "\n", AutoFlush = true };

            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken) => _writer.FlushAsync();

        public async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            string? line;
            do
            {
                line = await _reader.ReadLineAsync();
                // Somehow, we need to ignore lines starting with our user name? (see GH#60)
            } while (line == null || line.StartsWith(_userName));
            return line;
        }

        public Task WriteLineAsync(string line, CancellationToken cancellationToken) => _writer.WriteAsync(line);

        #region IDisposable support

        public void Dispose()
        {
            _shellStream?.Dispose();
            _reader?.Dispose();
            _writer?.Dispose();
            Client?.Dispose();
        }

        #endregion
    }
}
