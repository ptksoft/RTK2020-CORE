using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;

namespace PTKSOFT.Lib
{    
    public class my
    {        
        public static bool save_class_to_disk(object classToSave, string sFile) {	/*{{{
			*/
            try
            {
                FileStream theStream = System.IO.File.Create(sFile);
                BinaryFormatter binFormat = new BinaryFormatter();
                binFormat.Serialize(theStream, classToSave);
                theStream.Close();
                return (true);
            }
            catch
            {
                return (false);
            }
        }	/*}}}*/
        public static object load_class_from_disk(string sFile) {	/*{{{
			*/
            try
            {
                FileStream theStream = System.IO.File.Open(sFile, FileMode.Open);
                BinaryFormatter binFormat = new BinaryFormatter();
                object objDumy = binFormat.Deserialize(theStream);
                theStream.Close();
                return (objDumy);
            }
            catch
            {
                return (null);
            }
        }	/*}}}*/

        public static Hashtable hash_from_textfile(string fileName) {	/*{{{
			*/
            Hashtable myHash = new Hashtable();
            try
            {
                FileStream FS = File.OpenRead(fileName);
                StreamReader SR = new StreamReader(FS);
                string sLine;
                while ((sLine = SR.ReadLine()) != null)
                {
                    sLine = sLine.Trim();
                    if (!(sLine.Substring(0, 1).Equals("#")))
                    {
                        // This line is not comment line
                        if (sLine.Contains("="))
                        {
                            string[] sPart = sLine.Split(new string[] { "=" }, 2, StringSplitOptions.None);
                            myHash.Add(sPart[0].Trim(), sPart[1]);
                        }
                    }
                }
                SR.Close();
                FS.Close();
            }
            catch
            {
                myHash.Clear();
            }
            return (myHash);
        }	/*}}}*/
        public static bool hash_to_textfile(Hashtable hashData, string fileName) {	/*{{{
			*/
            if (fileName.Trim().Length < 1 || hashData.Count < 1) return (false);
            try
            {
                FileStream FS = File.Create(fileName);
                StreamWriter SW = new StreamWriter(FS);
                string sBuffer;
                foreach (string KeyName in hashData.Keys)
                {
                    sBuffer = KeyName + "=" + (string)hashData[KeyName];
                    SW.WriteLine(sBuffer);
                }
                SW.Close();
                FS.Close();
                return (true);
            }
            catch
            {
                return (false);
            }
        }	/*}}}*/

        public static T clone_object<T>(T source) {	/*{{{
			*/
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }	/*}}}*/

        public static Process execute_return_process(string sFile, string sParam, bool isShowWindows, bool isUseShell) {	/*{{{
			*/
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = sFile;
            myProcess.StartInfo.Arguments = sParam;
            myProcess.StartInfo.UseShellExecute = isUseShell;
            myProcess.StartInfo.CreateNoWindow = !isShowWindows;
            if (!isShowWindows) myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.Start();
            return (myProcess);
        }	/*}}}*/
        public static Process execute_return_process(string sFile, string sParam, bool isShowWindows) {	/*{{{
			*/
            return (execute_return_process(sFile, sParam, isShowWindows, false));
        }	/*}}}*/
        public static void execute_process(string sFile, string sParam, bool isWaitProcessEnd, bool isShowWindows) {	/*{{{
			*/
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = sFile;
            myProcess.StartInfo.Arguments = sParam;
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.CreateNoWindow = !isShowWindows;


            myProcess.Start();
            if (isWaitProcessEnd) myProcess.WaitForExit();
        }	/*}}}*/
        public static void execute_process(string sFile, string sParam) {	/*{{{
			*/
            execute_process(sFile, sParam, true, false);
        }	/*}}}*/
        public static void execute_process(string sFile) {	/*{{{
			*/
            execute_process(sFile, "", true, false);
        }	/*}}}*/

        public static string random_md5_string() {	/*{{{
			*/
            System.Random RDN = new Random();
            string sRandom = "";
            for (int i = 0; i < 100; ++i) sRandom += RDN.NextDouble().ToString();
            sRandom +=
            (
                string.Format("{0:00}", DateTime.Today.Year) + "-" +
                string.Format("{0:00}", DateTime.Today.Month) + "-" +
                string.Format("{0:00}", DateTime.Today.Day) + " " +
                string.Format("{0:00}", DateTime.Now.Hour) + ":" +
                string.Format("{0:00}", DateTime.Now.Minute) + ":" +
                string.Format("{0:00}", DateTime.Now.Second)
            );
            for (int i = 0; i < 100; ++i) sRandom += RDN.NextDouble().ToString();
            return (my.md5_string(sRandom));
        }	/*}}}*/

        public static string today_sql_datetime() {	/*{{{
			*/
            return (my.datetime_to_sql(DateTime.Now));
        }	/*}}}*/
        public static string today_sql_datetime_msec() {	/*{{{
			*/
            return (my.datetime_mesec_to_sql(DateTime.Now));
        }	/*}}}*/
        public static string program_path() {	/*{{{
			*/
            return (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        }	/*}}}*/

        public static string byte_to_hex(byte bNum) {	/*{{{
			*/
            return (string.Format("{0:X2}", bNum));
        }	/*}}}*/
        public static string byte_to_bin(byte bNum) {	/*{{{
			*/
            byte[] myBIT = { 1, 2, 4, 8, 16, 32, 64, 128 };
            string sDumy = "";
            for (int N = 7; N >= 0; --N)
            {
                if ((bNum & myBIT[N]) == myBIT[N])
                {
                    sDumy += "1";
                }
                else
                {
                    sDumy += "0";
                }
            }
            return (sDumy);
        }	/*}}}*/
        public static byte hex_to_byte(string sHex) {	/*{{{
			*/
            try
            {
                int nDumy = int.Parse(sHex, System.Globalization.NumberStyles.HexNumber);
                byte[] aByte = BitConverter.GetBytes(nDumy);
                return (aByte[0]);  // Only 1 Byte
            }
            catch
            {
                return (0);     // Error Convert
            }
        }	/*}}}*/
        public static int hex_to_int(string sHex) {	/*{{{
			*/
            try
            {
                int nDumy = int.Parse(sHex, System.Globalization.NumberStyles.HexNumber);
                return (nDumy); // Convert Success
            }
            catch
            {
                return (0);     // Error Convert
            }
        }	/*}}}*/
        public static string byte_array_to_hex(byte[] aData) {	/*{{{
			*/
            string sDumy = "";
            for (int i = 0; i < aData.Length; ++i)
                sDumy += (String.Format("{0:X2}", aData[i])) + " ";
            return (sDumy.Trim());
        }	/*}}}*/
        public static string string_to_hex(string sData) {	/*{{{
			*/
            Encoding enc = Encoding.GetEncoding(1252);
            byte[] dataAnsi = enc.GetBytes(sData);

            string sDumy = "";
            for (int i = 0; i < dataAnsi.Length; ++i)
                sDumy += (string.Format("{0:X2}", dataAnsi[i]) + " ");
            return (sDumy.Trim());
        }	/*}}}*/

        public static string base64_encode(string rawData, Encoding rawEnc, Base64FormattingOptions option) {	/*{{{
			*/
            try
            {
                byte[] bData = rawEnc.GetBytes(rawData);
                return (Convert.ToBase64String(bData, option));
            }
            catch
            {
                return ("");
            }
        }	/*}}}*/
        public static string base64_encode(string rawData, Encoding rawEnc) {	/*{{{
			*/
            try
            {
                return (base64_encode(rawData, rawEnc, Base64FormattingOptions.None));
            }
            catch
            {
                return ("");
            }
        }	/*}}}*/
        public static string base64_encode(string rawData) {	/*{{{
			*/
            try
            {
                return (base64_encode(rawData, Encoding.Default));
            }
            catch
            {
                return ("");
            }
        }	/*}}}*/
        public static string base64_decode(string dataBase64, Encoding wannaEnc) {	/*{{{
			*/
            try
            {
                byte[] bData = Convert.FromBase64String(dataBase64);
                return (wannaEnc.GetString(bData));
            }
            catch
            {
                return ("");
            }
        }	/*}}}*/
        public static string base64_decode(string dataBase64) {	/*{{{
			*/
            try
            {
                return (base64_decode(dataBase64, Encoding.Default));
            }
            catch
            {
                return ("");
            }
        }	/*}}}*/

        public static string md5_string(string sData, Encoding Enc) {	/*{{{
			*/
            byte[] bSource = Enc.GetBytes(sData);
            byte[] bHash = new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(bSource);
            return (BitConverter.ToString(bHash).ToLower().Replace("-", ""));
        }	/*}}}*/
        public static string md5_string(string sData) {	/*{{{
			*/
            return (md5_string(sData, Encoding.Default));
        }	/*}}}*/
        public static string md5_file(string sFilePath) {	/*{{{
			*/
            try
            {
                System.Security.Cryptography.MD5CryptoServiceProvider md5Provider = new System.Security.Cryptography.MD5CryptoServiceProvider();
                System.IO.FileStream fs = new System.IO.FileStream(sFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                Byte[] hashCode = md5Provider.ComputeHash(fs);
                fs.Close();
                return (BitConverter.ToString(hashCode).ToLower().Replace("-", ""));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }	/*}}}*/
        public static string sha1_string(string sData, Encoding Enc) {	/*{{{
			*/
            byte[] bSource = Enc.GetBytes(sData);
            byte[] bHash = new System.Security.Cryptography.SHA1CryptoServiceProvider().ComputeHash(bSource);
            return (BitConverter.ToString(bHash).ToLower().Replace("-", ""));
        }	/*}}}*/
        public static string sha1_string(string sData) {	/*{{{
			*/
            return (sha1_string(sData, Encoding.Default));
        }	/*}}}*/
        
        public static string datetime_to_sql(DateTime DateAndTime) {	/*{{{
			*/
            return (
                string.Format("{0:00}", DateAndTime.Year) + "-" +
                string.Format("{0:00}", DateAndTime.Month) + "-" +
                string.Format("{0:00}", DateAndTime.Day) + " " +
                string.Format("{0:00}", DateAndTime.Hour) + ":" +
                string.Format("{0:00}", DateAndTime.Minute) + ":" +
                string.Format("{0:00}", DateAndTime.Second)
            );
        }	/*}}}*/
        public static string datetime_mesec_to_sql(DateTime DateAndTime) {	/*{{{
			*/
            return (
                string.Format("{0:00}", DateAndTime.Year) + "-" +
                string.Format("{0:00}", DateAndTime.Month) + "-" +
                string.Format("{0:00}", DateAndTime.Day) + " " +
                string.Format("{0:00}", DateAndTime.Hour) + ":" +
                string.Format("{0:00}", DateAndTime.Minute) + ":" +
                string.Format("{0:00}", DateAndTime.Second) + "." +
                string.Format("{0:000}", DateAndTime.Millisecond)
            );
        }	/*}}}*/
        public static DateTime sql_to_datetime(string sqlFormatDateTime) {	/*{{{
			*/
            /* 2008-01-02 11:30:42.372 */
            /* 01234567890123456789012
             *           10        20
             **/
            try
            {
                DateTime dtBuff =
                    new DateTime(
                        int.Parse(sqlFormatDateTime.Substring(0, 4)),
                        int.Parse(sqlFormatDateTime.Substring(5, 2)),
                        int.Parse(sqlFormatDateTime.Substring(8, 2)),
                        int.Parse(sqlFormatDateTime.Substring(11, 2)),
                        int.Parse(sqlFormatDateTime.Substring(14, 2)),
                        int.Parse(sqlFormatDateTime.Substring(17, 2))
                    );
                return (dtBuff);
            }
            catch
            {
                return (new DateTime(0));
            }
        }	/*}}}*/
        public static DateTime sql_to_datetime_msec(string sqlFormatDateTime) {	/*{{{
			*/
            try
            {
                DateTime dtBuff =
                    new DateTime(
                        int.Parse(sqlFormatDateTime.Substring(0, 4)),
                        int.Parse(sqlFormatDateTime.Substring(5, 2)),
                        int.Parse(sqlFormatDateTime.Substring(8, 2)),
                        int.Parse(sqlFormatDateTime.Substring(11, 2)),
                        int.Parse(sqlFormatDateTime.Substring(14, 2)),
                        int.Parse(sqlFormatDateTime.Substring(17, 2)),
                        int.Parse(sqlFormatDateTime.Substring(20, 3))
                    );
                return (dtBuff);
            }
            catch
            {
                return (new DateTime(0));
            }
        }	/*}}}*/

        public static int to_int(string sNumber) {	/*{{{
			*/
            int iDumy;
            if (int.TryParse(sNumber, out iDumy)) return (iDumy);
            return (0);
        }	/*}}}*/
        public static Int64 to_int64(string sNumber) {	/*{{{
			*/
            Int64 iDumy;
            if (Int64.TryParse(sNumber, out iDumy)) return (iDumy);
            return (0);
        }	/*}}}*/
        public static float to_float(string sNumber) {	/*{{{
			*/
            float fDumy;
            if (float.TryParse(sNumber, out fDumy)) return (fDumy);
            return (0.00f);
        }	/*}}}*/
        public static double to_double(string sNumber) {	/*{{{
			*/
            double dDumy;
            if (double.TryParse(sNumber, out dDumy)) return (dDumy);
            return (0.00);
        }	/*}}}*/

        private static string[] _wordDigit = new string[] { "", "สิบ", "ร้อย", "พัน", "หมื่น", "แสน" };
        private static string[] _wordNum = new string[] { "", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า" };
        private static string _reverse_number_string(string sData) {	/*{{{
			*/
            string sDumy = "";
            string sDigit = "";
            int nDigit = 0;
            for (int i = sData.Length - 1; i >= 0; --i)
            {
                sDigit = sData.Substring(i, 1);
                if (int.TryParse(sDigit, out nDigit))
                    if (nDigit >= 0 && nDigit <= 9) sDumy += sDigit;
            }
            return (sDumy);
        }	/*}}}*/
        private static string _combie_number_and_digit(int iNum, int iDigit, int numAtDigitTen) {	/*{{{
			*/
            if (iDigit == 1)
            {
                // หลักสิบ
                if (iNum == 1) return (_wordDigit[iDigit]);     // สิบ
                if (iNum == 2) return ("ยี่" + _wordDigit[iDigit]); // ยี่สิบ
            }
            if (iDigit == 0)
            {
                if (numAtDigitTen > 0)
                {
                    // หลักหน่วย
                    if (iNum == 1) return ("เอ็ด");  // อ่านค่าเอ็ด
                }
            }
            return (_wordNum[iNum] + _wordDigit[iDigit]);
        }	/*}}}*/

        public static string reading_number(string sNumber) {	/*{{{
			*/
            // Check And Prepare Number
            string sReverse = _reverse_number_string(sNumber);
            while (sReverse.Length % 6 != 0) sReverse += "0";

            // Process Each 6Block
            string sReading = "";
            int iNum = 0;
            int numAtTen = 0;
            bool isValueIsStart = false;
            while (sReverse.Length > 0)
            {
                string s6Block = sReverse.Substring(sReverse.Length - 6, 6);
                if (sReverse.Length > 6)
                    sReverse = sReverse.Substring(0, sReverse.Length - 6);
                else
                    sReverse = "";

                numAtTen = int.Parse(s6Block.Substring(1, 1));
                for (int i = 5; i >= 0; --i)
                {
                    iNum = int.Parse(s6Block.Substring(i, 1));
                    if (!isValueIsStart && iNum > 0) isValueIsStart = true;
                    if (_wordNum[iNum] != "")
                    {
                        sReading += _combie_number_and_digit(iNum, i, numAtTen);
                    }
                }
                if (sReverse.Length > 0 && isValueIsStart) sReading += "ล้าน";
            }

            return (sReading);
        }	/*}}}*/
        public static string reading_money(string sMoney) {	/*{{{
			*/
            string sIntPart = "";
            string sFixPart = "";
            if (sMoney.Contains("."))
            {
                string[] arrPart = sMoney.Split(new string[] { "." }, 2, StringSplitOptions.None);
                sIntPart = arrPart[0];
                sFixPart = (arrPart[1].Trim() + "00").Substring(0, 2);
            }
            else
                sIntPart = sMoney;

            string AllReading = "";
            string ReadingIntPart = reading_number(sIntPart);
            string ReadingFixPart = reading_number(sFixPart);
            if (ReadingIntPart.Length > 0) AllReading += ReadingIntPart + "บาท";
            if (ReadingFixPart.Length > 0) AllReading += ReadingFixPart + "สตางค์";
            return (AllReading);
        }	/*}}}*/

    }
}
