using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TeamSpeak3QueryApi.Net
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

        public async Task SendFile(byte[] data, int port, string key)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, port).ConfigureAwait(false);
            var ns = client.GetStream();

            // Send key
            var keyBytes = Encoding.ASCII.GetBytes(key);
            await ns.WriteAsync(keyBytes, 0, keyBytes.Length).ConfigureAwait(false);
            ns.Flush();

            // Send data
            await ns.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
        }

        public async Task SendFile(Stream dataStream, long size, int port, string key)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, port).ConfigureAwait(false);
            var ns = client.GetStream();

            // Send key
            var keyBytes = Encoding.ASCII.GetBytes(key);
            ns.Write(keyBytes, 0, keyBytes.Length);
            ns.Flush();

            // Send data
            var bytesRemaining = size;
            var buffer = new byte[4096];
            do
            {
                // Read from source. Cast is safe because buffer.Length is Int32.
                var bytesRead = await dataStream.ReadAsync(buffer, 0, (int) Math.Min(buffer.Length, bytesRemaining)).ConfigureAwait(false);

                // Write into network stream
                await ns.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);

                bytesRemaining -= bytesRead;
            } while (bytesRemaining > 0);
        }

        public async Task<byte[]> ReceiveFile(int size, int port, string key)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, port).ConfigureAwait(false);
            var ns = client.GetStream();

            // Send key
            var keyBytes = Encoding.ASCII.GetBytes(key);
            ns.Write(keyBytes, 0, keyBytes.Length);
            ns.Flush();

            // Receive data
            var result = new byte[size];
            var bytesRead = 0;
            do
            {
                bytesRead += await ns.ReadAsync(result, bytesRead, size - bytesRead).ConfigureAwait(false);
            }
            while (bytesRead < size);

            return result;
        }
    }
}
