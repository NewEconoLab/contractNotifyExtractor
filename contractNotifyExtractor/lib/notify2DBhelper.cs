using System;
using System.Collections.Generic;
using System.Text;
using contractNotifyExtractor.lib;
using Newtonsoft.Json.Linq;

namespace contractNotifyExtractor.lib
{
    class notify2DBhelper
    {
        httpHelper hh = new httpHelper();
        mongodbHelper mh = new mongodbHelper();

        public string neoCliJsonRPCUrl = "";
        public string mongodbConnStr = "";
        public string mongodbDatabase = "";

        public notify2DBhelper(string cliUrl,string mongoConnStr,string mongoDBname) {
            neoCliJsonRPCUrl = cliUrl;
            mongodbConnStr = mongoConnStr;
            mongodbDatabase = mongoDBname;
        }

        public string getZero20()
        {
            return "0x" + new byte[20].BytesToHexString();
        }

        public Int64 getCliBlockCount()
        {
            string res = hh.getRPCdataByPost(neoCliJsonRPCUrl, "getblockcount", new object[0]);

            return (res != string.Empty) ? Int64.Parse(res) : -1;
        }

        public string getCliBlockData(Int64 blockIndex) {
            string res = hh.getRPCdataByPost(neoCliJsonRPCUrl, "getblock", new object[]{ blockIndex,1});

            return res;
        }

        public string getCliLogData(string txid) {
            string res = hh.getRPCdataByPost(neoCliJsonRPCUrl, "getapplicationlog", new object[]{ txid });

            return res;
        }

        public bool checkNewNotify(string notifyCollName)
        {
            Int64 cliBlockHeight = getCliBlockCount() - 1;
            Int64 dbNotifyBlockHeight = mh.getContractStorageHeight(mongodbConnStr, mongodbDatabase, getZero20(), notifyCollName);
            return cliBlockHeight > dbNotifyBlockHeight ? true : false;
        }
    }
}
