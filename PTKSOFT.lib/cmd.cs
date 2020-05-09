using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PTKSOFT.Lib
{
    public class CMD
    {
        // Private Variables
        private bool _valid = false;
        private string _name = "";
        private string[] _param = new string[0];
        private string _rawLine = "";
        private string _separator = "/";

        // Public Properties
        public bool valid { get { return (_valid); } }
        public string name { get { return (_name); } }
        public string[] param { get { return (_param); } }
        public string rawLine { get { return (_rawLine); } }
        public string separator { get { return (_separator); } }

        // Constructor        
        public CMD(string line2Parse) : this(line2Parse, "/", 256) { }
        public CMD(string line2Parse, int paramLimit) : this(line2Parse, "/", paramLimit) { }
        public CMD(string line2Parse, string separator, int paramLimit) {	/*{{{
			Create object and parse from line string
			then format into correct parts
			*/
            if (separator.Length < 1) return;
            if (paramLimit < 1) return;
            if (line2Parse.Length < 1) return;
            if (!line2Parse.StartsWith(separator)) return;

            _rawLine = line2Parse;
            _separator = separator;
            string[] arrSep = new string[] { separator };
            string[] arrResult = line2Parse.Split(arrSep, (2 + paramLimit), StringSplitOptions.None);
            _name = arrResult[1];
            int paramLength = arrResult.Length - 2;
            if (paramLength > 0)
            {
                // Have Parameter
                _param = new string[paramLength];
                for (int i = 0; i < paramLength; i++)
                {
                    _param[i] = arrResult[2 + i];
                }
            }
            _valid = true;
        }	/*}}}*/
    }
}
