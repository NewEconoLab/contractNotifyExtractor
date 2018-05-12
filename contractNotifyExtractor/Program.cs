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

        static private void extractNotifyInfo(string taskID,JObject task,string dbConnStr,string readDBname,string writeDBname) {
            string contractHash = task["contractHash"].ToString();
            int startHeight = 0;

            JArray JA = mh.GetData(dbConnStr, readDBname, "notify", "{'notifications.contract':'" + contractHash + "',blockindex:{'$gte':" + startHeight + "}}", "{blockindex:1,txid:1}",1);
            Int64 blockindex = (Int64)JA[0]["blockindex"];
            string txid = (string)JA[0]["txid"];
            JA = (JArray)JA[0]["notifications"];
            int i = 0;
            JArray JAtemp = new JArray();
            foreach (JObject J in JA)
            {
                if (J["contract"].ToString() == contractHash)
                {
                    J.Add("blockindex", blockindex);
                    J.Add("txid", txid);
                    J.Add("n", i);
                    JAtemp.Add(J);
                }             

                i++;
            }

            mh.InsertDataByJarray(dbConnStr, writeDBname, contractHash, JAtemp);

            var a = JAtemp.ToString();

            Console.WriteLine("任务ID：" + taskID + "已处理到高度：" + startHeight);
        }

        static void Main(string[] args)
        {
            

            int sleepTime = 1000;
            JObject appConfig = getConfig("appsettings.json");
            JObject taskListConfig = getConfig("extractTaskList.json");

            int i = 1;
            foreach (JObject task in (JArray)taskListConfig["taskList"])
            {
                string taskID = i.ToString("000000");

                string taskNetType = task["netType"].ToString();
                //var queryReadDBinfo = from mci
                //                      in appConfig["mongodbConnInfo"].Children()
                //                      where (string)mci["netType"] == taskNetType
                //                      select mci;
                var queryReadDBinfo = appConfig["mongodbConnInfo"].Children().Where(mci => (string)mci["netType"] == taskNetType);
                string dbConnStr = queryReadDBinfo.First()["mongodbConnStr"].ToString();
                string readDBname = queryReadDBinfo.First()["readDatabase"].ToString();
                string writeDBname = queryReadDBinfo.First()["writeDatebase"].ToString();

                Task task_extract = new Task(() => {

                    Console.WriteLine(task["memo"] + "_任务开始");
                    Console.WriteLine("本任务数据库连接：" + dbConnStr);
                    Console.WriteLine("本任务读-数据库：" + readDBname);
                    Console.WriteLine("本任务写-据库连：" + writeDBname);
                    Console.WriteLine("本任务网络类型：" + taskNetType);
                    Console.WriteLine("任务ID：" + taskID);
                    while (true)
                    {
                        Console.WriteLine(taskID);
                        extractNotifyInfo(taskID, task , dbConnStr, readDBname, writeDBname);
                        Thread.Sleep(sleepTime);
                    }
                });
                task_extract.Start();

                i++;
            }

            Console.ReadKey();
        }
    }
}
