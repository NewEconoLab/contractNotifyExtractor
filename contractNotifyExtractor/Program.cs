using System;
//using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using contractNotifyExtractor.lib;

namespace contractNotifyExtractor
{
    class Program
    {
        static mongodbHelper mh = new mongodbHelper();

        //static private IConfigurationRoot getConfig(string fileName)
        //{
        //    IConfigurationRoot config = new ConfigurationBuilder()
        //        .AddInMemoryCollection()    //将配置文件的数据加载到内存中
        //        .SetBasePath(System.IO.Directory.GetCurrentDirectory())   //指定配置文件所在的目录
        //        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)  //指定加载的配置文件
        //        .Build();    //编译成对象 

        //    return config;
        //}

        static private JObject getConfig(string fileName)
        {
           return JObject.Parse(File.ReadAllText(fileName));
        }

        static private bool extractNotifyInfo(string taskID,JObject task,string readConnStr, string readDBname,string writeConnStr, string writeDBname) {
            bool isNewDataExist = false;

            string taskContractHash = task["contractHash"].ToString();
            string taskNotifyDisplayName = task["notifyDisplayName"].ToString();
            JArray JAtaskNotifyStructure = (JArray)task["notifyStructure"];
            Int64 lastBlockindex = mh.getContractStorageHeight(writeConnStr, writeDBname, taskContractHash);

            #region
            //JObject contractStorageHeightTXID = mh.getContractStorageHeight(writeConnStr, writeDBname,contractHash);
            //Int64 workHeight = Int64.Parse(contractStorageHeightTXID["lastBlockindex"].ToString());

            //string queryBson = string.Empty;

            //if (workHeight == -1)//没有入过库，下一个
            //{
            //    queryBson = "{'notifications.contract':'" + contractHash + "',blockindex:{'$gte':" + workHeight + "}}";
            //}
            //else
            //{
            //    string workTXID = contractStorageHeightTXID["lastTXID"].ToString();
            //    bool isStoragedInBlock = mh.checkIsStoragedInBlock(readConnStr, readDBname, workHeight, workTXID);

            //    if (isStoragedInBlock)//入库且已处理上一个块所有TXID，下一个
            //    {
            //        queryBson = "{'notifications.contract':'" + contractHash + "',blockindex:{'$gte':" + workHeight + "}}";
            //    }
            //    else//入库但未处理上一个块所有TXID，重新处理本块
            //    {
            //        queryBson = "{'notifications.contract':'" + contractHash + "',blockindex:" + workHeight + "}";
            //    }
            //}
            #endregion

            JArray JA = mh.GetData(readConnStr, readDBname, "notify", "{'notifications.contract':'" + taskContractHash + "',blockindex:{'$gt':" + lastBlockindex + "}}", "{blockindex:1,txid:1}", 1);
            if (JA.Count > 0)
            {
                //获取已处理块高度的后一个有指定notify的块高度，并以此高度获取所有指定notify
                Int64 doBlockHeight = Int64.Parse(JA[0]["blockindex"].ToString());
                JA = mh.GetData(readConnStr, readDBname, "notify", "{blockindex:" + doBlockHeight + "}", "{txid:1}");

                //按块处理所有关联数据,处理好一个块所有数据一起写入
                JArray JAinsertData = new JArray();
                foreach (JObject J in JA)
                {
                    Int64 blockindex = (Int64)J["blockindex"];
                    string txid = (string)J["txid"];
                    JArray JAnotifications = (JArray)J["notifications"];

                    int n = 0;//标记notify在一个tx里的序号
                    foreach (JObject Jnotify in JAnotifications)
                    {
                        JArray JAstate = (JArray)Jnotify["state"]["value"];

                        string dataContractHash = Jnotify["contract"].ToString();
                        string dataNotifyDisplayName = JAstate[0]["value"].ToString().Hexstring2String();
                        if (dataContractHash == taskContractHash && dataNotifyDisplayName == taskNotifyDisplayName)
                        {
                            //Jnotify.Add("blockindex", blockindex);
                            //Jnotify.Add("txid", txid);
                            //Jnotify.Add("n", n);
                            

                            //存储解析后数据
                            JObject JnotifyInfo = new JObject();
                            //下列三项组合为唯一索引
                            JnotifyInfo.Add("blockindex", blockindex);
                            JnotifyInfo.Add("txid", txid);
                            JnotifyInfo.Add("n", n);
                            //解析数据
                            //JnotifyInfo.Add("infoDisplayName", dataNotifyDisplayName);

                            int i = 0;
                            foreach (JObject Jvalue in JAstate)
                            {
                                string notifyType = Jvalue["type"].ToString();
                                                               

                                if (notifyType == "Array")//数组类一层展开
                                {
                                    JArray JAarrayValue = (JArray)Jvalue["value"];

                                    int j = 0;
                                    foreach (JObject JvalueLevel2 in JAarrayValue) {
                                        string notifyTypeLevel2 = JvalueLevel2["type"].ToString();
                                        string notifyValueLevel2 = JvalueLevel2["value"].ToString();
                                        JObject JtaskEscapeInfo = (JObject)((JObject)JAtaskNotifyStructure[i])["arrayData"][j];

                                        //如果处理失败则不处理（用原值）
                                        notifyValueLevel2 = escapeHelper.contractDataEscap(notifyTypeLevel2, notifyValueLevel2, JtaskEscapeInfo);

                                        string taskName = JtaskEscapeInfo["name"].ToString();
                                        JnotifyInfo.Add(taskName, notifyValueLevel2);

                                        j++;
                                    }
                                }
                                else
                                {
                                    string notifyValue = Jvalue["value"].ToString();
                                    JObject JtaskEscapeInfo = (JObject)JAtaskNotifyStructure[i];

                                    //如果处理失败则不处理（用原值）
                                    notifyValue = escapeHelper.contractDataEscap(notifyType, notifyValue, JtaskEscapeInfo);

                                    string taskName = JtaskEscapeInfo["name"].ToString();
                                    JnotifyInfo.Add(taskName, notifyValue);
                                }                                                                                                  
                                i++;
                            }

                            //记录原数据
                            JnotifyInfo.Add("state", Jnotify["state"]);

                            //加入需要写入的数据的组
                            JAinsertData.Add(JnotifyInfo);
                        }

                        n++;
                    }
                }

                try
                {
                    var a = JAinsertData.ToString();
                    var queryStr = "{blockindex:" + JAinsertData[0]["blockindex"] + ",txid:'" + JAinsertData[0]["txid"] + "',n:" + JAinsertData[0]["n"] + "}";
                    //没有入库才入库
                    if (mh.GetData(writeConnStr, writeDBname, taskContractHash, queryStr).Count == 0)
                    {
                        //批量写入一个块的所有定义notify
                        mh.InsertDataByJarray(writeConnStr, writeDBname, taskContractHash, JAinsertData);
                        //自动添加必要索引(会自动判断索引是否存在，不存在才添加)
                        //mh.setIndex(writeConnStr, writeDBname, taskContractHash, "{'blockindex':1}", "i_blockindex");
                        mh.setIndex(writeConnStr, writeDBname, taskContractHash, "{'blockindex':1,'txid':1,'n':1}", "i_blockindex_txid_n", true);
                        //更新处理高度
                        mh.setContractStorageHeight(writeConnStr, writeDBname, taskContractHash, doBlockHeight);
                    }
                    else
                    {
                        Console.WriteLine("任务ID：" + taskID + "当前高度已入库，自动跳过当前高度" + doBlockHeight);
                        //更新处理高度
                        mh.setContractStorageHeight(writeConnStr, writeDBname, taskContractHash, doBlockHeight);
                    }
                }
                catch(Exception ex) {
                    var e = ex.Message;
                }

                isNewDataExist = true;

                Console.WriteLine("任务ID：" + taskID + "已处理到高度：" + doBlockHeight);
            }
            else {
                Console.WriteLine("任务ID：" + taskID + "已入库所有数据，等待新数据产生");
            }

            return isNewDataExist;
        }

        static void Main(string[] args)
        {
            //积极入库时间间隔
            int sleepTime_positive = 100;
            //消极入库时间间隔
            int sleepTime_passive = 1000;

            JObject appConfig = getConfig("appsettings.json");
            JObject taskListConfig = getConfig("extractTaskList.json");

            int i = 1;//任务号
            foreach (JObject task in (JArray)taskListConfig["taskList"])
            {
                string taskID = i.ToString("000000");

                string taskNetType = task["netType"].ToString();
                //var queryReadDBinfo = from mci
                //                      in appConfig["mongodbConnInfo"].Children()
                //                      where (string)mci["netType"] == taskNetType
                //                      select mci;
                var queryReadDBinfo = appConfig["mongodbConnInfo"].Children().Where(mci => (string)mci["netType"] == taskNetType);
                string readConnStr = queryReadDBinfo.First()["readConnStr"].ToString();
                string readDBname = queryReadDBinfo.First()["readDatabase"].ToString();
                string writeConnStr = queryReadDBinfo.First()["writeConnStr"].ToString();
                string writeDBname = queryReadDBinfo.First()["writeDatebase"].ToString();

                Task task_extract = new Task(() => {
                    Console.WriteLine(task["memo"] + "_任务开始");
                    Console.WriteLine("本任务读-数据库连接：" + readConnStr);
                    Console.WriteLine("本任务读-数据库：" + readDBname);
                    Console.WriteLine("本任务写-数据库连接：" + writeConnStr);
                    Console.WriteLine("本任务写-据库连：" + writeDBname);               
                    Console.WriteLine("本任务网络类型：" + taskNetType);
                    Console.WriteLine("任务ID：" + taskID);
                    while (true)
                    {
                        //Console.WriteLine(taskID);
                        bool isNewDataExist = extractNotifyInfo(taskID, task , readConnStr, readDBname, writeConnStr, writeDBname);
                        //本次操作，是否处理了新数据。处理新数据就积极入库，没有新数据就消极入库
                        if (isNewDataExist)
                        {
                            Thread.Sleep(sleepTime_positive);
                        }
                        else
                        {
                            Thread.Sleep(sleepTime_passive);
                        }
                        
                    }
                });
                task_extract.Start();

                i++;
            }

            Console.ReadKey();
        }
    }
}
