/* copyright @ anyplots.com, All right rights, visit https://anyplots.com/ for more information */
using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
namespace anyplots
{
    class Program
    {
        static string ver = null;
        public static string Version
        {
            get
            {
                if (ver == null)
                {
                    try
                    {
                        string version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).LegalCopyright;
                        int p1 = version.IndexOf('(');
                        int p2 = version.IndexOf(')');
                        ver = version.Substring(p1 + 1, p2 - p1 - 1);
                    }
                    catch { ver = "-"; }
                }
                return ver;
            }
        }
        static int nextdir = 0;
        public static List<string> Dirs = new List<string>();
        public static string NextDir
        {
            get { return Dirs[nextdir++ % Dirs.Count]; }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            try
            {
                if (args.ExceptionObject != null)
                {
                    Exception e = (Exception)args.ExceptionObject;
                    ApiController.Logging(false,0,"Error caught : " + e.Message);
                    ApiController.Logging(true, 0, "MyHandler1:" + args.IsTerminating + "\r\n" + e.Message + "\r\n" + e.StackTrace);
                }
                else
                {

                    ApiController.Logging(true, 0, "MyHandler2:" + args.IsTerminating);
                }
            }
            catch { }
        }
        
        static FileStream fslock = null;
        static FileStream LockFile(string file)
        {
            try
            {
                FileStream fslock = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                try
                {
                    fslock.Write(System.Text.Encoding.ASCII.GetBytes("lock"), 0, 4);
                    return fslock;
                }
                catch { fslock.Close(); }
            }
            catch { }
            return null;
        }
        static void InitClient()
        {
            foreach (string file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory,"*.client"))
            {
                FileStream lock_ = LockFile(file) ;
                if (lock_ != null)
                {
                    string token = Path.GetFileNameWithoutExtension(file);
                    if(token != ApiController.Login(ApiController.ProjectToken, token))
                    {
                        lock_.Close();
                        File.Delete(file);
                    }
                    else
                    {
                        fslock = lock_;
                        ApiController.ClientToken = token;
                        return;
                    }
                }
                Thread.Sleep(5000);
            }
            string newtoken_ = ApiController.Login(ApiController.ProjectToken, "");
            if (newtoken_.Length == 40)
            {
                FileStream lock_ = LockFile(AppDomain.CurrentDomain.BaseDirectory + newtoken_ + ".client");
                if (lock_ == null)
                {
                    throw new Exception("unkown error");
                }
                fslock = lock_;
                ApiController.ClientToken = newtoken_;
            }
            else
            {
                throw new Exception("login failed, unkown token: " + newtoken_);
            }
        }
        static bool IsWindows
        {
            get { return AppDomain.CurrentDomain.BaseDirectory.Contains('\\'); }
        }
        static string NormalizeDirPath(string path)
        {
            if (IsWindows)
            {
                return path.TrimEnd(new char[] { '/', '\\' }) + "\\";
            }
            else
            {
                return path.TrimEnd(new char[] { '/', '\\' }) + "/";
            }
        }
        static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            }
            catch { }
            if (args.Length < 4)
            {
                ApiController.Logging(false,0,"Usage:");
                if (IsWindows)
                {
                    ApiController.Logging(false,0,"use the command like: .\\CloudChiaPlotter.exe -p {project_token} -d d:\\;e:\\;f:\\;g:\\");
                }
                else
                {
                    ApiController.Logging(false,0,"use the command like: ./CloudChiaPlotter -p {project_token} -d /mnt/d;/mnt/e/;/mnt/f;/mnt/g");
                }
                Console.ReadLine();
                return;
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-p":
                            ApiController.ProjectToken = args[++i];
                            if (ApiController.ProjectToken.Length != 40)
                            {
                                ApiController.Logging(false,0,"project_token is incorrect:" + ApiController.ProjectToken);
                            }
                            break;
                        case "-d":
                            Dictionary<string, string> filter = new Dictionary<string, string>();
                            foreach (string item in args[++i].Split(';'))
                            {
                                string dir = NormalizeDirPath(item);
                                if (Directory.Exists(dir))
                                {
                                    if (!filter.ContainsKey(dir.ToLower()))
                                    {
                                        if (TestDir(dir))
                                        {
                                            filter.Add(dir.ToLower(), null);
                                            Dirs.Add(dir);
                                            ApiController.Logging(false,0,"add dir:" + dir);
                                        }
                                        else
                                        {
                                            ApiController.Logging(false,0,"test dir failed, skip  dir:" + dir);
                                        }
                                    }
                                    else
                                    {
                                        ApiController.Logging(false,0,"skip duplicate dir:" + dir);
                                    }
                                }
                                else
                                {
                                    ApiController.Logging(false,0,"unkown dir:" + dir);
                                }
                            }
                            break;
                        default:
                            ApiController.Logging(false,0,"unkown argument:" + args[i]);
                            break;
                    }
                }
            }
            InitClient();
            ApiController.Logging(false,0,"client token:" + ApiController.ClientToken);
            Run();
        }
        static long GetFreeSpace(string path)
        {
            try
            {
                return new DriveInfo(path).AvailableFreeSpace;
            }
            catch (Exception ex)
            {
                ApiController.Logging(false,0,ex.Message);
            }
            return 0;
        }
        public static int GetFreeSpace(out int cnt)
        {
            int total = 0;
            cnt = 0;
            foreach (string dir in Program.Dirs)
            {
                try
                {
                    cnt++;
                    int ret = (int)(GetFreeSpace(dir) >> 30);
                    if (ret > 1000000000) { ret = 0; } // error
                    total += ret;
                }
                catch { }
            }
            return total;
        }
        static bool TestDir(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path + "testfile", FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    try
                    {
                        fs.SetLength(100L * 1024 * 1024 * 1024);
                    }
                    catch (Exception ex)
                    {
                        ApiController.Logging(false,0,ex.Message + ex.StackTrace);
                        return false;
                    }
                }
                File.Delete(path + "testfile");
            }
            catch (Exception ex)
            {
                ApiController.Logging(false,0,ex.Message + ex.StackTrace);
                return false;
            }
            return true;
        }
        static Int64 GetFileSize(string file)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return fs.Length;
            }
        }
        static string GetUncompletedPlot(out int fileid, out Int64 filesize, out string filename)
        {
            fileid = 0;
            filesize = 0;
            filename = "";
            foreach (string dir in Program.Dirs)
            {
                try
                {
                    foreach (string file in Directory.GetFiles(dir, "*.data" + ApiController.Ext))
                    {
                        filesize = GetFileSize(file);
                        string[] parts = Path.GetFileNameWithoutExtension(file).Split('.');
                        fileid = int.Parse(Path.GetFileNameWithoutExtension(parts[0]));
                        filename = parts[1] + ".plot";
                        return file;
                    }
                }
                catch (Exception ex)
                {
                    fileid = 0;
                    filesize = 0;
                    ApiController.Logging(true, 0, "GetUncompletedPlot: " + ex.Message + "\r\n" + dir);
                }
            }
            return "";
        }
        static void Clear(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    ApiController.Logging(false, 0, "Delete: " + path);
                    File.Delete(path);
                }
                catch { }
            }
            path = path.Replace(".data" + ApiController.ClientToken, ".conf" + ApiController.ClientToken);
            if (File.Exists(path))
            {
                try
                {
                    ApiController.Logging(false, 0, "Delete: " + path);
                    File.Delete(path);
                }
                catch { }
            }
        }
        static PlotWriter GetPlotWriter(int fileid, string filename, long filesize)
        {
            for (int i = 0; i < Program.Dirs.Count; i++)
            {
                string dir = Program.NextDir;
                try
                {
                    if (GetFreeSpace(dir) > filesize || File.Exists(dir + fileid + "." + filename + ".data" + ApiController.Ext))
                    {
                        return new PlotWriter(dir, fileid.ToString(), filename, filesize);
                    }
                    else
                    {
                        ApiController.Logging(true, fileid, "Skip No space: " + dir);
                    }
                }
                catch (Exception ex)
                {
                    ApiController.Logging(true, fileid, dir + "\r\n" + ex.Message + "\r\n" + ex.StackTrace);
                }
            }
            ApiController.Logging(true, fileid,"No space");
            return null;
        }
        static void Run()
        {
            string lastname = "";
            int fileid = 0;
            while (true)
            {
                try
                {
                    fileid = 0;
                    Dictionary<string, string> plots = new Dictionary<string, string>(10000); //check duplicate plot file
                    foreach (string dir in Program.Dirs)
                    {
                        try
                        {
                            foreach (string file in Directory.GetFiles(dir, "*.plot"))
                            {
                                string key = Path.GetFileName(file);
                                if (!plots.ContainsKey(key))
                                {
                                    plots.Add(key, null);
                                }
                            }
                        }
                        catch { }
                    }
                    int uncompletedid = 0;
                    long filesize;
                    string filename;
                    string path = GetUncompletedPlot(out uncompletedid, out filesize, out filename);
                    fileid = uncompletedid;
                    string url = ApiController.GetPlot(ref fileid, ref filesize, ref filename);
                    if (uncompletedid > 0 && uncompletedid != fileid)
                    {
                        Clear(path);
                        uncompletedid = 0;
                    }
                    if (fileid > 0)
                    {
                        ApiController.Logging(false, 0, "**************************************");
                        if (lastname == filename || plots.ContainsKey(filename))
                        {
                            ApiController.Stats(fileid, 100, 0);
                            ApiController.Logging(true, fileid, "Skip same file: " + filename);
                        }
                        else
                        {
                            ApiController.Logging(false, fileid, "Start downloading: " + filename);
                            using (PlotWriter writer = GetPlotWriter(fileid, filename, filesize))
                            {
                                if(writer == null)
                                {
                                    ApiController.Logging(true, fileid, "Download Failed: " + filename + ", it will auto try again in 30 seconds.");
                                    Thread.Sleep(30000);
                                    continue;
                                }
                                Client mc = new Client(url, fileid, writer);
                                if (mc.Run())
                                {
                                    lastname = filename;
                                    ApiController.Logging(true, fileid, "Download Success: " + filename);
                                }
                                else
                                {
                                    ApiController.Logging(true, fileid, "Download Failed: " + filename + ", it will auto try again in 30 seconds.");
                                    Thread.Sleep(30000);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            Console.Write("\r[" + DateTime.Now.ToString("HH:mm:ss") + "]" + filename);
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ApiController.Logging(true, fileid, "Client.Run:" + ex.Message + ex.StackTrace);
                    Thread.Sleep(30000);
                }
            }


        }
    }
}
