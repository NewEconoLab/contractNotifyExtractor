using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace contractNotifyExtractor.lib
{
    public static class escapeHelper
    {
        public static string contractDataEscap(string type, string value, JObject taskEscapeInfo)
        {
            string result = value;

            string escap = (string)taskEscapeInfo["escape"];
            int decimals = 0;
            switch (escap)
            {
                case "String"://处理成字符串
                    if (isZeroEmpty(type, value))
                    { result = "";}
                    else
                    {
                        try
                        {
                            result = value.Hexstring2String();
                        }
                        catch { }
                    }

                    break;
                case "Address"://处理成地址
                    if (isZeroEmpty(type, value))
                    { result = ""; }
                    else
                    {
                        try
                        {
                            result = ThinNeo.Helper.GetAddressFromScriptHash2(value.HexString2Bytes());
                        }
                        catch { }
                    }

                    break;
                case "BigInteger"://处理成大整数                  
                    if (taskEscapeInfo["decimals"] != null) decimals = int.Parse(taskEscapeInfo["decimals"].ToString());

                    if (isZeroEmpty(type, value))
                    {
                        result = "0";
                    }
                    else
                    {
                        try
                        {
                            if (type == "ByteArray")
                            {
                                result = value.getNumStrFromHexStr(decimals);
                            }
                            else//Integer
                            {
                                result = value.getNumStrFromIntStr(decimals);
                            }
                        }
                        catch { }
                    }

                    break;
                default://未定义，默认是hexString，合约中是byte[]，尝试逆转操作
                    if (isZeroEmpty(type, value))
                    {
                        result = "";
                    }
                    else
                    {
                        try
                        {
                            result = "0x" + value.hexstringReverse();
                        }
                        catch { }
                    }

                    break;
            }

            return result;
        }

        //为0.为空，为false再NEOvm处理中，有时通知输出类型会变成各种奇怪东西，此处统一判断
        public static bool isZeroEmpty(string type, string value)
        {
            if (type == "Boolean" && value == "False") return true;
            if (type == "ByteArray" && value == "") return true;

            return false;
        }

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

        public static string hexstringReverse(this string hexstr)
        {
            return hexstr.HexString2Bytes().Reverse().ToArray().BytesToHexString();
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

        public static string BytesToHexString(this byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var b in data)
            {
                sb.Append(b.ToString("x02"));
            }
            return sb.ToString();
        }
    }
}
