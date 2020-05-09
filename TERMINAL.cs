using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

using PTKSOFT.Lib;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MAIN
{
	public class TERMINAL
	{
		private static TcpServiceLine tcpTerminal = null;
		private static AutoResetEvent trickTcpStart = new AutoResetEvent(false);
		private static bool isTcpStartSuccess = false;
		
		public static bool InitAll () {
			zlog.debug("Begin Init TERMINAL module");
			
			zlog.debug("\tStart tcpTerminal");
			(new Thread(()=>{
				tcpTerminal = new TcpServiceLine();
				tcpTerminal.OnClientDisconnected += new ClientDisconnectedEvent(tcpTerminal_OnClientDisconnected);
				tcpTerminal.OnClientConnectedFail += new ClientConnectedFailEvent(tcpTerminal_OnClientConnectedFail);
				tcpTerminal.OnClientConnectedSuccess += new ClientConnectedSuccessEvent(tcpTerminal_OnClientConnectedSuccess);
				tcpTerminal.OnLineArrival += new LineArrivalEvent(tcpTerminal_OnLineArrival);
				tcpTerminal.nameServer = "TERMINAL";
				isTcpStartSuccess = tcpTerminal.start(
										RUN.Config[KW.TerminalListenIp], 
										RUN.Config.get_int(KW.TerminalListenPort), 
										RUN.Config.get_int(KW.TerminalMaxWorker)
										);
				trickTcpStart.Set();
				if (! isTcpStartSuccess) return;
				// begin monitoring global Signal
				while (! that.isQuit) {
					Thread.Sleep(1000);
				}
				// global signal is Fire, then stop SERVER
				tcpTerminal.stop();
			})).Start();			
			trickTcpStart.WaitOne();
			if (isTcpStartSuccess) {
				zlog.debug("\t* Success Init Terminatel Module");
				return(true);
			}
			else {
				zlog.debug("\t! FAIL Init Terminatel Module");
				return (true);			
			}
		}

		static void tcpTerminal_OnLineArrival(string sLine, ClientTcp clientRec, int idxThread)
		{
			zlog.debug(				
				"Client ID#" + clientRec.id.ToString() + " (" + 
				clientRec.remoteIp + ":" + clientRec.remotePort.ToString() + ") " +
				"LineData " + sLine.Length.ToString() + " bytes +"
				);
		}
		static void tcpTerminal_OnClientConnectedSuccess(ClientTcp clientRec)
		{
			zlog.debug(
				"Client ID#" + clientRec.id.ToString() + " (" + 
				clientRec.remoteIp + ":" + clientRec.remotePort.ToString() + ") " +
				"Connected *"
				);
			clientRec.write_line(__RTK_LOGO());
		}
		static string __RTK_LOGO () {
			return(@"
 ____ _____ _  __  ____   ___ ____   ___  
|  _ \_   _| |/ / |___ \ / _ \___ \ / _ \ 
| |_) || | | ' /    __) | | | |__) | | | |
|  _ < | | | . \   / __/| |_| / __/| |_| |
|_| \_\|_| |_|\_\ |_____|\___/_____|\___/ 
			");			
		}
		static void tcpTerminal_OnClientConnectedFail()
		{
			zlog.debug("Client Fail Connected");
		}
		static void tcpTerminal_OnClientDisconnected(ClientTcp clientRec)
		{
			zlog.debug(
				"Client ID#" + clientRec.id.ToString() + " (" +
				clientRec.remoteIp + ":" + clientRec.remotePort.ToString() + ") " +
				"Disconnected !"
				);
		}
	}
}
