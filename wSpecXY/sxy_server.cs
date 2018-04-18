using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace wSpecXY
{
    class sxy_server
    {
        private const int PACKET_SIZE       = 512;      // byte
        private const int RECV_IDLE_TIMEOUT = 500;      // idle regard as no data

        private static int[] rate_table = { 16*1024*2+16384*2, 16*1024, 8*1024*2, 8*1024 };

        private static UdpClient raw_udp_server;
        private static IPEndPoint raw_udp_endpoint = new IPEndPoint(IPAddress.Any, 0);

        private static int send_rate;

        private static void log(LOG_TYPE log_type, string text)
        {
            logger.log(log_type, "[serv] " + text);
        }

        private static void log(string text)
        {
            log(LOG_TYPE.LOG_INFO, text);
        }

        public static void set_rate(int rate_index)
        {
            send_rate = rate_table[rate_index];

            return ;
        }

        public static void send_raw(byte[] data)
        {
            int i_data;
            byte[] pkt_data = new byte[PACKET_SIZE];

            int send_interval = 1000/(send_rate/PACKET_SIZE);

            if (raw_udp_endpoint.Port == 0)
            {
                return ;
            }

            for (i_data = 0; i_data < (data.Length - PACKET_SIZE); i_data += PACKET_SIZE)
            {
                Buffer.BlockCopy(data, i_data, pkt_data, 0, PACKET_SIZE);
                raw_udp_server.Send(pkt_data, PACKET_SIZE, raw_udp_endpoint);

                Thread.Sleep(send_interval);
            }

            return ;
        }

        public static byte[] receive_raw()
        {
            byte[] data = new byte[0];
            byte[] pkt_data = new byte[0];

            Stopwatch idle_pkt_watch = Stopwatch.StartNew();

            while (true)
            {
                try
                {
                    pkt_data = raw_udp_server.Receive(ref raw_udp_endpoint);
                }
                catch (Exception ex)
                {

                }

                if (pkt_data.Length > 0)
                {
                    Array.Resize(ref data, data.Length + pkt_data.Length);
                    Buffer.BlockCopy(pkt_data, 0, data, data.Length - pkt_data.Length, pkt_data.Length);

                    pkt_data = null;
                    pkt_data = new byte[0];
                    idle_pkt_watch.Restart();
                }
                else if (idle_pkt_watch.ElapsedMilliseconds > RECV_IDLE_TIMEOUT)
                {
                    break;
                }
            }

            return data;
        }

        public sxy_server(int port)
        {
            /* raw udp server */
            raw_udp_server = new UdpClient(port);
            raw_udp_server.EnableBroadcast = true;
            raw_udp_server.Client.ReceiveTimeout = 50;

            log(LOG_TYPE.LOG_WARN, "Raw UDP Server created on port " + port);

            //byte[] t = new byte[0];
            //raw_udp_server.Send(t, 0, new IPEndPoint(IPAddress.Broadcast, 3442));

            raw_udp_endpoint = new IPEndPoint(IPAddress.Any, 0);
        }
    }
}
