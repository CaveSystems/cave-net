using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Cave.Net
{
    /// <summary>
    /// Provides a class for sending wake on lan packets.
    /// </summary>
    public class WakeOnLan
    {
        /// <summary>
        /// Sends a magic packet to a physical address.
        /// </summary>
        /// <param name="macAddress">Physical address to send packet to.</param>
        /// <param name="secureOnPassword">Secure on password. (This is sent in clear text!).</param>
        /// <returns>Returns a dictionary with used ip address and exception.</returns>
        public static IDictionary<IPAddress, Exception> SendMagicPacket(PhysicalAddress macAddress, string secureOnPassword = null)
        {
            var result = new Dictionary<IPAddress, Exception>();
            foreach (var local in NetTools.GetLocalAddresses(OperationalStatus.Up))
            {
                var broadcast = local.GetBroadcastAddress();
                try
                {
                    SendMagicPacket(broadcast, macAddress, secureOnPassword);
                    result[broadcast] = null;
                }
                catch (Exception ex)
                {
                    result[broadcast] = ex;
                }
            }
            return result;
        }

        /// <summary>
        /// Sends a magic packet to a physical address.
        /// </summary>
        /// <param name="broadcastAddress">Target address to send to.</param>
        /// <param name="macAddress">Physical address to send packet to.</param>
        /// <param name="secureOnPassword">Secure on password. (This is sent in clear text!).</param>
        public static void SendMagicPacket(IPAddress broadcastAddress, PhysicalAddress macAddress, string secureOnPassword = null)
        {
            // build packet:
            // 6 bytes 0xff
            // 16 repetitions of the 6 byte mac address
            // followed by the optional (in)SecureOnPassword
            var size = 17 * 6;
            byte[] buffer;
            if (secureOnPassword == null)
            {
                buffer = new byte[size];
            }
            else
            {
                var passwordBytes = Encoding.ASCII.GetBytes(secureOnPassword);
                buffer = new byte[size + passwordBytes.Length];
                passwordBytes.CopyTo(buffer, size);
            }
            for (var i = 0; i < 6; i++)
            {
                buffer[i] = 0xff;
            }
            var macBytes = macAddress.GetAddressBytes();
            if (macBytes.Length != 6)
            {
                throw new ArgumentOutOfRangeException(nameof(macAddress), "Physical mac address bytes out of range (6)!");
            }

            for (var i = 6; i < size; i += 6)
            {
                macBytes.CopyTo(buffer, i);
            }

            using var udp = new UdpClient();
            udp.EnableBroadcast = true;
            foreach (var targetPort in new[] { 0, 7, 9 })
            {
                udp.Send(buffer, buffer.Length, new IPEndPoint(broadcastAddress, targetPort));
            }
        }
    }
}
