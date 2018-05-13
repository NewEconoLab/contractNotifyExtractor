using System;
using System.Collections.Generic;
using System.Text;

namespace contractNotifyExtractor.lib
{
    public static class escapeHelper
    {
        public static string String2Hexstring(this string str)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(str);
            string byteStr = string.Empty;
            foreach (byte b in byteArray)
            {
                byteStr += Convert.ToString(b, 16);
            }

            return byteStr;
        }

        public static string Hexstring2String(this string hexstr)
        {
            List<byte> byteArray = new List<byte>();

            for (int i = 0; i < hexstr.Length; i = i + 2)
            {
                string s = hexstr.Substring(i, 2);
                byteArray.Add(Convert.ToByte(s, 16));
            }

            string str = Encoding.UTF8.GetString(byteArray.ToArray());

            return str;
        }

        public static string BytesToHexString(this byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var b in data)
            {
                sb.Append(b.ToString("x02"));
            }
            return sb.ToString();
        }

        public static string FromHexString(this string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return Encoding.Unicode.GetString(bytes); // returns: "Hello world" for "48656C6C6F20776F726C64"
        }

        public static string formatHexStr(this string hexStr)
        {
            string result = hexStr.ToLower();

            if (result.IndexOf("0x") == -1)
                result = "0x" + result;

            return result;
        }

        public static byte[] HexString2Bytes(this string str)
        {
            byte[] b = new byte[str.Length / 2];
            for (var i = 0; i < b.Length; i++)
            {
                b[i] = byte.Parse(str.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return b;
        }

    }
}
