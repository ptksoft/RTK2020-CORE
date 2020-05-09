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
	public class RUN
	{
		public static CONFIG Config;
		public static CONFIG Counter;
		public static CONFIG TimeLogic;
		public static CONFIG Transform;
		
		static string LastConfigModifyTime = "";

		static string folderConfig;
		static string fileConfigName;
		static string fileConfigBackup;
		public static string fileTariffName;
		public static string fileTransformName;
		static string fileCounterName;
		static string fileLogicTimeName;
		public static ArrayList allThread = ArrayList.Synchronized(new ArrayList());    // All running thread		
		
		static bool PrepareConfigFile ()
		{
			Console.WriteLine("RUN->Prepare Configuration...");
			folderConfig = my.program_path() + Path.DirectorySeparatorChar + "CONFIGs";
			if (!Directory.Exists(folderConfig))
			{
				try { Directory.CreateDirectory(folderConfig); }
				catch (Exception ex)
				{
					Console.WriteLine("ERROR! Cannot create config folder [" + folderConfig + "]" + ex.Message);					
					Thread.Sleep(5000);
					Console.ReadLine();
					return (false);
				}
			}
			fileConfigName = folderConfig
				+ Path.DirectorySeparatorChar
				+ "main.ini";
			fileConfigBackup = folderConfig
				+ Path.DirectorySeparatorChar
				+ "main__" + my.datetime_to_sql(DateTime.Now).Replace("-","").Replace(":","").Replace(" ","") + ".ini";
			Config = new CONFIG();			
			if (!Config.load_from_file(fileConfigName))
			{
				if (!__Config_Create_FirstTime())
				{
					Console.WriteLine("ERROR! Cannot Create Config File !!!");
					Thread.Sleep(5000);
					Console.ReadLine();
					return (false);
				}
			}
			if (!__Config_Verify_Loaded())
			{
				Console.WriteLine("ERROR! Cannot VerifyConfig !!!");
				Thread.Sleep(5000);
				Console.ReadLine();
				return (false);
			}

			// Everything OK, Success
			return (true);
		}
		private static bool __Config_Verify_Loaded()
		{
			bool isTrigSave = false;
			LastConfigModifyTime = Config[KW.LastConfigUpdateTime];
			
            // Every verification OK
            if (isTrigSave) TrickSaveConfig();
			return (true);
		}
        private static bool ____ValidateAndInitValue(string ConfigKey, string InitValue)
        {
            if (RUN.Config[ConfigKey] == "")
            {
                zlog.warning("! EMPTY Config[" + ConfigKey + "] then auto-init to [" + InitValue + "]");
                RUN.Config[ConfigKey] = InitValue;
                return (true);
            }
            return (false);
        }
		private static bool __Config_Create_FirstTime()
		{
			/* First Initialize Configuration */			
			Config[KW.LastConfigUpdateTime] = my.datetime_to_sql(DateTime.Now);
			Config[KW.PathLogFile] = my.program_path() + Path.DirectorySeparatorChar + "LOGs";
			Config[KW.InstantBindPort] = "51630";	// Port for Instant Bind
			

			/* Try Save Configuration file */
			return (Config.save_to_file(fileConfigName));
		}

		static void Thread_SaveConfig()
		{
			zlog.debug("Thread SaveConfig START...");
			while (!that.isQuit)
			{
				Thread.Sleep(1000);
				if (LastConfigModifyTime == Config[KW.LastConfigUpdateTime]) { continue; }

				// Config has been Modify So, Save it Now
				if (Config.save_to_file(fileConfigBackup)) {
					zlog.debug("Running->ThreadSaveConfig: BACKUP Config.save_to_file(" + fileConfigBackup + ")");
				}
				else {
					zlog.error("Running->ThreadSaveConfig: ERROR!! Config.save_to_file(" + fileConfigName + ")");
				}
				Config[KW.LastConfigUpdateTime] = my.datetime_to_sql(DateTime.Now);
				if (Config.save_to_file(fileConfigName))
				{
					zlog.debug("Running->ThreadSaveConfig: SUCCESS Config.save_to_file(" + fileConfigName + ")");
					LastConfigModifyTime = Config[KW.LastConfigUpdateTime];
				}
				else
				{
					zlog.error("Running->ThreadSaveConfig: ERROR!! Config.save_to_file(" + fileConfigName + ")");
				}
			}
			zlog.debug("Thread SaveConfig STOP...");
		}
		public static void TrickSaveConfig()
		{
			Config[KW.LastConfigUpdateTime] = my.datetime_mesec_to_sql(DateTime.Now);
		}

		public static bool InitAll()
		{
			if (!PrepareConfigFile()) return (false);

			try {
				Thread t = new Thread(new ThreadStart(Thread_SaveConfig));
				lock (RUN.allThread.SyncRoot) { RUN.allThread.Add(t); }
				t.Name = "RUN.T.SaveConfig";
				t.IsBackground = true;
				t.Start();
			}
			catch (Exception ex) {
				zlog.error("Error in start Thread: " + ex.Message);
				return(false);
			}
			
			return (true);		
		}
			
	}
}
