using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;

namespace PTKSOFT.Lib
{
    [Serializable]
    public class CONFIG
    {
        // Private Variables ---------------------------------------------------
        private Hashtable myHash = Hashtable.Synchronized(new Hashtable());
        private string[] arrEQ = new string[] { "=" };
        private string _lastError = "";
        private string _lastFileName = "";

        // Private Method -----------------------------------------------------
        private bool _fetch_keyvalue_from_string(string sKeyEqValue) {	/*{{{
			*/
            if (sKeyEqValue.Contains("="))
            {
                try
                {
                    string[] asPart = sKeyEqValue.Split(arrEQ, 2, StringSplitOptions.None);
                    string sKey = asPart[0].Trim().ToUpper();
                    string sValue = asPart[1];
                    if (sKey.Length > 0)
                    {
                        lock (myHash.SyncRoot)
                        {
                            myHash[sKey] = sValue;
                        }
                        return (true);
                    }
                    else return (false);
                }
                catch (Exception ex)
                {
                    this._lastError = ex.Message;
                    return (false);
                }
            }
            else return (false);
        }	/*}}}*/

        // Constructor --------------------------------------------------------

        // Properties ---------------------------------------------------------
        public string this[string sKey] {	/*{{{
			*/
            get
            {
                sKey = sKey.Trim().ToUpper();
                lock (myHash.SyncRoot)
                {
                    if (myHash.Contains(sKey))
                    {
						if (myHash[sKey] == null) return ("");
                        else return (myHash[sKey].ToString());
                    }
                    else
                    {
                        return ("");
                    }
                }
            }
            set
            {
                sKey = sKey.Trim().ToUpper();
                lock (myHash.SyncRoot)
                {
                    if (sKey.Length > 0) myHash[sKey] = value;
                }
            }
        }	/*}}}*/
        public bool is_empty {	/*{{{
			*/
            get
            {
                lock (myHash.SyncRoot)
                {
                    return ((bool)(myHash.Count < 1));
                }
            }
        }	/*}}}*/
        public int count {	/*{{{
			*/
            get
            {
                lock (myHash.SyncRoot)
                {
                    return (myHash.Count);
                }
            }
        }	/*}}}*/
        public ICollection keys {	/*{{{
			*/
            get
            {
                lock (myHash.SyncRoot)
                {
                    return (myHash.Keys);
                }
            }
        }	/*}}}*/
        public string lastError { get { return (_lastError); } }

        // Method -------------------------------------------------------------
        public bool contains(string sKey) {	/*{{{
			*/
            lock (myHash.SyncRoot)
            {
                return (myHash.Contains(sKey.Trim().ToUpper()));
            }
        }	/*}}}*/
        public void flush() {	/*{{{
			*/
            lock (myHash.SyncRoot)
            {
                myHash.Clear();
            }
        }	/*}}}*/
        public bool remove(string sKeyToRemove) {	/*{{{
			*/
            sKeyToRemove = sKeyToRemove.Trim().ToUpper();
            lock (myHash.SyncRoot)
            {
                try
                {
                    if (myHash.Contains(sKeyToRemove))
                    {
                        myHash.Remove(sKeyToRemove);
                        return (true);
                    }
                    else
                        return (false);
                }
                catch (Exception ex)
                {
                    this._lastError = ex.Message;
                    return (false);
                }
            }
        }	/*}}}*/

        public bool save_to_file(string nameFile) {	/*{{{
			*/
            try
            {
                FileStream FS = File.Create(nameFile);
                StreamWriter SW = new StreamWriter(FS, Encoding.UTF8);

                // Write Packet Data
                string sBuffer;
                lock (myHash.SyncRoot)
                {
                    ArrayList arrKey = new ArrayList(myHash.Keys);
                    arrKey.Sort();
                    for (int i = 0; i < arrKey.Count; ++i)
                    {
                        sBuffer = arrKey[i] + "=" + (string)myHash[arrKey[i]];
                        SW.WriteLine(sBuffer);
                    }
                }

                // Finish
                SW.Flush();
                SW.Close();
                FS.Close();

                // Save history to lastFileName
                _lastFileName = nameFile;

                return (true);
            }
            catch (Exception ex)
            {
                this._lastError = ex.Message;
                return (false);
            }
        }	/*}}}*/
        public bool save() {	/*{{{
			*/
            if (_lastFileName.Length < 1) return (false);
            return (save_to_file(_lastFileName));
        }	/*}}}*/
        public bool load_from_file(string nameFile)	{	/*{{{
			*/
            try
            {
                FileStream FS = File.OpenRead(nameFile);
                StreamReader SR = new StreamReader(FS, Encoding.UTF8);
                string sLine;

                // Try To Read Packet Body If HeaderOK
                while ((sLine = SR.ReadLine()) != null) _fetch_keyvalue_from_string(sLine);

                // Clear Stream
                SR.Close();
                FS.Close();

                // Save History to lastFileName
                _lastFileName = nameFile;

                // Return What Happen depend on HeadersFetchResult
                return (true);
            }
            catch (Exception ex)
            {
                this._lastError = ex.Message;
                return (false);
            }
        }	/*}}}*/
        public bool load() {	/*{{{
			*/
            if (_lastFileName.Length < 1) return (false);
            return (load_from_file(_lastFileName));
        }	/*}}}*/

        public string debug_string(bool isSortKey) {	/*{{{
			*/
            string sBuff = "";

            sBuff +=
            "+----------------------------------------------------------------------+\n";
            lock (myHash.SyncRoot)
            {
                ArrayList arrKey = new ArrayList(myHash.Keys);
                if (isSortKey) arrKey.Sort();
                for (int i = 0; i < arrKey.Count; ++i)
                {
                    sBuff += "|" + arrKey[i] + "=" + (string)myHash[arrKey[i]] + "\n";
                }
            }
            sBuff +=
            "+----------------------------------------------------------------------+\n";

            return (sBuff);
        }	/*}}}*/
        public string debug_string() { /*{{{
			*/
			return (debug_string(false)); 
		}	/*}}}*/

        public string get_sql_string(string sKey) {	/*{{{
			*/
            return ("'" + this[sKey].Replace("'", "") + "'");
        }	/*}}}*/
        public int get_int(string sKey) {	/*{{{
			*/
            int iDumy = 0;
            if (int.TryParse(this[sKey], out iDumy))
                return (iDumy);
            else
                return (0);
        }	/*}}}*/
        public Int64 get_int64(string sKey) {	/*{{{
			*/
            Int64 iDumy = 0;
            if (Int64.TryParse(this[sKey], out iDumy))
                return (iDumy);
            else
                return (0);
        }	/*}}}*/
        public float get_float(string sKey) {	/*{{{
			*/
            float fDumy = 0.00f;
            if (float.TryParse(this[sKey], out fDumy))
                return (fDumy);
            else
                return (0.00f);
        }	/*}}}*/
        public decimal get_decimal(string sKey)	{	/*{{{
			*/
            decimal num = 0M;
            if (decimal.TryParse(this[sKey], out num))
                return (num);
            else
                return (0M);
        }	/*}}}*/
        public double get_double(string sKey) {	/*{{{
			*/
            double fDumy = 0.00;
            if (double.TryParse(this[sKey], out fDumy))
                return (fDumy);
            else
                return (0.00);
        }	/*}}}*/
    }
}
