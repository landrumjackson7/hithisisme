using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ExitClone.Net
{
    /// <summary>
    /// Minimal SOCKS5 wire helpers shared by the local listener and the upstream relay client.
    /// </summary>
    public static class Socks5
    {
        public const byte Version = 0x05;
        public const byte AuthNone = 0x00;
        public const byte CmdConnect = 0x01;
        public const byte CmdUdpAssociate = 0x03;
        public const byte AddrIpv4 = 0x01;
        public const byte AddrDomain = 0x03;
        public const byte AddrIpv6 = 0x04;
        public const byte ReplySucceeded = 0x00;
        public const byte ReplyGeneralFailure = 0x01;
        public const byte ReplyCommandNotSupported = 0x07;

        public static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int count)
        {
            int read = 0;
            while (read < count)
            {
                int n = await stream.ReadAsync(buffer, read, count - read).ConfigureAwait(false);
                if (n <= 0) throw new IOExceptionShim("SOCKS peer closed the connection");
                read += n;
            }
        }

        public static byte[] BuildAddress(string host, int port)
        {
            IPAddress ip;
            byte[] head;
            if (IPAddress.TryParse(host, out ip))
            {
                var raw = ip.GetAddressBytes();
                head = new byte[1 + raw.Length];
                head[0] = raw.Length == 4 ? AddrIpv4 : AddrIpv6;
                Buffer.BlockCopy(raw, 0, head, 1, raw.Length);
            }
            else
            {
                var name = Encoding.ASCII.GetBytes(host ?? "");
                head = new byte[2 + name.Length];
                head[0] = AddrDomain;
                head[1] = (byte)name.Length;
                Buffer.BlockCopy(name, 0, head, 2, name.Length);
            }

            var result = new byte[head.Length + 2];
            Buffer.BlockCopy(head, 0, result, 0, head.Length);
            result[head.Length] = (byte)(port >> 8);
            result[head.Length + 1] = (byte)(port & 0xFF);
            return result;
        }

        public static async Task<Tuple<string, int>> ReadAddressAsync(NetworkStream stream)
        {
            var one = new byte[1];
            await ReadExactAsync(stream, one, 1).ConfigureAwait(false);
            string host;
            switch (one[0])
            {
                case AddrIpv4:
                {
                    var raw = new byte[4];
                    await ReadExactAsync(stream, raw, 4).ConfigureAwait(false);
                    host = new IPAddress(raw).ToString();
                    break;
                }
                case AddrIpv6:
                {
                    var raw = new byte[16];
                    await ReadExactAsync(stream, raw, 16).ConfigureAwait(false);
                    host = new IPAddress(raw).ToString();
                    break;
                }
                case AddrDomain:
                {
                    await ReadExactAsync(stream, one, 1).ConfigureAwait(false);
                    var raw = new byte[one[0]];
                    await ReadExactAsync(stream, raw, raw.Length).ConfigureAwait(false);
                    host = Encoding.ASCII.GetString(raw);
                    break;
                }
                default:
                    throw new IOExceptionShim("Unsupported SOCKS address type");
            }

            var portBytes = new byte[2];
            await ReadExactAsync(stream, portBytes, 2).ConfigureAwait(false);
            return Tuple.Create(host, (portBytes[0] << 8) | portBytes[1]);
        }

        /// <summary>Parses a UDP relay datagram: RSV(2) FRAG(1) ADDR PORT PAYLOAD.</summary>
        public static bool TryParseUdpDatagram(byte[] data, int length, out string host, out int port, out int payloadOffset)
        {
            host = null;
            port = 0;
            payloadOffset = 0;
            if (length < 7 || data[2] != 0) return false;

            int i = 3;
            switch (data[i++])
            {
                case AddrIpv4:
                    if (length < i + 6) return false;
                    host = new IPAddress(new[] { data[i], data[i + 1], data[i + 2], data[i + 3] }).ToString();
                    i += 4;
                    break;
                case AddrIpv6:
                    if (length < i + 18) return false;
                    var raw = new byte[16];
                    Buffer.BlockCopy(data, i, raw, 0, 16);
                    host = new IPAddress(raw).ToString();
                    i += 16;
                    break;
                case AddrDomain:
                    int len = data[i++];
                    if (length < i + len + 2) return false;
                    host = Encoding.ASCII.GetString(data, i, len);
                    i += len;
                    break;
                default:
                    return false;
            }

            port = (data[i] << 8) | data[i + 1];
            payloadOffset = i + 2;
            return true;
        }

        public static byte[] BuildUdpDatagram(string host, int port, byte[] payload, int offset, int count)
        {
            var addr = BuildAddress(host, port);
            var datagram = new byte[3 + addr.Length + count];
            datagram[0] = 0;
            datagram[1] = 0;
            datagram[2] = 0;
            Buffer.BlockCopy(addr, 0, datagram, 3, addr.Length);
            Buffer.BlockCopy(payload, offset, datagram, 3 + addr.Length, count);
            return datagram;
        }
    }

    public class IOExceptionShim : Exception
    {
        public IOExceptionShim(string message) : base(message) { }
    }
}
