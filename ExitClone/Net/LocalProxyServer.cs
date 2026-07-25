using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ExitClone.Net
{
    public class ProxyLogEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    /// <summary>
    /// The data plane. Games (or the launcher, via the system proxy / a per-title profile) point
    /// at this local SOCKS5 endpoint; traffic is then pushed through the selected relay chains.
    /// TCP flows race every chain and keep the winner, UDP flows are duplicated across all of
    /// them with de-duplication on the way back.
    /// </summary>
    public class LocalProxyServer : IDisposable
    {
        private readonly List<IList<RelayEndpoint>> _chains = new List<IList<RelayEndpoint>>();
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private bool _disposed;

        public int Port { get; private set; }
        public bool PacketDuplication { get; set; } = true;
        public bool Running => _listener != null;

        public long BytesSent;
        public long BytesReceived;
        public long PacketsDuplicated;
        public long PacketsDeduplicated;
        public int ActiveConnections;

        public event EventHandler<ProxyLogEventArgs> Log;

        public void SetChains(IEnumerable<IList<RelayEndpoint>> chains)
        {
            lock (_chains)
            {
                _chains.Clear();
                foreach (var chain in chains) _chains.Add(chain);
                if (_chains.Count == 0) _chains.Add(new List<RelayEndpoint>());
            }
        }

        private List<IList<RelayEndpoint>> Chains
        {
            get { lock (_chains) return _chains.ToList(); }
        }

        public void Start(int port)
        {
            Stop();
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            BytesSent = BytesReceived = PacketsDuplicated = PacketsDeduplicated = 0;
            Emit("Local SOCKS5 endpoint listening on 127.0.0.1:" + Port);
            Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            try { _cts?.Cancel(); } catch (Exception) { }
            try { _listener?.Stop(); } catch (Exception) { }
            _listener = null;
            ActiveConnections = 0;
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            var listener = _listener;
            while (!token.IsCancellationRequested && listener != null)
            {
                TcpClient client;
                try { client = await listener.AcceptTcpClientAsync().ConfigureAwait(false); }
                catch (Exception) { break; }

                var _ = Task.Run(async () =>
                {
                    Interlocked.Increment(ref ActiveConnections);
                    try { await HandleClientAsync(client, token).ConfigureAwait(false); }
                    catch (Exception ex) { Emit("Session ended: " + ex.Message); }
                    finally
                    {
                        Interlocked.Decrement(ref ActiveConnections);
                        try { client.Close(); } catch (Exception) { }
                    }
                });
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            client.NoDelay = true;
            var stream = client.GetStream();

            var head = new byte[2];
            await Socks5.ReadExactAsync(stream, head, 2).ConfigureAwait(false);
            if (head[0] != Socks5.Version) throw new IOExceptionShim("Not a SOCKS5 client");
            var methods = new byte[head[1]];
            await Socks5.ReadExactAsync(stream, methods, methods.Length).ConfigureAwait(false);
            await stream.WriteAsync(new byte[] { Socks5.Version, Socks5.AuthNone }, 0, 2).ConfigureAwait(false);

            var request = new byte[3];
            await Socks5.ReadExactAsync(stream, request, 3).ConfigureAwait(false);
            var target = await Socks5.ReadAddressAsync(stream).ConfigureAwait(false);

            switch (request[1])
            {
                case Socks5.CmdConnect:
                    await HandleConnectAsync(stream, target.Item1, target.Item2, token).ConfigureAwait(false);
                    break;
                case Socks5.CmdUdpAssociate:
                    await HandleUdpAssociateAsync(client, stream, token).ConfigureAwait(false);
                    break;
                default:
                    await ReplyAsync(stream, Socks5.ReplyCommandNotSupported, IPAddress.Loopback, 0).ConfigureAwait(false);
                    break;
            }
        }

        private async Task HandleConnectAsync(NetworkStream client, string host, int port, CancellationToken token)
        {
            TcpClient upstream;
            IList<RelayEndpoint> winner;
            try
            {
                var result = await UpstreamDialer.RaceAsync(Chains, host, port, token).ConfigureAwait(false);
                upstream = result.Item1;
                winner = result.Item2;
            }
            catch (Exception ex)
            {
                Emit("TCP " + host + ":" + port + " failed: " + ex.Message);
                await ReplyAsync(client, Socks5.ReplyGeneralFailure, IPAddress.Loopback, 0).ConfigureAwait(false);
                return;
            }

            Emit("TCP " + host + ":" + port + " via " + Describe(winner));
            await ReplyAsync(client, Socks5.ReplySucceeded, IPAddress.Loopback, Port).ConfigureAwait(false);

            using (upstream)
            {
                var remote = upstream.GetStream();
                var up = PumpAsync(client, remote, true, token);
                var down = PumpAsync(remote, client, false, token);
                await Task.WhenAny(up, down).ConfigureAwait(false);
            }
        }

        private async Task PumpAsync(NetworkStream from, NetworkStream to, bool outbound, CancellationToken token)
        {
            var buffer = new byte[16 * 1024];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int n = await from.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    if (n <= 0) break;
                    await to.WriteAsync(buffer, 0, n, token).ConfigureAwait(false);
                    if (outbound) Interlocked.Add(ref BytesSent, n);
                    else Interlocked.Add(ref BytesReceived, n);
                }
            }
            catch (Exception)
            {
                // Either side closing ends the flow; the caller tears the pair down.
            }
        }

        private async Task HandleUdpAssociateAsync(TcpClient control, NetworkStream stream, CancellationToken token)
        {
            using (var local = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            {
                var bound = (IPEndPoint)local.Client.LocalEndPoint;
                await ReplyAsync(stream, Socks5.ReplySucceeded, IPAddress.Loopback, bound.Port).ConfigureAwait(false);
                Emit("UDP associate on 127.0.0.1:" + bound.Port + " (" + Chains.Count + " route(s))");

                var flow = new UdpFlow(this, local, Chains, token);
                await flow.RunAsync(control).ConfigureAwait(false);
            }
        }

        private static string Describe(IList<RelayEndpoint> chain)
        {
            return chain == null || chain.Count == 0 ? "direct" : string.Join(" -> ", chain.Select(c => c.Label));
        }

        private static async Task ReplyAsync(NetworkStream stream, byte code, IPAddress address, int port)
        {
            var addr = Socks5.BuildAddress(address.ToString(), port);
            var reply = new byte[3 + addr.Length];
            reply[0] = Socks5.Version;
            reply[1] = code;
            reply[2] = 0;
            Buffer.BlockCopy(addr, 0, reply, 3, addr.Length);
            await stream.WriteAsync(reply, 0, reply.Length).ConfigureAwait(false);
        }

        internal void Emit(string message) => Log?.Invoke(this, new ProxyLogEventArgs { Message = message });

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _cts?.Dispose();
        }

        /// <summary>
        /// A single game UDP flow fanned out over every selected route.
        /// </summary>
        private class UdpFlow
        {
            private readonly LocalProxyServer _owner;
            private readonly UdpClient _local;
            private readonly List<IList<RelayEndpoint>> _chains;
            private readonly CancellationToken _token;
            private readonly PacketDeduplicator _dedup = new PacketDeduplicator();
            private readonly List<UdpUpstream> _upstreams = new List<UdpUpstream>();
            private IPEndPoint _client;

            public UdpFlow(LocalProxyServer owner, UdpClient local, List<IList<RelayEndpoint>> chains, CancellationToken token)
            {
                _owner = owner;
                _local = local;
                _chains = chains;
                _token = token;
            }

            public async Task RunAsync(TcpClient control)
            {
                foreach (var chain in _owner.PacketDuplication ? _chains : _chains.Take(1).ToList())
                {
                    var exit = chain.LastOrDefault();
                    var upstream = new UdpUpstream(exit);
                    try
                    {
                        await upstream.OpenAsync(_token).ConfigureAwait(false);
                        _upstreams.Add(upstream);
                        var _ = Task.Run(() => DownstreamLoopAsync(upstream));
                    }
                    catch (Exception ex)
                    {
                        upstream.Dispose();
                        _owner.Emit("Route " + (exit?.Label ?? "direct") + " unavailable for UDP: " + ex.Message);
                    }
                }

                if (_upstreams.Count == 0)
                {
                    var fallback = new UdpUpstream(null);
                    await fallback.OpenAsync(_token).ConfigureAwait(false);
                    _upstreams.Add(fallback);
                    var _ = Task.Run(() => DownstreamLoopAsync(fallback));
                }

                var closed = WaitForControlCloseAsync(control);
                await Task.WhenAny(UpstreamLoopAsync(), closed).ConfigureAwait(false);
                foreach (var upstream in _upstreams) upstream.Dispose();
            }

            private async Task UpstreamLoopAsync()
            {
                while (!_token.IsCancellationRequested)
                {
                    UdpReceiveResult received;
                    try { received = await _local.ReceiveAsync().ConfigureAwait(false); }
                    catch (Exception) { return; }

                    _client = received.RemoteEndPoint;
                    string host;
                    int port, offset;
                    if (!Socks5.TryParseUdpDatagram(received.Buffer, received.Buffer.Length, out host, out port, out offset))
                        continue;

                    int count = received.Buffer.Length - offset;
                    bool first = true;
                    foreach (var upstream in _upstreams.ToList())
                    {
                        try
                        {
                            await upstream.SendAsync(host, port, received.Buffer, offset, count).ConfigureAwait(false);
                            Interlocked.Add(ref _owner.BytesSent, count);
                            if (!first) Interlocked.Increment(ref _owner.PacketsDuplicated);
                            first = false;
                        }
                        catch (Exception)
                        {
                            // A dead route must not stall the remaining copies.
                        }
                    }
                }
            }

            private async Task DownstreamLoopAsync(UdpUpstream upstream)
            {
                while (!_token.IsCancellationRequested)
                {
                    UdpPayload payload;
                    try { payload = await upstream.ReceiveAsync().ConfigureAwait(false); }
                    catch (Exception) { return; }
                    if (payload?.Data == null || _client == null) continue;

                    if (_upstreams.Count > 1 && _dedup.IsDuplicate(payload.Data, 0, payload.Data.Length))
                    {
                        Interlocked.Increment(ref _owner.PacketsDeduplicated);
                        continue;
                    }

                    try
                    {
                        var wrapped = Socks5.BuildUdpDatagram(payload.Host, payload.Port, payload.Data, 0, payload.Data.Length);
                        await _local.SendAsync(wrapped, wrapped.Length, _client).ConfigureAwait(false);
                        Interlocked.Add(ref _owner.BytesReceived, payload.Data.Length);
                    }
                    catch (Exception) { return; }
                }
            }

            private static async Task WaitForControlCloseAsync(TcpClient control)
            {
                var buffer = new byte[1];
                try
                {
                    var stream = control.GetStream();
                    while (await stream.ReadAsync(buffer, 0, 1).ConfigureAwait(false) > 0) { }
                }
                catch (Exception) { }
            }
        }
    }
}
