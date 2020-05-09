using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Reflection;

using PTKSOFT.Lib;
using Newtonsoft.Json;

namespace MAIN
{
	class Program
	{
		static void Main(string[] args)
		{
			// Init Program version information
			that.Version += " (version " + Assembly.GetEntryAssembly().GetName().Version.ToString() + ")";
			Console.Title = that.Version;

			// Initialize Configuration
			if (!RUN.InitAll())
			{ Console.WriteLine("ERROR! Cannot init configuration file/folder/value"); return; }

			// Prepare Admin for verify existing running process
			if (!that.Init_InstanceTCP())
			{ Console.WriteLine("ERROR! Cannot bind InstanceTCP for start process"); return; }

			// Initialize Logfile
			if (!zlog.start(RUN.Config[KW.PathLogFile], 0, "", 0))
			{ Console.WriteLine("ERROR! Cannot init Logfile"); that.isQuit = true; Thread.Sleep(1000); return; }
			zlog.debug(that.Version + " ... Starting");

			// Initialize Global Vars
			that.Init_Global_Vars();
			
			// Init Terminal
			if (! TERMINAL.InitAll()) return;

			// Enter Main LOOP	----------------------------------------------------------------------------
			zlog.info(that.Version + " ... Ready");
			string today = my.datetime_to_sql(DateTime.Now).Substring(0, 10);
			int C15 = 0;
			while (!that.isQuit)
			{
				Thread.Sleep(1000);
				// Check day-switch and clear old log	-------------------------------
				if (!my.datetime_to_sql(DateTime.Now).Substring(0, 10).Equals(today))
				{
					zlog.info("Today was change from: " + today);
					today = my.datetime_to_sql(DateTime.Now).Substring(0, 10);
					zlog.info("..Today was change to: " + today);
					zlog.clear_old_log_file(120);
					zlog.info("....Finish run clear_old_log(120)_file PROCESS");
				}
				// Check 15 Second Check-Point	----------------------------------------
				C15++;
				if (C15 >= 15)
				{
					C15 = 0;
					zlog.info(that.Version + " ... 15-Second-Check-Point");
				}
			}
			Console.WriteLine("Terminate @ " + my.datetime_to_sql(DateTime.Now));
		}
	}
}
