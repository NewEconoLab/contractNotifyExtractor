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

        //构造0合约hash（代表空）
        public string getZero20()
        {
            return "0x" + new byte[20].BytesToHexString();
        }

        //目标处理的块总数
        public Int64 getCliBlockCount()
        {
            string res = hh.getRPCdataByPost(neoCliJsonRPCUrl, "getblockcount", new object[0]);

            return (res != string.Empty) ? Int64.Parse(res) : -1;
        }

        //根据块索引获取块数据
        public string getCliBlockData(Int64 blockIndex) {
            string res = hh.getRPCdataByPost(neoCliJsonRPCUrl, "getblock", new object[]{ blockIndex,1});

            return res;
        }

        //根据Txid获取notify数据
        public string getCliLogData(string txid) {
            string res = hh.getRPCdataByPost(neoCliJsonRPCUrl, "getapplicationlog", new object[]{ txid });

            return res;
        }

        //检查是否有未入库的块
        public bool checkNewNotify(string notifyCollName)
        {
            Int64 cliBlockHeight = getCliBlockCount() - 1;
            Int64 dbNotifyBlockHeight = mh.getContractStorageHeight(mongodbConnStr, mongodbDatabase, getZero20(), notifyCollName);
            return cliBlockHeight > dbNotifyBlockHeight ? true : false;
        }

        //根据表名存储notify数据（自动增加高度）
        public void storageNotifyByBlockIndex(string notifyCollName)
        {
            JArray notifyJA = new JArray();

            Int64 dbNotifyBlockHeight = mh.getContractStorageHeight(mongodbConnStr, mongodbDatabase, getZero20(), notifyCollName);
            JObject blockDataJ = JObject.Parse(getCliBlockData(dbNotifyBlockHeight++));
            foreach (JObject tx in (JArray)blockDataJ["tx"])
            {
                string txid = tx["txid"].ToString();
                JObject notifyJ = JObject.Parse(getCliLogData(txid));
                notifyJ.Add("blockindex", dbNotifyBlockHeight);
                notifyJA.Add(notifyJ);
            }
            //批量插入一个块的所有数据
            mh.InsertDataByJarray(mongodbConnStr, mongodbDatabase, notifyCollName, notifyJA);
            //设置索引
            mh.setIndex(mongodbConnStr, mongodbDatabase, notifyCollName, "{'blockindex’:1,'txid':1}", "i_blockindex_txid", true);
            //更新处理高度
            mh.setContractStorageHeight(mongodbConnStr, mongodbDatabase, getZero20(), notifyCollName, dbNotifyBlockHeight);
        }
    }
}
