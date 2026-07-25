using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ExitClone.Net
{
    public class UdpPayload
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// One outbound UDP path: either straight out of the local NIC or through a relay's
    /// SOCKS5 UDP associate. Every path used by a flow is an independent copy of the traffic.
    /// </summary>
    public class UdpUpstream : IDisposable
    {
        private readonly RelayEndpoint _relay;
        private TcpClient _control;
        private UdpClient _socket;
        private IPEndPoint _relayUdp;
        private bool _disposed;

        public string Label => _relay == null ? "direct" : _relay.Label;
        public bool IsRelayed => _relay != null;

        public UdpUpstream(RelayEndpoint relay)
        {
            _relay = relay;
        }

        public async Task OpenAsync(CancellationToken token)
        {
            _socket = new UdpClient(0);
            if (_relay == null) return;

            _control = new TcpClient { NoDelay = true };
            await _control.ConnectAsync(_relay.Host, _relay.Port).ConfigureAwait(false);
            var bound = await UpstreamDialer.HandshakeAsync(_control.GetStream(), Socks5.CmdUdpAssociate, "0.0.0.0", 0)
                .ConfigureAwait(false);

            var host = bound.Item1 == "0.0.0.0" ? _relay.Host : bound.Item1;
            var address = Core.PingEngine.ResolveFirst(host);
            if (address == null) throw new IOExceptionShim("Relay returned an unusable UDP endpoint");
            _relayUdp = new IPEndPoint(address, bound.Item2);
        }

        public async Task<int> SendAsync(string host, int port, byte[] payload, int offset, int count)
        {
            if (_disposed) return 0;
            if (_relay == null)
            {
                var target = new IPEndPoint(Core.PingEngine.ResolveFirst(host) ?? IPAddress.Loopback, port);
                var buffer = new byte[count];
                Buffer.BlockCopy(payload, offset, buffer, 0, count);
                return await _socket.SendAsync(buffer, buffer.Length, target).ConfigureAwait(false);
            }

            var wrapped = Socks5.BuildUdpDatagram(host, port, payload, offset, count);
            return await _socket.SendAsync(wrapped, wrapped.Length, _relayUdp).ConfigureAwait(false);
        }

        /// <summary>Receives one datagram and strips the relay header when present.</summary>
        public async Task<UdpPayload> ReceiveAsync()
        {
            var result = await _socket.ReceiveAsync().ConfigureAwait(false);
            var data = result.Buffer;
            if (_relay == null)
                return new UdpPayload
                {
                    Host = result.RemoteEndPoint.Address.ToString(),
                    Port = result.RemoteEndPoint.Port,
                    Data = data
                };

            string host;
            int port, offset;
            if (!Socks5.TryParseUdpDatagram(data, data.Length, out host, out port, out offset)) return null;
            var payload = new byte[data.Length - offset];
            Buffer.BlockCopy(data, offset, payload, 0, payload.Length);
            return new UdpPayload { Host = host, Port = port, Data = payload };
        }

        public void Dispose()
        {
            _disposed = true;
            try { _socket?.Close(); } catch (Exception) { }
            try { _control?.Close(); } catch (Exception) { }
        }
    }
}
