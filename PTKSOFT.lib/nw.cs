using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace PTKSOFT.Lib
{
    public class nw
    {
        public static string[] get_network_interface_name() {	/*{{{
			*/
            NetworkInterface[] allNIC = NetworkInterface.GetAllNetworkInterfaces();
            string[] nameNIC = new string[0];
            for (int i = 0; i < allNIC.Length; ++i)
            {
                NetworkInterface oneNIC = allNIC[i];
                if (!(oneNIC.NetworkInterfaceType == NetworkInterfaceType.Loopback))
                {
                    // Not include Loop Back Interface in list
                    Array.Resize(ref nameNIC, nameNIC.Length + 1);
                    nameNIC[nameNIC.Length - 1] = oneNIC.Name;
                }
            }
            return (nameNIC);
        }	/*}}}*/
        public static string[] get_local_ipv4_list() {	/*{{{
			*/
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            List<string> listBuffer = new List<string>();
            foreach (IPAddress ip in host.AddressList)
            {
                string[] parts = ip.ToString().Split(new string[] { "." }, StringSplitOptions.None);
                if (parts.Length != 4) continue;    // NOT IPv4
                listBuffer.Add(ip.ToString());
            }
            return (listBuffer.ToArray());
        }	/*}}}*/
        public static string[] get_local_ipv4_broadcast_list() {	/*{{{
			*/
            string[] arrIPv4 = get_local_ipv4_list();
            List<string> buffer = new List<string>();
            for (int i = 0; i < arrIPv4.Length; i++)
            {
                string[] parts = arrIPv4[i].Split(new string[] { "." }, StringSplitOptions.None);
                if (parts.Length != 4) continue;
                parts[3] = "255";
                buffer.Add(string.Join(".", parts));
            }
            return (buffer.ToArray());
        }	/*}}}*/
        public static string[] get_possible_ipv4_range_not_me_class_c() {	/*{{{
			*/
            string[] arrMyIPv4 = get_local_ipv4_list();
            List<string> arrBuff = new List<string>();
            for (int i = 0; i < arrMyIPv4.Length; i++)
            {
                string[] parts = arrMyIPv4[i].Split(new string[] { "." }, StringSplitOptions.None);
                if (parts.Length != 4) continue;    // Only IPV4
                int p3 = int.Parse(parts[3]);
                for (int p = 1; p < 255; p++)
                {
                    if (p == p3) continue;  // Ignore My IP
                    arrBuff.Add(string.Join(".", new string[] {parts[0], parts[1], parts[2], p.ToString()}));
                }
            }
            return (arrBuff.ToArray());
        }	/*}}}*/
        public static bool ping_success(string sHost) {	/*{{{
			*/
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            string data = "12345678901234567890123456789012";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            PingReply reply = pingSender.Send(sHost, 120, buffer, options);
            if (reply.Status == IPStatus.Success) return (true);
            return (false);
        }	/*}}}*/
		public static bool connect_success(string strIpAddress, int intPort, int nTimeoutMsec)
		{
			Socket socket = null;
			try
			{
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, false);

				IAsyncResult result = socket.BeginConnect(strIpAddress, intPort, null, null);
				bool success = result.AsyncWaitHandle.WaitOne(nTimeoutMsec, true);

				return socket.Connected;
			}
			catch
			{
				return false;
			}
			finally
			{
				if (null != socket)
					socket.Close();
			}
		}        
    }
}
