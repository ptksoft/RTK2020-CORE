using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PTKSOFT.Lib
{
    //------------------------------------------------------------------------
    /*
	 *	2020-05-09	Add ability to set nameServer for use as name of each thread 
	 *
	 */
    public class TcpServerThreadPool
    {
        // Private flage
        private bool isTerminate = false;
        private bool isStart = false;
        private ManualResetEvent trickListenFinish = new ManualResetEvent(false);
        private Queue clientRecQ = Queue.Synchronized(new Queue());
        private IPAddress ipAddress;
        private int portNum;

        // String Constant
        protected string CRLF;
        protected string[] arrCRLF;

        // Form Invoke
        protected Form frmInvoke = null;

        // Thread variables 
        private int _maxWorkerThread = 10;
        private Thread[] _workerThread;
        private Thread _listenThread;
        private Thread _counter5SecThread;

        // Stat variables
        protected long _statCountClientConnect = 0;
        protected long _statCountClientErrorAccept = 0;
        protected long _statCountClientDisconnect = 0;
        protected long _statCountClientLine = 0;
        protected decimal _statAvrClientConnect = 0M;
        protected decimal _statAvrClientErrorAccept = 0M;
        protected decimal _statAvrClientDisconnect = 0M;
        protected decimal _statAvrClientLine = 0M;

        // PUBLIC Propertie(s)
        public string nameServer = "TCP_SERVER";		// Name to be show in zLog thread Name
        public decimal statAvrClientConnect { get { return (_statAvrClientConnect); } }
        public decimal statAvrClientErrorAccept { get { return (_statAvrClientErrorAccept); } }
        public decimal statAvrClientDisconnect { get { return (_statAvrClientDisconnect); } }
        public decimal statAvrClientLine { get { return (_statAvrClientLine); } }

        // Constructor
        public TcpServerThreadPool(Form invokeForm) {	/*{{{
			*/
            this.CRLF = "\r\n";
            this.arrCRLF = new string[] { "\r\n" };
            this.frmInvoke = invokeForm;
        }	/*}}}*/
        public TcpServerThreadPool() : this(null) { }

        // Public Method(s)
        public bool start(string ipAddress, int portNum, int maxWorkingThread) {	/*{{{
			*/
            this._maxWorkerThread = maxWorkingThread;
            return (this.start(ipAddress, portNum));
        }	/*}}}*/
        public bool start(string ipAddress, int portNum) {	/*{{{
			*/
            if (this.isStart) return (true);
            try
            {
                this.ipAddress = IPAddress.Parse(ipAddress);
                this.portNum = portNum;
                this._listenThread = new Thread(new ThreadStart(this.thread_listen));
                this._listenThread.Name = this.nameServer + "-Listener";
                this._listenThread.IsBackground = true;
                this._listenThread.Start();
                this.trickListenFinish.WaitOne(5000, true);
                this.trickListenFinish.Reset();

                if (this.isStart)
                {   
                    // Start STAT Counter Thread
                    this._counter5SecThread = new Thread(new ThreadStart(this.thread_counter5Sec));
                    this._counter5SecThread.Name = this.nameServer + "-Counter5Sec";
                    this._counter5SecThread.IsBackground = true;
                    this._counter5SecThread.Start();
                    
                    // Start Listen finish? then start Thread ReadWrite
                    this._workerThread = new Thread[this._maxWorkerThread];
                    for (int i = 0; i < this._maxWorkerThread; i++)
                    {
                        this._workerThread[i] = new Thread(new ThreadStart(this.thread_pooling));
                        this._workerThread[i].Name = this.nameServer + "-Worker_" + i.ToString();
                        this._workerThread[i].IsBackground = true;
                        this._workerThread[i].Start();
                    }
                }

                return (this.isStart);
            }
            catch
            {
                return (false);
            }
        }	/*}}}*/
        public void stop() {	/*{{{
			*/
            this.isTerminate = true;

            for (int i = 0; i < this._maxWorkerThread; i++)
            {
                if (
                    this._workerThread[i] != null &&
                    this._workerThread[i].IsAlive
                    )
                {
                    this._workerThread[i].Join();
                }
            }

            ClientTcp clientRec;
            while (true)
            {
                lock (this.clientRecQ.SyncRoot)
                {
                    if (this.clientRecQ.Count < 1) break;
                    clientRec = (ClientTcp)this.clientRecQ.Dequeue();
                }
                if (clientRec != null)
                {
                    try
                    {
                        clientRec.netStream.Close();
                        clientRec.handleClient.Close();
                    }
                    catch { }
                }
            }

        }	/*}}}*/

        // Private Thread(s)
        private void thread_listen() {	/*{{{
			*/
            TcpListener listener = new TcpListener(this.ipAddress, this.portNum);
            try
            {
                listener.Start();
                isStart = true;
                this.trickListenFinish.Set();

                // Cycle Listen for Client Connection
                ulong NumClient = 0;

                while (!this.isTerminate)
                {
                    TcpClient handleClient = listener.AcceptTcpClient();
                    if (handleClient != null)
                    {
                        NumClient++;
                        ClientTcp clientRec = new ClientTcp(NumClient, handleClient);
                        this.when_client_connect_success(clientRec);
                        lock (this.clientRecQ.SyncRoot)
                        {
                            this.clientRecQ.Enqueue(clientRec);
                        }
                        Interlocked.Increment(ref this._statCountClientConnect);                        
                    }
                    else
                    {
                        Interlocked.Increment(ref this._statCountClientErrorAccept);
                        this.when_client_connect_fail();
                    }
                }
            }
            catch
            {
                isStart = false;
                this.trickListenFinish.Set();
                return;
            }
        }	/*}}}*/
        private void thread_pooling() {	/*{{{
			*/
            string[] parts = Thread.CurrentThread.Name.Split(new string[] { "_" }, StringSplitOptions.None);
            int ThisThreadID = -1;
            if (parts.Length == 3) int.TryParse(parts[2], out ThisThreadID);
            this.when_thread_pooling_init(ThisThreadID);

            ClientTcp clientRec = null;
            while (!this.isTerminate)
            {
                clientRec = null;
                lock (this.clientRecQ.SyncRoot)
                {
                    if (this.clientRecQ.Count > 0)
                        clientRec = (ClientTcp)this.clientRecQ.Dequeue();
                }
                if (clientRec == null)
                {
                    // Nothing to Process
                    Thread.Sleep(1);
                    continue;
                }
                else
                {
                    switch (clientRec.read())
                    {
                        case -2:    // Disconnecting or Close
                        case -1:    // Error Exception
                            //Console.WriteLine("Client #" + clientRec.ID.ToString() + " Disconnected or Closed");
                            break;

                        case 0:     // No-More data, then Process IT now
                            //Console.WriteLine(Thread.CurrentThread.Name + " Read 0 data");                            
                            break;

                        default:    // Have data but not sure how many
                            //Console.WriteLine(Thread.CurrentThread.Name + " Read >0 data");
                            if (clientRec.dataBuffer.Length > 0) 
                                this.when_data_arrival(clientRec, ThisThreadID);  // Raise EVENT if have DATA
                            break;
                    }

                    // Push client for Next Cycle
                    if (clientRec.isAlive)
                    {
                        lock (this.clientRecQ.SyncRoot)
                        {
                            this.clientRecQ.Enqueue(clientRec);
                        }
                    }
                    else
                    {
                        Interlocked.Increment(ref this._statCountClientDisconnect);
                        this.when_client_disconnect(clientRec);
                    }

                    Thread.Sleep(1);
                }
            }
        }	/*}}}*/
        private void thread_counter5Sec() {	/*{{{
			*/
            int iRound = 0;
            long nCC;
            long nCE;
            long nCD;
            long nCL;
            while (!this.isTerminate)
            {
                iRound++;                
                Thread.Sleep(100);

                if (iRound >= (50)) // 5000 Milisec
                {
                    iRound = 0;

                    // Copy counter from Global Stat
                    nCC = _statCountClientConnect;
                    nCE = _statCountClientErrorAccept;
                    nCD = _statCountClientDisconnect;
                    nCL = _statCountClientLine;
                    
                    // Clear counter
                    _statCountClientConnect = 0;
                    _statCountClientErrorAccept = 0;
                    _statCountClientDisconnect = 0;
                    _statCountClientLine = 0;

                    // Find average value
                    _statAvrClientConnect = nCC / 5;
                    _statAvrClientErrorAccept = nCE / 5;
                    _statAvrClientDisconnect = nCD / 5;
                    _statAvrClientLine = nCL / 5;

                    // Call other method when counter 5 sec reach
                    this.when_counter_5sec_done();
                }
            }
        }	/*}}}*/

        // Protected virtual event that can Override
        protected virtual void when_thread_pooling_init(int idxThread) {	/*{{{
			*/
            // Thread Pool is Initialize
        }	/*}}}*/
        protected virtual void when_counter_5sec_done() {	/*{{{
			*/
            // Clear Stat and do anything
        }	/*}}}*/
        protected virtual void when_client_connect_success(ClientTcp clientRec) {	/*{{{
			*/
            // Client is Connected Success
        }	/*}}}*/
        protected virtual void when_client_connect_fail() {	/*{{{
			*/
            // Connected Fail
        }	/*}}}*/
        protected virtual void when_client_disconnect(ClientTcp clientRec) {	/*{{{
			*/
            // Client Disconnected - Normal & AbNormal Case
        }	/*}}}*/
        protected virtual void when_data_arrival(ClientTcp clientRec, int idxThread) {	/*{{{
			*/
            // Byte data any length was Read-In
            clientRec.dataBuffer = "";      // Clear DataBuffer if doesnot do anything
        }	/*}}}*/
    }

    //------------------------------------------------------------------------
    public class ClientTcp
    {
        private bool _isAlive;
        private ulong _id;
        private TcpClient _hClient;
        private NetworkStream _netStream;
        private byte[] _byteBuffer;
        private string _dataBuffer = "";
        private string _remoteIP = "";
        private int _remotePort = 0;
        private string _lastError = "";
        private object _tag = null;
        private List<string> _lineList;

        public bool isAlive { get { return (this._isAlive); } }
        public ulong id { get { return (this._id); } }
        public TcpClient handleClient { get { return (this._hClient); } }
        public NetworkStream netStream { get { return (this._netStream); } }
        public byte[] byteBuffer { get { return (this._byteBuffer); } }
        public string dataBuffer { get { return (this._dataBuffer); } set { this._dataBuffer = value; } }
        public string remoteIp { get { return (this._remoteIP); } }
        public int remotePort { get { return (this._remotePort); } }
        public object tag { get { return (this._tag); } set { this._tag = value; } }
        public List<string> lineList { get { return(this._lineList); } }

        public ClientTcp(ulong iClient, TcpClient hClient) {	/*{{{
			*/
            this._isAlive = true;
            this._id = iClient;
            this._hClient = hClient;
            this._netStream = _hClient.GetStream();
            this._byteBuffer = new byte[_hClient.ReceiveBufferSize];
            this._lineList = new List<string>();

            IPEndPoint ep = (IPEndPoint)hClient.Client.RemoteEndPoint;
            this._remoteIP = ep.Address.ToString();
            this._remotePort = ep.Port;
        }	/*}}}*/
        public int read() {	/*{{{
			*/
            try
            {
                bool isClose = false;
                if (_hClient.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (_hClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        isClose = true;
                    }
                }

                if (isClose)
                {
                    // Try to Clear All
                    this._netStream.Close();
                    this._hClient.Close();
                    this._isAlive = false;
                    return (-2);
                }


                if (this._hClient.Available > 0)
                {
                    int iByteRead = this._netStream.Read(this._byteBuffer, 0, (int)this._byteBuffer.Length);
                    if (iByteRead > 0)
                    {
                        this._dataBuffer += Encoding.Default.GetString(this._byteBuffer, 0, iByteRead);
                        return (iByteRead);
                    }
                }

                // Nothing To Read
                return (0);
            }
            catch (Exception ex)
            {
                this._lastError = ex.Message;

                // Socket is Broken
                this._netStream.Close();
                this._hClient.Close();
                this._isAlive = false;
                return (-1);
            }
        }	/*}}}*/
        public bool write(string sData) {	/*{{{
			*/
            if (!this._netStream.CanWrite) return (false);
            byte[] abData = Encoding.Default.GetBytes(sData);
            try
            {
                this._netStream.Write(abData, 0, abData.Length);
                return (true);
            }
            catch (Exception ex)
            {
                this._lastError = ex.Message;
                return (false);
            }
        }	/*}}}*/
        public bool write_byte(byte[] abData) {	/*{{{
			*/
            if (!this._netStream.CanWrite) return (false);            
            try
            {
                this._netStream.Write(abData, 0, abData.Length);
                return (true);
            }
            catch (Exception ex)
            {
                this._lastError = ex.Message;
                return (false);
            }
        }	/*}}}*/
        public bool write_line(string sData) {	/*{{{
			*/
            return (this.write(sData + "\r\n"));
        }	/*}}}*/
    }

    //------------------------------------------------------------------------
    public delegate void ThreadPoolingInitEvent(int idxThread);
    public delegate void Counter5SecDoneEvent();
    public delegate void ClientConnectedSuccessEvent(ClientTcp clientRec);
    public delegate void ClientConnectedFailEvent();
    public delegate void ClientDisconnectedEvent(ClientTcp clientRec);
    public delegate void LineArrivalEvent(string sLine, ClientTcp clientRec, int idxThread);
    
    //------------------------------------------------------------------------
    public class TcpServiceLine : TcpServerThreadPool
    {
        public event ThreadPoolingInitEvent OnThreadPoolingInit = null;
        public event Counter5SecDoneEvent OnCounter5SecDone = null;
        public event ClientConnectedSuccessEvent OnClientConnectedSuccess = null;
        public event ClientConnectedFailEvent OnClientConnectedFail = null;
        public event ClientDisconnectedEvent OnClientDisconnected = null;
        public event LineArrivalEvent OnLineArrival = null;

        public TcpServiceLine(Form invokeForm) : base(invokeForm) { }
        public TcpServiceLine() : base() { }

        protected override void when_thread_pooling_init(int idxThread) {	/*{{{
			*/
            if (OnThreadPoolingInit == null) return;
            if (this.frmInvoke == null) this.OnThreadPoolingInit(idxThread);
            else this.frmInvoke.Invoke(OnThreadPoolingInit, new object[] { idxThread });
        }	/*}}}*/
        protected override void when_counter_5sec_done() {	/*{{{
			*/
            if (OnCounter5SecDone == null) return;
            if (this.frmInvoke == null) this.OnCounter5SecDone();
            else this.frmInvoke.Invoke(OnCounter5SecDone);
        }	/*}}}*/
        protected override void when_client_connect_success(ClientTcp clientRec) {	/*{{{
			*/
            if (OnClientConnectedSuccess == null) return;
            if (this.frmInvoke == null) this.OnClientConnectedSuccess(clientRec);
            else this.frmInvoke.Invoke(OnClientConnectedSuccess, new object[] { clientRec });
        }	/*}}}*/
        protected override void when_client_connect_fail() {	/*{{{
			*/
            if (OnClientConnectedFail == null) return;
            if (this.frmInvoke == null) this.OnClientConnectedFail();
            else this.frmInvoke.Invoke(OnClientConnectedFail);
        }	/*}}}*/
        protected override void when_client_disconnect(ClientTcp clientRec) {	/*{{{
			*/
            if (OnClientDisconnected == null) return;
            if (this.frmInvoke == null) this.OnClientDisconnected(clientRec);
            else this.frmInvoke.Invoke(OnClientDisconnected, new object[] { clientRec });
        }	/*}}}*/
        protected override void when_data_arrival(ClientTcp clientRec, int idxThread) {	/*{{{
			*/
            string dataBuffer = clientRec.dataBuffer;
            string curLine = "";
            while (dataBuffer.Contains(CRLF))
            {
                string[] parts = dataBuffer.Split(arrCRLF, 2, StringSplitOptions.None);
                curLine = parts[0];
                dataBuffer = parts[1];
                clientRec.dataBuffer = parts[1];
                Interlocked.Increment(ref this._statCountClientLine);

                if (OnLineArrival != null)  // Call Event if Define
                {
                    if (this.frmInvoke == null)
                        this.OnLineArrival(curLine, clientRec, idxThread);
                    else
                        this.frmInvoke.Invoke(OnLineArrival, new object[] { curLine, clientRec, idxThread });
                }
            }
        }	/*}}}*/
    }

    //------------------------------------------------------------------------
}
