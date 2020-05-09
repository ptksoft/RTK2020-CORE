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
	public delegate void RemoteOpenSuccessEvent();
	public delegate void RemoteOpenFailEvent();
	public delegate void RemoteCloseEvent();
	public delegate void RemoteDataArrivalEvent(byte[] data);
	public delegate void RemoteLineArrivalEvent(string line);

	public class RemoteClientTCP
	{
		private Thread _mainThread;
		private bool _isAlive;
		private bool _isAutoReOpen = false;
		private bool _isBeginOpen = false;		
		private bool _isTerminate = false;		

		private string _hostName;
		private int _hostPort;		
		private TcpClient _hClient;
		private NetworkStream _netStream;
		private string _lastError = "";
		private byte[] _byteBuffer;
		private string _dataBuffer = "";
		private byte[] _byteWrite;
		private Queue _byteWriteQ = Queue.Synchronized(new Queue());

		public bool IsAlive { get { return (this._isAlive); } }
		public bool IsAutoReOpen { get { return (this._isAutoReOpen); } set { this._isAutoReOpen = value; } }
		public bool IsTerminate { get { return (this._isTerminate); } set { this._isTerminate = value; } }
		public string LastError { get { return (this._lastError); } }
		public event RemoteOpenSuccessEvent OnRemoteOpenSuccess = null;
		public event RemoteOpenFailEvent OnRemoteOpenFail = null;
		public event RemoteCloseEvent OnRemoteClose = null;
		public event RemoteDataArrivalEvent OnRemoteDataArrival = null;
		public event RemoteLineArrivalEvent OnRemoteLineArrival = null;

		public RemoteClientTCP(string hostName, int hostPort)
		{
			if (hostName.Length > 0) _hostName = hostName;
			if (hostPort > 0) _hostPort = hostPort;

			_mainThread = new Thread(new ThreadStart(MainThread));
			_mainThread.IsBackground = true;
			_mainThread.Name = "TcpClientThread";
			_mainThread.Start();
		}
		public RemoteClientTCP() : this("", 0) { }
		public void Open()
		{
			this._isBeginOpen = true;
		}
		public void Open(string hostName, int hostPort)
		{
			if (hostName.Length > 0) _hostName = hostName;
			if (hostPort > 0) _hostPort = hostPort;
			this._isBeginOpen = true;
		}
		public void Close()
		{
			if (this._netStream != null) this._netStream.Close();
		}
		public void Terminate()
		{
			this._isAutoReOpen = false;
			this._isTerminate = true;
			if ((this._hClient != null) && (this._hClient.Connected)) this._hClient.Close();
		}
		public bool Write(byte[] abData)
		{
			try
			{
				if (!this._netStream.CanWrite)
				{
					this._lastError = "netSTream Cannot Write";
					return (false);
				}
				this._netStream.Write(abData, 0, abData.Length);
				return (true);
			}
			catch (Exception ex)
			{
				this._lastError = ex.Message;
				return (false);
			}
		}
		public bool Write(string sData)
		{
			byte[] abData = Encoding.Default.GetBytes(sData);
			return (this.Write(abData));
		}
		public bool WriteLine(string sLine)
		{
			return (this.Write(sLine + "\r\n"));
		}
		public void qWrite(byte[] abData)
		{
			lock (this._byteWriteQ.SyncRoot)
			{
				this._byteWriteQ.Enqueue(abData);
			}
		}
		public void qWrite(string sData)
		{
			byte[] abData = Encoding.Default.GetBytes(sData);
			this.qWrite(abData);
		}
		public void qWriteLine(string sLine)
		{
			this.qWrite(sLine + "\r\n");
		}

		protected void MainThread()
		{
			string[] arrCRLF = new string[] { "\r\n" };
			string CRLF = "\r\n";
			int iRead;
			while (!_isTerminate)
			{
				// Wait OPEN	=================================================
				if (this._hClient == null || (!this._isAutoReOpen))
				{
					while ((!this._isTerminate) && (!this._isBeginOpen)) { Thread.Sleep(1); }
				}
				if (this._isTerminate) break;

				// Begin OPEN	=================================================
				this._isBeginOpen = false;
				lock (this._byteWriteQ.SyncRoot) { this._byteWriteQ.Clear(); }
				try
				{
					this._hClient = new TcpClient();
					this._hClient.Connect(this._hostName, this._hostPort);
					this._isAlive = true;
					this._netStream = this._hClient.GetStream();
					this._byteBuffer = new byte[this._hClient.ReceiveBufferSize];
					this._dataBuffer = "";
					if (this._hClient.Connected)
					{
						if (this.OnRemoteOpenSuccess != null) this.OnRemoteOpenSuccess();
					}
					else
					{
						if (this.OnRemoteOpenFail != null) this.OnRemoteOpenFail();
						continue;
					}
				}
				catch (Exception ex)
				{
					_lastError = ex.Message;
					if (this.OnRemoteOpenFail != null) this.OnRemoteOpenFail();
					continue;
				}
				if (this._isTerminate) break;

				// Loop READ & WRITE	===========================================
				while ((!this._isTerminate) && this._isAlive)
				{
					iRead = this.__Read();
					switch (iRead)
					{
						case -2:		// DisConnect or Close
						case -1:		// Error Exception
						case 0:			// No-More Data
							break;
						default:		// Have data but not sure how many
							if (this._dataBuffer.Length > 0)
							{
								if (this.OnRemoteDataArrival != null)
								{
									byte[] bData = new byte[iRead];
									Array.Copy(_byteBuffer, bData, iRead);
									this.OnRemoteDataArrival(bData);
								}
								else if (this._dataBuffer.Contains(CRLF))
								{
									string[] buff;
									while (this._dataBuffer.Contains(CRLF)) {
										buff = this._dataBuffer.Split(arrCRLF, 2, StringSplitOptions.None);
										this._dataBuffer = buff[1];
										if (this.OnRemoteLineArrival != null) this.OnRemoteLineArrival(buff[0]);
									}
								}
							}
							break;
					}
					lock (this._byteWriteQ.SyncRoot)
					{
						if (_byteWriteQ.Count > 0)
						{
							this._byteWrite = (byte[])this._byteWriteQ.Dequeue();
						}
					}
					if (this._byteWrite != null)
					{
						this.Write(this._byteWrite);
						this._byteWrite = null;
					}
					Thread.Sleep(1);
				}
				if (!this._isAlive && this.OnRemoteClose != null) this.OnRemoteClose();
				if (this._isTerminate) break;
			}
		}
		protected int __Read()
		{	/*{{{
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
	}

}
