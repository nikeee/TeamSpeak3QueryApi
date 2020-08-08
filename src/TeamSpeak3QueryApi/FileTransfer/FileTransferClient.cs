using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TeamSpeak3QueryApi.Net.FileTransfer
{
    internal class FileTransferClient
    {
        private readonly string _host;
        private static int _currentFileTransferId = 1;

        public FileTransferClient(string hostName)
        {
            _host = hostName;
        }

        public int GetFileTransferId()
        {
            return _currentFileTransferId++;
        }

        public async Task SendFileAsync(byte[] data, int port, string key)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, port).ConfigureAwait(false);
            using var ns = client.GetStream();

            // SendAsync key
            var keyBytes = Encoding.ASCII.GetBytes(key);
            await ns.WriteAsync(keyBytes, 0, keyBytes.Length).ConfigureAwait(false);
            await ns.FlushAsync().ConfigureAwait(false);

            // SendAsync data
            await ns.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
        }

        public async Task SendFileAsync(Stream dataStream, int port, string key)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, port).ConfigureAwait(false);
            using var ns = client.GetStream();

            // SendAsync key
            var keyBytes = Encoding.ASCII.GetBytes(key);
            await ns.WriteAsync(keyBytes, 0, keyBytes.Length).ConfigureAwait(false);
            await ns.FlushAsync().ConfigureAwait(false);

            // SendAsync data
            await dataStream.CopyToAsync(ns).ConfigureAwait(false);
        }

        public async Task<Stream> ReceiveFileAsync(int size, int port, string key)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, port).ConfigureAwait(false);
            using var ns = client.GetStream();

            // SendAsync key
            var keyBytes = Encoding.ASCII.GetBytes(key);
            await ns.WriteAsync(keyBytes, 0, keyBytes.Length).ConfigureAwait(false);
            await ns.FlushAsync().ConfigureAwait(false);

            // Receive data
            var result = new MemoryStream(size);
            await ns.CopyToAsync(result).ConfigureAwait(false);
            result.Position = 0;

            return result;
        }
    }
}
