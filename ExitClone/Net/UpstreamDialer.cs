using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ExitClone.Net
{
    public class RelayEndpoint
    {
        public string Id { get; set; }
        public string Host { get; set; }
        public int Port { get; set; } = 1080;
        public string Label => Id ?? Host;
    }

    /// <summary>
    /// Opens a connection through a chain of SOCKS5 relays, ending at the real destination.
    /// An empty chain means a direct connection.
    /// </summary>
    public static class UpstreamDialer
    {
        public static int ConnectTimeoutMs = 6000;

        public static async Task<TcpClient> ConnectAsync(IList<RelayEndpoint> chain, string host, int port,
            CancellationToken token = default(CancellationToken))
        {
            if (chain == null || chain.Count == 0)
            {
                var direct = new TcpClient { NoDelay = true };
                await WithTimeout(direct.ConnectAsync(host, port), token).ConfigureAwait(false);
                return direct;
            }

            var client = new TcpClient { NoDelay = true };
            try
            {
                await WithTimeout(client.ConnectAsync(chain[0].Host, chain[0].Port), token).ConfigureAwait(false);
                var stream = client.GetStream();

                for (int i = 0; i < chain.Count; i++)
                {
                    var nextHost = i + 1 < chain.Count ? chain[i + 1].Host : host;
                    var nextPort = i + 1 < chain.Count ? chain[i + 1].Port : port;
                    await HandshakeAsync(stream, Socks5.CmdConnect, nextHost, nextPort).ConfigureAwait(false);
                }
                return client;
            }
            catch
            {
                client.Close();
                throw;
            }
        }

        /// <summary>Performs greeting + request and returns the bound address reported by the relay.</summary>
        public static async Task<Tuple<string, int>> HandshakeAsync(NetworkStream stream, byte command, string host, int port)
        {
            var greeting = new byte[] { Socks5.Version, 1, Socks5.AuthNone };
            await stream.WriteAsync(greeting, 0, greeting.Length).ConfigureAwait(false);

            var response = new byte[2];
            await Socks5.ReadExactAsync(stream, response, 2).ConfigureAwait(false);
            if (response[0] != Socks5.Version || response[1] != Socks5.AuthNone)
                throw new IOExceptionShim("Relay rejected the SOCKS5 greeting");

            var addr = Socks5.BuildAddress(host, port);
            var request = new byte[3 + addr.Length];
            request[0] = Socks5.Version;
            request[1] = command;
            request[2] = 0;
            Buffer.BlockCopy(addr, 0, request, 3, addr.Length);
            await stream.WriteAsync(request, 0, request.Length).ConfigureAwait(false);

            var head = new byte[3];
            await Socks5.ReadExactAsync(stream, head, 3).ConfigureAwait(false);
            if (head[1] != Socks5.ReplySucceeded)
                throw new IOExceptionShim("Relay refused the request (code " + head[1] + ")");
            return await Socks5.ReadAddressAsync(stream).ConfigureAwait(false);
        }

        /// <summary>
        /// Races several relay chains and keeps the first connection that completes, which is the
        /// TCP equivalent of sending a packet down every route at once.
        /// </summary>
        public static async Task<Tuple<TcpClient, IList<RelayEndpoint>>> RaceAsync(
            IEnumerable<IList<RelayEndpoint>> chains, string host, int port, CancellationToken token)
        {
            var pending = chains.Select(async chain =>
            {
                var client = await ConnectAsync(chain, host, port, token).ConfigureAwait(false);
                return Tuple.Create(client, chain);
            }).ToList();

            Exception last = null;
            while (pending.Count > 0)
            {
                var finished = await Task.WhenAny(pending).ConfigureAwait(false);
                pending.Remove(finished);
                if (finished.Status == TaskStatus.RanToCompletion)
                {
                    foreach (var loser in pending)
                        Forget(loser);
                    return finished.Result;
                }
                last = finished.Exception?.GetBaseException() ?? last;
            }
            throw last ?? new IOExceptionShim("No relay route could be established");
        }

        private static void Forget(Task<Tuple<TcpClient, IList<RelayEndpoint>>> task)
        {
            task.ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion) t.Result.Item1.Close();
                else { var _ = t.Exception; }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private static async Task WithTimeout(Task task, CancellationToken token)
        {
            var timeout = Task.Delay(ConnectTimeoutMs, token);
            if (await Task.WhenAny(task, timeout).ConfigureAwait(false) == timeout)
                throw new IOExceptionShim("Connection timed out");
            await task.ConfigureAwait(false);
        }
    }
}
