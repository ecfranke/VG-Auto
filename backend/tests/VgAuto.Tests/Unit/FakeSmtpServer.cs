using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VgAuto.Tests.Unit
{
    /// <summary>Minimal plain text SMTP server (no TLS) that records received messages.</summary>
    public sealed class FakeSmtpServer : IDisposable
    {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource cts = new();
        public ConcurrentQueue<(string From, string To, string Data, string Auth)> Received { get; } = new();
        public int Port { get; }

        public FakeSmtpServer()
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            _ = Task.Run(AcceptLoop);
        }

        private async Task AcceptLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await listener.AcceptTcpClientAsync(cts.Token); }
                catch { return; }
                _ = Task.Run(() => Handle(client));
            }
        }

        private async Task Handle(TcpClient client)
        {
            using var _ = client;
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.ASCII);
            var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
            await writer.WriteLineAsync("220 fake ESMTP");
            string from = null, to = null, auth = null;
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) return;
                var cmd = line.ToUpperInvariant();
                if (cmd.StartsWith("EHLO"))
                {
                    await writer.WriteLineAsync("250-fake");
                    await writer.WriteLineAsync("250-AUTH PLAIN");
                    await writer.WriteLineAsync("250 8BITMIME");
                }
                else if (cmd.StartsWith("HELO")) await writer.WriteLineAsync("250 fake");
                else if (cmd.StartsWith("AUTH PLAIN"))
                {
                    var payload = line.Length > 11 ? line[11..] : null;
                    if (payload == null) { await writer.WriteLineAsync("334 "); payload = await reader.ReadLineAsync(); }
                    auth = Encoding.UTF8.GetString(Convert.FromBase64String(payload)).Replace('\0', '|');
                    await writer.WriteLineAsync("235 ok");
                }
                else if (cmd.StartsWith("MAIL FROM")) { from = line[10..].Trim(); await writer.WriteLineAsync("250 ok"); }
                else if (cmd.StartsWith("RCPT TO")) { to = line[8..].Trim(); await writer.WriteLineAsync("250 ok"); }
                else if (cmd == "DATA")
                {
                    await writer.WriteLineAsync("354 go");
                    var data = new StringBuilder();
                    string l;
                    while ((l = await reader.ReadLineAsync()) != null && l != ".") data.AppendLine(l);
                    Received.Enqueue((from, to, data.ToString(), auth));
                    await writer.WriteLineAsync("250 queued");
                }
                else if (cmd == "QUIT") { await writer.WriteLineAsync("221 bye"); return; }
                else await writer.WriteLineAsync("250 ok");
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            listener.Stop();
        }
    }
}
