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
	public class that
	{
		public static bool isQuit = false;
		public static string Version = "RTK2020-CORE";
		public static Random RDN = new Random();

        public static TcpListener InstanceTCP;
		public static bool Init_InstanceTCP()
		{
			InstanceTCP = new TcpListener(IPAddress.Loopback, RUN.Config.get_int(KW.InstantBindPort));
			try
			{
				InstanceTCP.Start();
				return (true);
			}
			catch
			{
				return (false);
			}

		}		        
		public static void Init_Global_Vars()
		{
			// Additional Master & Transaction file configuration
		}

	}
}
