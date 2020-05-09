using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MAIN
{
    class zINTERVAL     // interval Checking Class
    {
        private double _msecTimeOut = 0;
        private DateTime _timeStamp = new DateTime(0);
        private DateTime _emptyTime = new DateTime(0);
        private TimeSpan _tspan;

        public zINTERVAL(double msecTimeOut, bool isStartNow) {	/*{{{
			*/
            _msecTimeOut = msecTimeOut;
            if (isStartNow) _timeStamp = DateTime.Now;
        }	/*}}}*/
        public zINTERVAL(double msecTimeOut) : this(msecTimeOut, false) { }
        public zINTERVAL() : this(0, false) { }

        public bool Empty {	/*{{{
			*/
            get { return ((bool)(_timeStamp == _emptyTime)); }
        }	/*}}}*/
        public bool Expire {	/*{{{
			*/
            get
            {
                if ((_timeStamp == _emptyTime) || (_msecTimeOut == 0)) return (false);
                _tspan = DateTime.Now - _timeStamp;
                return ((bool)(_tspan.TotalMilliseconds >= _msecTimeOut));
            }
        }	/*}}}*/

        public bool ExpireFrom(double msecToCheck) {	/*{{{
			*/
            if (_timeStamp == _emptyTime) return (false);
            _tspan = DateTime.Now - _timeStamp;
            return ((bool)(_tspan.TotalMilliseconds >= msecToCheck));
        }	/*}}}*/
        public void Set() { _timeStamp = DateTime.Now; }
        public void Reset() { _timeStamp = _emptyTime; }
    }

    class zPACKET    // Log Message Packet
    {
        public DateTime When = DateTime.MinValue;
        public zLEVEL Level = zLEVEL.DEBUG;
        public string Data = "";

        public bool isWriteFile = true;
        public bool isWriteConsole = true;
        public bool isWriteClient = true;
        public bool isWriteServer = true;

        public zPACKET(string data, zLEVEL level) {	/*{{{
			*/
            this.When = DateTime.Now;
            this.Data = data;
            this.Level = level;
        }	/*}}}*/
    }

    public enum zLEVEL      // Log Level
    {
        DEBUG = 0,
        INFO = 1,
        WARNING = 2,
        ERROR = 3
    }
   
    public class zlog
    {
        private static bool isQuit = false;                 // Primary Flag for This module, every thread should depend on this flag
        private static bool isInit = false;                 // Startup Flag, every thread should check before start or doing something

        private static string folderLog = "";               // Path that Log Folder should store
        
        private static string fileLogName = "";             // File Name of current log file                
        private static string fileDate = "";                // Current date of file log
        private static StreamWriter fileLogStream = null;       // File Pointer for write log to file stream

        /* Customize thread priority for each major thread */
        public static ThreadPriority priorityThrMaster = ThreadPriority.BelowNormal;
        public static ThreadPriority priorityThrWriteFile = ThreadPriority.Lowest;
        public static ThreadPriority priorityThrWriteConsole = ThreadPriority.Lowest;
        public static ThreadPriority priorityThrWriteClient = ThreadPriority.Lowest;
        public static ThreadPriority priorityThrWriteServer = ThreadPriority.Lowest;

        /* Customize to InputOutPut for each Thread */
        public static bool isWriteFile = true;
        public static bool isWriteConsole = true;
        public static bool isWriteClient = true;
        public static bool isWriteServer = true;

        /* format of file log that can customize before start() method */
        public static string fileLogPrefix = "";           // Prefix for log file Name
        public static string fileLogSuffix = "";           // Suffix for log file Name
        public static string fileLogExt = "log";           // Extension name for file log (default is .log)

        private static int clientPort = 0;                  // Port Number to Listening for client connect
        
        private static string serverIp = "";                // IP Address for Central log server
        private static int serverPort = 0;                  // Port Number for Connect to Central log server

        private static Queue queMaster = Queue.Synchronized(new Queue());       // Master Que Job for deploy to each queue
        private static Queue queWriteFile = Queue.Synchronized(new Queue());    // Que Job for write log to Text File
        private static Queue queWriteConsole = Queue.Synchronized(new Queue()); // Que Job for write log to Console
        private static Queue queWriteClient = Queue.Synchronized(new Queue()); // Que Job for write log to TCP/IP Client
        private static Queue queWriteServer = Queue.Synchronized(new Queue());  // Que Job for write log to Central TCP/IP Log Server
        private static string[] charLogLevel = new string[] { "D", "I", "W", "E"};     // Prefix character show in file log as first char of line

        /* Flag for check thread is starting OK? */
        private static bool isStartOK_Master = false;
        private static bool isStartOK_WriteFile = false;
        private static bool isStartOK_WriteConsole = false;
        private static bool isStartOK_WriteClient = false;
        private static bool isStartOK_WriteServer = false;

        /* Thread Method for each logging Role */
        private static void _thread_master() {	/*{{{
			*/
            zlog.isStartOK_Master = true;       // ok we enter this thread it create success
            zPACKET pk = null;
            while (!zlog.isQuit)
            {
                Thread.Sleep(0);                // Let other process interrupt

                lock (queMaster.SyncRoot)
                {
                    if (queMaster.Count > 0) { pk = (zPACKET)queMaster.Dequeue(); }
                    else { pk = null; }
                }
                if (pk != null)
                {
                    // Distribute Log to each thread
                    if (pk.isWriteFile) { lock (queWriteFile.SyncRoot) { queWriteFile.Enqueue(pk); } }
                    if (pk.isWriteConsole) { lock (queWriteConsole.SyncRoot) { queWriteConsole.Enqueue(pk); } }
                    if (pk.isWriteClient) { lock (queWriteClient.SyncRoot) { queWriteClient.Enqueue(pk); } }
                    if (pk.isWriteServer) { lock (queWriteServer.SyncRoot) { queWriteServer.Enqueue(pk); } }

                    // Continue next cycle with let change other process to interrupt
                    continue;
                }

                // Not have any more log queue, Sleep at least 1 microsecond
                Thread.Sleep(1);  
            }
        }	/*}}}*/
        private static void _thread_write_file() {	/*{{{
			*/
            zlog.debug("zLog Thread_WriteFile ... Start");
            zlog.debug("zLog Thread_WriteFile ... folderLog = " + zlog.folderLog);

            // Check and Create Folder
            if (!Directory.Exists(zlog.folderLog))
            {
                try
                {
                    Directory.CreateDirectory(zlog.folderLog);
                }
                catch (Exception ex)
                {
                    zlog.lastError = "Thread WriteFile cannot create Director! {" + ex.Message + "}";
                    return;
                }
            }            
            
            // This point we can say this thread is start success
            zlog.isStartOK_WriteFile = true;

            // Main LOOP for cycle each Log Packet Queue
            while (!zlog.isQuit)
            {
                Thread.Sleep(0);
                lock (queWriteFile.SyncRoot)
                {
                    if (queWriteFile.Count > 0)
                    {
                        zPACKET pk = (zPACKET)queWriteFile.Dequeue();
                        __check_and_prepare_file();
                        
                        try
                        {
                            zlog.fileLogStream.WriteLine(
                                zlog.charLogLevel[(int)pk.Level] +
                                pk.When.ToString("[HHmmss:ffffff] ") +
                                pk.Data
                                );
                        }
                        catch (Exception ex)
                        {
                            zlog.lastError = ex.Message;
                        }
                        // Do something with Log Packet, Then continue next packet
                        continue;
                    }
                }
                Thread.Sleep(1);
            }
        }	/*}}}*/
        private static bool __check_and_prepare_file() {	/*{{{
			*/
            try
            {
                string newFileDate = __date_now_iso();
                if (zlog.fileDate != newFileDate)
                {
                    // Check Directory and File Name
                    if (!Directory.Exists(zlog.folderLog)) Directory.CreateDirectory(zlog.folderLog);
                    zlog.fileLogName = __gen_file_name();

                    // Close current open file
                    if (zlog.fileLogStream != null) zlog.fileLogStream.Close();

                    // Open new file
                    zlog.fileLogStream = File.AppendText(zlog.folderLog + Path.DirectorySeparatorChar + zlog.fileLogName);
                    zlog.fileLogStream.AutoFlush = true;

                    // File is upto date Now
                    zlog.fileDate = newFileDate;
                }

                // Everything OK
                return (true);
            }
            catch (Exception ex)
            {
                zlog.lastError = ex.Message;
                return (false);
            }
        }	/*}}}*/
        private static string __gen_file_name() {	/*{{{
			*/
            return (
               zlog.fileLogPrefix +
               zlog.__date_now_iso() +
               zlog.fileLogSuffix +
               "." +
               zlog.fileLogExt
            );
        }	/*}}}*/
        private static string __date_now_iso() {	/*{{{
			*/
            return (
                string.Format("{0:00}", DateTime.Now.Year) + "-" +
                string.Format("{0:00}", DateTime.Now.Month) + "-" +
                string.Format("{0:00}", DateTime.Now.Day)
            );
        }	/*}}}*/
        private static void _thread_write_console() {	/*{{{
			*/
            zlog.isStartOK_WriteConsole = true;
            zlog.debug("zLog Thread_WriteConsole ... Start");

            // Main LOOP for cycle each Log Packet Queue
            while (!zlog.isQuit)
            {
                Thread.Sleep(0);
                lock (queWriteConsole.SyncRoot)
                {
                    if (queWriteConsole.Count > 0)
                    {
                        zPACKET pk = (zPACKET)queWriteConsole.Dequeue();
                        Console.Write(pk.When.ToString("[HHmmss:ffffff] "));
                        Console.WriteLine(pk.Data);
                        continue;
                    }
                }
                Thread.Sleep(1);
            }
        }	/*}}}*/
        private static void _thread_write_client() {	/*{{{
			*/
            zlog.isStartOK_WriteClient = true;
            zlog.debug("zLog Thread_WriteClient ... Start");

            // Check Client Port and Init Client Service
            if (zlog.clientPort > 0)
            {
                // Init and Listen TCP Server for client connect
            }

            // Main LOOP for cycle each Log Packet Queue
            while (!zlog.isQuit)
            {
                Thread.Sleep(0);
                lock (queWriteClient.SyncRoot)
                {
                    if (queWriteClient.Count > 0)
                    {
                        zPACKET pk = (zPACKET)queWriteClient.Dequeue();
                        // Do something with Log Packet, Then continue next packet
						pk = null;
                        continue;
                    }
                }
                Thread.Sleep(1);
            }
        }	/*}}}*/
        private static void _thread_write_server() {	/*{{{
			*/
            zlog.isStartOK_WriteServer = true;
            zlog.debug("zLog Thread_WriteServer ... Start");

            // Check and Init Server Port
            if (zlog.serverPort > 0)
            {
                if (zlog.serverIp.Trim().Length == 0) zlog.serverIp = "127.0.0.1";
                // Not Init client for send log to server
            }

            // Main LOOP for cycle each Log Packet Queue
            while (!zlog.isQuit)
            {
                Thread.Sleep(0);
                lock (queWriteServer.SyncRoot)
                {
                    if (queWriteServer.Count > 0)
                    {
                        zPACKET pk = (zPACKET)queWriteServer.Dequeue();
                        // Do something with Log Packet, Then continue next packet
						pk = null;
                        continue;
                    }
                }
                Thread.Sleep(1);
            }
        }	/*}}}*/

        public static string lastError = "";                // Last Error of this module
        public static bool start(string strFolderLog, int numClientPort, string strServerIp, int numServerPort) {	/*{{{
			*/
            if (zlog.isInit) return (true);

            // Prepare folder Log
            zlog.folderLog = 
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
                Path.DirectorySeparatorChar + 
                "LOGs";
            if (strFolderLog.Length > 0) 
                zlog.folderLog = strFolderLog;

            // Init Master Thread
            Thread thrMaster = new Thread(new ThreadStart(_thread_master));
            thrMaster.Name = "zLog.Thread_Master";
            thrMaster.IsBackground = true;
            thrMaster.Priority = priorityThrMaster;
            thrMaster.Start();
            zINTERVAL itvMaster = new zINTERVAL(5000, true);
            while ((!zlog.isStartOK_Master) && (!itvMaster.Expire)) Thread.Sleep(1);
            if (!zlog.isStartOK_Master) return (false);
            
            // Init WriteFile Thread
            Thread thrWriteFile = new Thread(new ThreadStart(_thread_write_file));
            thrWriteFile.Name = "zLog.Thread_WriteFile";
            thrWriteFile.IsBackground = true;
            thrWriteFile.Priority = priorityThrWriteFile;
            thrWriteFile.Start();
            zINTERVAL itvWriteFile = new zINTERVAL(5000, true);
            while ((!zlog.isStartOK_WriteFile) && (!itvWriteFile.Expire)) Thread.Sleep(1);
            if (!zlog.isStartOK_WriteFile) return (false);

            // Init WriteConsole Thread
            Thread thrWriteConsole = new Thread(new ThreadStart(_thread_write_console));
            thrWriteConsole.Name = "zLog.Thread_WriteConsole";
            thrWriteConsole.IsBackground = true;
            thrWriteConsole.Priority = priorityThrWriteConsole;
            thrWriteConsole.Start();
            zINTERVAL itvWriteConsole = new zINTERVAL(5000, true);
            while ((!zlog.isStartOK_WriteConsole) && (!itvWriteConsole.Expire)) Thread.Sleep(1);
            if (!zlog.isStartOK_WriteConsole) return (false);

            // Init WriteClient Thread
            Thread thrWriteClient = new Thread(new ThreadStart(_thread_write_client));
            thrWriteClient.Name = "zLog.Thread_WriteClient";
            thrWriteClient.IsBackground = true;
            thrWriteClient.Priority = priorityThrWriteClient;
            thrWriteClient.Start();
            zINTERVAL itvWriteClient = new zINTERVAL(5000, true);
            while ((!zlog.isStartOK_WriteClient) && (!itvWriteClient.Expire)) Thread.Sleep(1);
            if (!zlog.isStartOK_WriteClient) return (false);

            // Init WriteServer Thread
            Thread thrWriteServer = new Thread(new ThreadStart(_thread_write_server));
            thrWriteServer.Name = "zLog.Thread_WriteServer";
            thrWriteServer.IsBackground = true;
            thrWriteServer.Priority = priorityThrWriteServer;
            thrWriteServer.Start();
            zINTERVAL itvWriteServer = new zINTERVAL(5000, true);
            while ((!zlog.isStartOK_WriteServer) && (!itvWriteServer.Expire)) Thread.Sleep(1);
            if (!zlog.isStartOK_WriteServer) return (false);

            isInit = true;  // Everything OK
            return (true);
        }	/*}}}*/
        public static bool start() {	/*{{{
			*/
            return (zlog.start("", 0, "", 0));
        }	/*}}}*/
        public static void stop() {	/*{{{
			*/
            zlog.isQuit = true;
        }	/*}}}*/

        public static int clear_old_log_file(int dayAge) {	/*{{{
			*/
            if (!zlog.isInit)
            {
                zlog.lastError = "NOT! zLog.isInit()";
                return (-1);
            }
            if (zlog.isQuit)
            {
                zlog.lastError = "zLog isQuit !";
                return (-1);
            }
            if (dayAge < 2)
            {
                zlog.lastError = "dayAge < 2";
                return (-1);
            }

            if (zlog.folderLog.Length < 1)
            {
                zlog.lastError = "zLog.folerLog is EMPTY";
                return (-1);
            }
            if (!Directory.Exists(zlog.folderLog))
            {
                zlog.lastError = "NOT! exists zLog.folderLog (" + zlog.folderLog + ")";
                return (-1);
            }

            string[] pathList = Directory.GetFiles(zlog.folderLog, "*." + zlog.fileLogExt);
            if (pathList.Length < 1) return(-1);
            
            int countFileClear = 0;
            for (int i = 0; i < pathList.Length; i++)
            {
                string fileNameOnly = Path.GetFileNameWithoutExtension(pathList[i]);
                if (zlog.fileLogPrefix.Length > 0)
                    fileNameOnly = fileNameOnly.Replace(zlog.fileLogPrefix, "");
                if (zlog.fileLogSuffix.Length > 0)
                    fileNameOnly = fileNameOnly.Replace(zlog.fileLogSuffix, "");
                string[] arrTm = fileNameOnly.Split(new string[] { "-" }, StringSplitOptions.None);
                if (arrTm.Length != 3) continue;
                DateTime dateOfFile = DateTime.MinValue;
                try
                {
                    dateOfFile = new DateTime(
                                    int.Parse(arrTm[0]),
                                    int.Parse(arrTm[1]),
                                    int.Parse(arrTm[2])
                                    );
                }
                catch { continue; }
                if (dateOfFile == DateTime.MinValue) continue;
                TimeSpan interval = DateTime.Now - dateOfFile;
                if (interval.Days >= dayAge)
                {
                    try { File.Delete(pathList[i]); countFileClear++; }
                    catch {}
                }
            }

            // Everyting OK
            return (countFileClear);
        }	/*}}}*/

        public static void write(string data, zLEVEL level) {	/*{{{
			*/
            if (zlog.isQuit) return;

            // Determine if this thread have Name?
            string threadName = "";
            if (Thread.CurrentThread.Name != null) threadName = Thread.CurrentThread.Name.Trim();
            if (threadName.Length > 0) threadName = "{" + threadName + "} ";
            
            // Build log Packet and Enqueue to Master Thread
            zPACKET pk = new zPACKET(threadName + data, level);
            pk.isWriteFile = isWriteFile;
            pk.isWriteConsole = isWriteConsole;
            pk.isWriteClient = isWriteClient;
            pk.isWriteServer = isWriteServer;
            lock (queMaster.SyncRoot) { queMaster.Enqueue(pk); }
        }	/*}}}*/
        public static void debug(string data) { zlog.write(data, zLEVEL.DEBUG); }
        public static void info(string data) { zlog.write(data, zLEVEL.INFO); }
        public static void warning(string data) { zlog.write(data, zLEVEL.WARNING); }
        public static void error(string data) { zlog.write(data, zLEVEL.ERROR); }
    }
}
