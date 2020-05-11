using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace MAIN
{
	public class CMD_TREE
	{
		protected static Hashtable hCmdTree;
				
		public static void Init_HashCommandTree () {
			zlog.debug("Init Hashable Command TREE");
			hCmdTree = new Hashtable();
			string[] par = CMD_ACTION.AllCommand.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string s in par) {
				string[] t = s.Split(new string[] {"/"}, StringSplitOptions.RemoveEmptyEntries);
				Array.Sort(t);
				if (t.Length > 0) {
					_StoreToHash(hCmdTree, t);
				}
			}
		}
		static void _StoreToHash (Hashtable h, string[] t) {
			string t0 = t[0].Trim().ToUpper();
			if (t0.Length < 1) return;
			Hashtable child;
			if (! h.Contains(t0)) {
				// create new store
				child = new Hashtable();
				h[t0] = child;
			}
			else {
				// mapping existing
				child = (Hashtable)h[t0];
			}
			if (t.Length < 2) return;
			
			// Store next level with Recursive-call
			string[] tNext = new string[t.Length-1];
			List<string> L = new List<string>(t);
			L.RemoveAt(0);
			tNext = L.ToArray();
			_StoreToHash(child, tNext);
		}		
		
		public static bool MappingAction (string cmdPath, Action<object[]> actionProc) {
			// extract command path
			List<string> lC = new List<string>(
								cmdPath.Split(
									new string[] {"/"}, 
									StringSplitOptions.RemoveEmptyEntries
									)
								);
			// first jump point
			Hashtable curH = hCmdTree;
			
			try {
				// walk to near end of path
				for (int i=0; i<(lC.Count-1); i++) {
					curH = (Hashtable)curH[lC[i]];
				}
				// attach action to the end
				curH[lC[lC.Count-1]] = actionProc;
			}
			catch (Exception ex) {
				// something wrong
				zlog.error("Fail in MappingAction: " + ex.Message);
				return(false);
			}
			return(true);
		}
		
		public static void Show_HashCommandTree () {
			_PrintTreeHash(hCmdTree, "");
		}
		static void _PrintTreeHash(object o, string sIndent)
		{
			if (o is Hashtable)
			{	// Have child command
				Hashtable h = (Hashtable)o;
				if (h.Count < 1) return;
				string[] aK = new string[h.Keys.Count];
				h.Keys.CopyTo(aK, 0);
				Array.Sort(aK);
				foreach (string k in aK)
				{
					zlog.debug(sIndent + k);
					_PrintTreeHash(h[k], sIndent + "\t");
				}
			}
			else
			{	// Reach action object
				zlog.debug(sIndent + "->" + o.ToString());
			}
		}		
		
		public static bool TranslateCommand (
								string line, 
								ref List<string> listSubCMD, 
								ref List<string> listParam, 
								ref Action<Object[]> actionProc
								) 
		{
			// Prepare blank result
			listSubCMD = new List<string>();
			listParam = new List<string>();
			
			// Extract command path
			List<string> lCmd = new List<string>(
									line.Trim().Split(
										new string[] {"/"}, 
										StringSplitOptions.RemoveEmptyEntries
									)
								);
			// Check blank command
			if (lCmd.Count < 1) {
				// Display list of root command
				string[] v = new string[hCmdTree.Count];
				hCmdTree.Keys.CopyTo(v,0);
				listSubCMD = new List<string>(v);
				return(false);
			}
			
			// Begin to decode each level
			Hashtable h2Check = hCmdTree;
			while (true) {
				string C = lCmd[0].ToUpper();
				lCmd.RemoveAt(0);
				object oNow = _MapFromHash(h2Check, C);
				if (oNow == null) {
					listSubCMD = null;		// Tell that Invalid Command
					return(false);			// Not found VALID in command TREE
				}
				if (oNow is Action<Object[]>) 
				{
					// Reach Action Function
					actionProc = (Action<object[]>)oNow;
					break;
				}
				Hashtable hNow = (Hashtable)oNow;
				if (hNow.Count == 0) {
					// Reach bottom of TREE
					// and no Link to Action
					break;
				}
				if (lCmd.Count < 1) {
					// Empty next command, but have Sub Command
					string[] v = new string[hNow.Count];
					hNow.Keys.CopyTo(v, 0);
					listSubCMD = new List<string>(v);
					return(false);
				}
				h2Check = hNow;	// Use Result to be Hash Level to Check next cycle
			}
			listParam = lCmd;	// Return Remain Path as PARAM
			return(true);
		}
		static object _MapFromHash (Hashtable h, string cmd) {
			// check full mapping, if found return
			if (h.Contains(cmd)) return(h[cmd]);
			
			// check parial mapping, if found return
			string[] k = new string[h.Count];
			h.Keys.CopyTo(k, 0);
			Array.Sort(k);
			foreach (string s in k) {
				if (s.StartsWith(cmd)) {
					return(h[s]);
				}
			}
			
			// Not match any command
			return(null);
		}
		
	}
}
