/* copyright @ anyplots.com, All right rights, visit https://anyplots.com/ for more information */
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Net.Sockets;
namespace anyplots
{
    static class ApiController
    {
        public static int ClientBandwidth = 0;
        public static string ProjectToken = "", ClientToken = "";
        public static string Ext { get { return ClientToken.Substring(0, 8); } }
        static ApiController()
        {
            ServicePointManager.DefaultConnectionLimit = 10000;
            ServicePointManager.MaxServicePoints = 10000;
            ThreadPool.SetMaxThreads(1024, 1024);
        }
        static string DownloadData(string address, string postdata, int timeout)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(address);
            req.Timeout = timeout;
            req.ProtocolVersion = HttpVersion.Version11;
            req.UserAgent = "CloudChiaPlotter";
            req.AllowReadStreamBuffering = true;
            req.KeepAlive = true;
            req.Method = "POST";
            ServicePoint point = req.ServicePoint;
            req.AutomaticDecompression = DecompressionMethods.GZip;
            if (!string.IsNullOrEmpty(postdata))
            {
                req.ContentType = "application/json; charset=UTF-8";
                req.Accept = "application/json";
                using (Stream stream = req.GetRequestStream())
                {
                    Byte[] bytes_ = Encoding.UTF8.GetBytes(postdata);
                    stream.Write(bytes_, 0, bytes_.Length);
                }
            }
            IAsyncResult ar = req.BeginGetResponse(null, null);
            if (ar.AsyncWaitHandle.WaitOne(timeout))
            {
                using (HttpWebResponse res = (HttpWebResponse)req.EndGetResponse(ar))
                {
                    using (StreamReader stream = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
                    {
                        return stream.ReadToEnd();
                    }
                }
            }
            else
            {
                throw new Exception("request timeout:" + timeout);
            }
        }
        static string HttpRequest(string url, Object result)
        {
            Byte[] buffer = new byte[1024 * 10];
            string postdata = "";
            if (result != null)
            {
                postdata = JsonConvert.SerializeObject(result);
                postdata = JsonConvert.ToString(postdata);
            }
            return DownloadData(url, postdata, 30000);
        }
        class LoginArg
        {
            public string client_token = "";
        }
        public static string Login(string project_token, string client_token)
        {
            string res = HttpRequest("https://anyplots.com/api/v1/client/login", new { project_token = project_token, client_token= client_token, bandwidth = ClientBandwidth, version = Program.Version });
            LoginArg args = JsonConvert.DeserializeObject<LoginArg>(res) ?? new LoginArg();
            return args.client_token;
        }
        class GetPlotArg
        {
            public int id = 0;
            public Int64 size = 0;
            public string name = "";
            public string url = "";
        }
        public static string GetPlot(ref int id, ref long filesize, ref string filename)
        {
            string res = HttpRequest("https://anyplots.com/api/v1/client/get_plot", new { token = ClientToken, id = id, size = filesize, name = filename });
            GetPlotArg args = JsonConvert.DeserializeObject<GetPlotArg>(res) ?? new GetPlotArg();
            id = args.id;
            filesize = args.size;
            filename = args.name;
            return args.url;
        }
        class StatsRes
        {
            public int action = 128;
            public int status = 0;
            public string result;
        }
        public static int Stats(int id, float percent, float speed, int pingmin, int pingavg, int pinglos)
        {
            int freespace, disks;
            freespace = Program.GetFreeSpace(out disks);
            string res = HttpRequest("https://anyplots.com/api/v1/client/stats",
                new { token = ClientToken, id = id, percent = percent, speed = speed, pingmin=pingmin, pingavg, pinglos = pinglos, freespace = freespace, disks = disks });
            StatsRes ret = JsonConvert.DeserializeObject<StatsRes>(res) ?? new StatsRes();
            return ret.action;
        }
        public static void Logging(bool server, int fileid, string logs)
        {
            Console.WriteLine("\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + logs);
            if (server)
            {
                try
                {
                    HttpRequest("https://anyplots.com/api/v1/client/logging", new { token = ClientToken, id = fileid, data = logs });
                }
                catch (Exception ex)
                {
                    try
                    {
                        HttpRequest("https://anyplots.com/api/v1/client/logging", new { token = ClientToken, id = fileid, data = logs });
                    }
                    catch { }
                }
            }
        }
    }

}
