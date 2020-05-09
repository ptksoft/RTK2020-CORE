using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace PTKSOFT.Lib
{
    public class PacketUdp
    {
        /* Properties */
        public byte[] dataByteArray { get; set; }
        public IPEndPoint remoteEndPoint { get; set; }

        /* Properties - Read ONLY! */
        public string ipAddress { get { return (remoteEndPoint.Address.ToString()); } }
        public int port { get { return (remoteEndPoint.Port); } }

        /* Constructor */
        public PacketUdp(byte[] bufferByteArray, IPEndPoint remoteEndPoint) {	/*{{{
			*/
            this.dataByteArray = bufferByteArray;
            this.remoteEndPoint = remoteEndPoint;
        }	/*}}}*/
        public PacketUdp(byte[] bufferByteArray, string remoteIP, int remotePort) :
            this(bufferByteArray, new IPEndPoint(IPAddress.Parse(remoteIP), remotePort)) {}
        public PacketUdp(string strData, IPEndPoint remoteEndPoint) :
            this(Encoding.Default.GetBytes(strData), remoteEndPoint) { }
        public PacketUdp(string strData, string remoteIP, int remotePort) :
            this(Encoding.Default.GetBytes(strData), new IPEndPoint(IPAddress.Parse(remoteIP), remotePort)) { }
    }

    public delegate void WhenPacketUdpArrival(PacketUdp packet);
    public class UdpServer
    {
        /* Private Zone */
        private bool _isReady = false;        
        private string _lastError = "";
        private bool isTerminate = false;
        private IPEndPoint localEndPoint;
        private UdpClient udpReceive;
        private UdpClient udpSend;
        private Thread thrReceiveUDP;
        private Thread thrProcessUDP;
        private Thread thrSendUDP;
        private Queue queProcessUDP = Queue.Synchronized(new Queue());
        private Queue queSendUDP = Queue.Synchronized(new Queue());
        private ManualResetEvent trickProcessPacket = new ManualResetEvent(false);
        private ManualResetEvent trickSendPacket = new ManualResetEvent(false);
        private Form frmInvoke = null;

        /* Properties Zone */
        public bool isReady { get { return (_isReady); } }
        public string lastError { get { return (_lastError); } }

        /* Event Zone */
        public event WhenPacketUdpArrival OnPacketUdpArrival = null;

        /* Contructor Zone */
        public UdpServer(string ipLocal, int portLocal, Form formInvoke) {	/*{{{
			*/
            this.frmInvoke = formInvoke;

            // Check and create EndPoint
            try
            {
                if (ipLocal.Trim().Length.Equals(0))
                    localEndPoint = new IPEndPoint(IPAddress.Any, portLocal);
                else
                    localEndPoint = new IPEndPoint(IPAddress.Parse(ipLocal), portLocal);
            }
            catch (Exception ex) { _lastError = ex.Message; return; }

            // Create Receive UDP
            try
            {
                this.udpReceive = new UdpClient();
                this.udpReceive.Client.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true
                );
                this.udpReceive.DontFragment = true;
                this.udpReceive.EnableBroadcast = true;
                this.udpReceive.Client.Bind(localEndPoint);
            }
            catch (Exception ex) { _lastError = ex.Message; return; }

            // Create Send UDP
            try
            {
                this.udpSend = new UdpClient();
                this.udpSend.Client.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true
                );
                this.udpSend.DontFragment = true;
                this.udpSend.EnableBroadcast = true;
                this.udpSend.Client.Bind(localEndPoint);
            }
            catch (Exception ex) { _lastError = ex.Message; return; }

            // Start Thread Receive
            this.thrReceiveUDP = new Thread(new ThreadStart(thread_receive_udp));
            this.thrReceiveUDP.Priority = ThreadPriority.AboveNormal;
            this.thrReceiveUDP.IsBackground = true;
            this.thrReceiveUDP.Start();
            // Start Thread Process
            this.thrProcessUDP = new Thread(new ThreadStart(thread_process_udp));
            this.thrProcessUDP.Priority = ThreadPriority.AboveNormal;
            this.thrProcessUDP.IsBackground = true;
            this.thrProcessUDP.Start();
            // Start Thread Send
            this.thrSendUDP = new Thread(new ThreadStart(thread_send_udp));
            this.thrSendUDP.Priority = ThreadPriority.AboveNormal;
            this.thrSendUDP.IsBackground = true;
            this.thrSendUDP.Start();

            // Everything OK
            _isReady = true;
        }	/*}}}*/
        public UdpServer(string ipLocal, int portLocal) : this(ipLocal, portLocal, null) { }
        public UdpServer(int portLocal, Form formInvoke) : this("", portLocal, formInvoke) { }
        public UdpServer(int portLocal) : this("", portLocal, null) { }

        /* Public Method */
        public void send(PacketUdp packet) {	/*{{{
			*/
            lock (this.queSendUDP.SyncRoot) { this.queSendUDP.Enqueue(packet); }
            this.trickSendPacket.Set();
        }	/*}}}*/
        public void terminate() {	/*{{{
			*/
            this.isTerminate = true;

            /* Close All Socket */
            this.udpReceive.Close();
            this.udpSend.Close();

            /* Wait Thread Terminate */
            this.thrSendUDP.Join();
            this.thrProcessUDP.Join();
            this.thrReceiveUDP.Join();
        }	/*}}}*/

        /* Internal Thread */
        private void thread_receive_udp() {	/*{{{
			*/
            while (!this.isTerminate)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);                    
                    byte[] buffer = udpReceive.Receive(ref remoteEP);
                    if (buffer.Length > 0)
                    {
                        lock (queProcessUDP.SyncRoot) { queProcessUDP.Enqueue(new PacketUdp(buffer, remoteEP)); }
                        trickProcessPacket.Set();
                    }
                }
                catch (Exception ex) { _lastError = ex.Message; }

                Thread.Sleep(0);
            }
        }	/*}}}*/
        private void thread_process_udp() {	/*{{{
			*/
            int countPacket = 0;
            PacketUdp packetUDP;
            while (!this.isTerminate)
            {
                Thread.Sleep(0);
                lock (this.queProcessUDP.SyncRoot) { countPacket = this.queProcessUDP.Count; }
                if (countPacket < 1)
                {
                    this.trickProcessPacket.WaitOne(100, true);
                    this.trickProcessPacket.Reset();
                    continue;
                }
                
                lock (this.queProcessUDP.SyncRoot) { packetUDP = (PacketUdp)this.queProcessUDP.Dequeue(); }                
                
                // No EVENT handle
                if (this.OnPacketUdpArrival == null) continue;
                
                // Call related EVENT handle with parameter
                if (this.frmInvoke == null)
                    this.OnPacketUdpArrival(packetUDP);
                else
                    this.frmInvoke.Invoke(OnPacketUdpArrival, packetUDP);
            }
        }	/*}}}*/
        private void thread_send_udp() {	/*{{{
			*/
            int countPacket = 0;
            PacketUdp packet;

            while (!this.isTerminate)
            {
                Thread.Sleep(0);
                lock (this.queSendUDP.SyncRoot) { countPacket = this.queSendUDP.Count; }
                if (countPacket < 1)
                {
                    this.trickSendPacket.WaitOne(100, true);
                    continue;
                }

                lock (this.queSendUDP.SyncRoot) { packet = (PacketUdp)this.queSendUDP.Dequeue(); }
                try
                {
                    this.udpSend.Send(packet.dataByteArray, packet.dataByteArray.Length, packet.remoteEndPoint);
                }
                catch (Exception ex) { _lastError = ex.Message; }
            }
        }	/*}}}*/
    }
}
