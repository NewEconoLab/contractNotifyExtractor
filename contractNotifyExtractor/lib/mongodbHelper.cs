using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace contractNotifyExtractor.lib
{
    public class mongodbHelper
    {
        public JArray GetData(string mongodbConnStr, string mongodbDatabase, string coll, string findFliter, string sortFliter,int limit = 0)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            List<BsonDocument> query = new List<BsonDocument>();
            if (limit == 0)
            {
                query = collection.Find(BsonDocument.Parse(findFliter)).Sort(sortFliter).ToList();
            }
            else
            {
                query = collection.Find(BsonDocument.Parse(findFliter)).Sort(sortFliter).Limit(limit).ToList();
            }
            client = null;

            if (query.Count > 0)
            {
                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                JArray JA = JArray.Parse(query.ToJson(jsonWriterSettings));
                foreach (JObject j in JA)
                {
                    j.Remove("_id");
                }
                return JA;
            }
            else { return new JArray(); }
        }

        //批量写入数据
        public void InsertDataByJarray(string mongodbConnStr, string mongodbDatabase, string coll, JArray Jdata)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            List<BsonDocument> bsons = new List<BsonDocument>();
            foreach (JObject J in Jdata)
            {
                string strData = Newtonsoft.Json.JsonConvert.SerializeObject(J);
                BsonDocument bson = BsonDocument.Parse(strData);
                bsons.Add(bson);
            }

            //collection.InsertOne(bsons[0]);
            collection.InsertMany(bsons.ToArray());

            client = null;
        }

        public Int64 getContractStorageHeight(string mongodbConnStr, string mongodbDatabase, string contractHash)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>("contractStorageHeight");

            var queryBson = BsonDocument.Parse("{contractHash:'" + contractHash + "'}");
            var query = collection.Find(queryBson).ToList();
            client = null;

            if (query.Count == 0)
            {
                return -1;
            }
            else
            {
                return Int64.Parse(query[0]["lastBlockindex"].ToString());
            }
        }

        public void setContractStorageHeight(string mongodbConnStr, string mongodbDatabase, string contractHash, Int64 lastBlockindex)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>("contractStorageHeight");

            var setBson = BsonDocument.Parse("{contractHash:'" + contractHash + "',lastBlockindex:" + lastBlockindex + "}");

            var queryBson = BsonDocument.Parse("{contractHash:'" + contractHash + "'}");
            var query = collection.Find(queryBson).ToList();
            if (query.Count == 0)
            {
                collection.InsertOne(setBson);
            }
            else
            {
                collection.ReplaceOne(queryBson, setBson);
            }

            client = null;
        }

        //public bool checkIsStoragedInBlock(string mongodbConnStr, string mongodbDatabase,Int64 blockindex,string txid)
        //{
        //    var client = new MongoClient(mongodbConnStr);
        //    var database = client.GetDatabase(mongodbDatabase);
        //    var collection = database.GetCollection<BsonDocument>("notify");

        //    var queryBson = BsonDocument.Parse("{blockindex:" + blockindex + "}");
        //    var query = collection.Find(queryBson).Sort("{txid:-1}").Limit(1).ToList();
        //    client = null;

        //    if (query.Count == 0)
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        if (query[0]["txid"].AsString == txid)
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //}
    }
}
