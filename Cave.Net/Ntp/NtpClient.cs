using System;
using System.Net;
using System.Net.Sockets;
using Cave.IO;

namespace Cave.Net.Ntp
{
    /// <summary>
    /// Provides functions to retrieve ntp timestamps.
    /// </summary>
    public class NtpClient
    {
        /// <summary>
        /// Sends a simple ntp query and returns the answer. The default timeout is 1s.
        /// </summary>
        /// <param name="server">Server to send the request to.</param>
        /// <param name="timeoutMilliseconds">Time in milliseconds to wait until a <see cref="TimeoutException"/> occurs.</param>
        /// <exception cref="TimeoutException">Thrown if a timeout occurs while waiting for an answer.</exception>
        /// <returns>Returns the first received answer.</returns>
        public static NtpAnswer Query(string server, int timeoutMilliseconds = 0)
        {
            if (timeoutMilliseconds == 0)
            {
                timeoutMilliseconds = 1000;
            }

            using var client = new UdpClient();
            var ep = new IPEndPoint(0, 123);
            client.Client.SendTimeout = timeoutMilliseconds;
            client.Client.ReceiveTimeout = timeoutMilliseconds;
            client.Connect(server, 123);

            var packet = new NtpPacket()
            {
                // Set version number to 4 and Mode to 3
                Settings = 0x1B,
                TransmitTimestamp = DateTime.UtcNow,
            };
            var dataSnd = MarshalStruct.GetBytes(packet);
            client.Send(dataSnd, dataSnd.Length);
            var dataRvd = client.Receive(ref ep);

            var answer = MarshalStruct.GetStruct<NtpPacket>(dataRvd);
            return new NtpAnswer(answer);
        }
    }
}
