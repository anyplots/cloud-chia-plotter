/* copyright @ anyplots.com, All rights reserved, visit https://anyplots.com/ for more information */
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
using System.Threading;
using System.Diagnostics;
namespace anyplots
{
    class Client
    {
        public static String downloadstatus = "";
        public static int optimizedmaxthreads = -1;
        string url = "";
        int id = 0;
        PlotWriter writer = null;
        bool running = true;
        static int maxthreads = 8;
        List<Thread> threads = new List<Thread>();

        bool eof = false;
        long position = 0;
        long endposition = 0;
        object syncposition = new object();
        Block NextBlock()
        {
            Block block = null;
            lock (syncposition)
            {
                try
                {
                    if (endposition > position)
                    {
                        block = new Block(position, (int)Math.Min(Block.BlockSize, endposition - position));
                        return block;
                    }
                    else
                    {
                        eof = true;
                        return null;// end
                    }
                }
                finally
                {
                    if (block != null) position += block.buffer.Length;
                }
            }
        }
        public Client(string url, int id, PlotWriter writer)
        {
            this.url = url;
            this.id = id;
            this.writer = writer;
            this.endposition = writer.FileSize;
            this.position = writer.DownloadedPosition;
        }
        bool diskslow = false;
        void DownloadThread(object o)
        {
            int tid = (int)o;
            Block block = null;
            Stopwatch sw = new Stopwatch();
            Random rnd = new Random();
            while (running)
            {
                try
                {
                    if (block == null)
                    {
                        if (tid >= maxthreads)
                        {
                            Thread.Sleep(rnd.Next(5, 15));
                            if (eof)
                            {
                                return; // end
                            }
                            continue;
                        }
                    }
                    for (int i = 0; running && i < 100000; i++)
                    {
                        if (!writer.IsQueueFull) break;
                        if (i > 1000) { diskslow = true; }
                        Thread.Sleep(10);
                    }
                    diskslow = false;
                    if (block == null)
                    {
                        block = NextBlock();
                    }
                    if (block == null)
                    {
                        return; // end
                    }
                    sw.Restart();
                    sw.Start();
                    bool ret = BlockTransfer.GetBlock(url, block);
                    sw.Stop();
                    if (ret)
                    {
                        writer.Write(block);
                        block = null;
                    }
                    else
                    {
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception ex)
                {
                    ApiController.Logging(true, id, "DownloadThread(" + tid + "):" + ex.Message + "\r\n" + ex.StackTrace);
                    Thread.Sleep(10000);
                }
            }
        }
        int pingavg = 100;
        int pingmin = 2000;
        int pinglos = 0;
        void PingServer(object o)
        {
            IPAddress ip = IPAddress.Parse((string)o);
            Queue<int> items = new Queue<int>(500);
            Stopwatch watch = new Stopwatch();
            while (running)
            {
                watch.Reset();
                watch.Start();
                using (Ping ping = new Ping())
                {
                    try
                    {
                        Byte[] data = Encoding.ASCII.GetBytes((Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString()).Substring(0, 64));
                        PingReply pr = ping.Send(ip, 1000, data);
                        if (pr.Status == IPStatus.Success)
                        {
                            items.Enqueue((int)pr.RoundtripTime);
                        }
                        else
                        {
                            items.Enqueue(1000);
                        }
                    }
                    catch { items.Enqueue(1000); }
                }
                Queue<int> items_ = new Queue<int>(500);
                while (items.Count > 300) items.Dequeue();
                int total = 0,count = 0, min = 1000;
                while (items.Count > 0)
                {
                    int t = items.Dequeue();
                    total += t;
                    min = Math.Min(min, t);
                    if (t >= 1000) count++;
                    items_.Enqueue(t);
                }
                items = items_;
                pingmin = min;
                pingavg = total / items_.Count;
                pinglos = 100 * count / items_.Count;
                if (optimizedmaxthreads == -1)
                {
                    float max_;
                    if (items_.Count > 200 && pinglos > 0)
                    {
                        max_ = Math.Min(BlockTransfer.Speed * 5, ApiController.ClientBandwidth) / 16f * Math.Min(8, Math.Max(1, (pingavg / 50f)));
                    }
                    else
                    {
                        max_ = ApiController.ClientBandwidth / 16f * Math.Min(8, Math.Max(1, (pingavg / 50f)));
                    }
                    max_ -= max_ * pinglos * 2 / 100;
                    if (max_ < 4)
                    {
                        max_ = 4;
                    }
                    if (max_ > threads.Count)
                    {
                        max_ = threads.Count;
                    }
                    maxthreads = (int)max_;
                }
                watch.Stop();
                if (watch.ElapsedMilliseconds < 995)
                {
                    Thread.Sleep(1000 - (int)watch.ElapsedMilliseconds);
                }
            }
        }
        public bool Run()
        {
            bool ret = true;
            try
            {
                maxthreads = ApiController.ClientBandwidth / 16;
                if (maxthreads > 128) maxthreads = 128;
                if (maxthreads < 4) maxthreads = 4;
                Thread t = new Thread(PingServer);
                t.Start(url.Split(':')[0]);
                for (int i = 0; i < 128; i++)
                {
                    threads.Add(new Thread(DownloadThread));
                    threads[i].Start(i);
                    Thread.Sleep(10);
                }
                try { if (Console.WindowWidth < 80) Console.WindowWidth = 80; } catch { }
                DateTime nextstat = DateTime.MinValue;
                string name = url.Split('/')[1].Split('.')[1];
                while (running)
                {
                    bool isalive = false;
                    for (int i = 0; i < threads.Count; i++)
                    {
                        if (threads[i].IsAlive)
                        {
                            isalive = true;
                            break;
                        }
                    }
                    if (writer.Err != null)
                    {
                        ret = false;
                        running = false;
                        writer.Dispose();
                        ApiController.Logging(true, id, writer.Err.Message + "\r\n" + writer.Err.StackTrace);
                        break;
                    }
                    if (isalive)
                    {
                        double percent = Math.Max(writer.Percent - 0.0001f, 0); // 100% will cause plot delete on the server side.
                        float speed = BlockTransfer.Speed;
                        Console.Write(("\r[" + id + "] " + percent.ToString("f4") + "%, " + speed.ToString("f1") + "Mbps, " + maxthreads + " Threads").PadRight(45, ' ') +
                            (diskslow ? "write queue full, disk I/O speed is slow" : (name.Substring(0, 30) + "...")));

                        if (optimizedmaxthreads != -1)
                        {
                            maxthreads = optimizedmaxthreads;
                        }
                        if (nextstat < DateTime.Now)
                        {
                            nextstat = DateTime.Now.AddSeconds(30);
                            int status = ApiController.Stats(id, (float)percent, (float)speed, pingmin, pingavg, pinglos);
                            if (status == -9999) // error
                            {
                                running = false;
                                ret = false;
                                writer.Dispose();
                                break;
                            }
                            else if (status < 0)
                            {
                                optimizedmaxthreads = -status;
                            }
                        }
                    }
                    else
                    {
                        if (writer.IsCompleted)
                        {
                            writer.Finish();
                            ApiController.Stats(id, 100, 0, pingmin, pingavg, pinglos);
                            break;
                        }
                    }
                    Thread.Sleep(1000);
                }
                for (int i = 0; i < threads.Count; i++)
                {
                    threads[i].Join();
                }
            }
            finally
            {
                running = false;
                try { writer.Dispose(); } catch { }
            }
            return ret;
        }
    }
}
