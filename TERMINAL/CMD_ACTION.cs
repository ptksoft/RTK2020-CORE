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
	public class CMD_ACTION
	{
		public static string AllCommand = @"
				HELP
				CITY/LIST
				CITY/INFO
				OFFICER/LIST
				OFFICER/INFO
				GAME/VERSION
				GAME/QUIT
				GAME/TERMINATE
				";		
		public static bool Init_Action () 
		{
			CMD_TREE.MappingAction("HELP", HELP);
			
			CMD_TREE.MappingAction("CITY/LIST", EMPTY);
			CMD_TREE.MappingAction("CITY/INFO", EMPTY);
			
			CMD_TREE.MappingAction("OFFICER/LIST", EMPTY);
			CMD_TREE.MappingAction("OFFICER/INFO", EMPTY);
			
			CMD_TREE.MappingAction("GAME/VERSION", EMPTY);
			CMD_TREE.MappingAction("GAME/QUIT", GAME_QUIT);
			CMD_TREE.MappingAction("GAME/TERMINATE", GAME_TERMINATE);
			return(true);
		}
		
		public static void EMPTY (object[] arrObj) 
		{
			ClientTcp clientRec = (ClientTcp)arrObj[1];
			clientRec.write_line("This Command is NOT IMPLEMENT");
		}
		
		public static void HELP (object[] arrObj) 
		{
			List<string> listParam = (List<string>)arrObj[0];
			ClientTcp clientRec = (ClientTcp)arrObj[1];
			clientRec.write_line("Do You want some HELP?");
		}
		public static void GAME_QUIT (object[] arrObj) 
		{
			ClientTcp clientRec = (ClientTcp)arrObj[1];
			clientRec.write_line("Bye ...");
			clientRec.netStream.Close();
		}
		public static void GAME_TERMINATE (object[] arrObj)
		{
			ClientTcp clientRec = (ClientTcp)arrObj[1];
			clientRec.write_line("Terminating ...");
			clientRec.netStream.Close();
			
			zlog.warning("Game Engine was request to terminate by User");
			that.isQuit = true;
		}
	}
}
