/* copyright @ anyplots.com, All right rights, visit https://anyplots.com/ for more information */
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
namespace anyplots
{
    class Client
    {
        public static String downloadstatus = "";
        public static int optimizedmaxthreads = -1;
        public static int limitedmaxthreads = 128;
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
                    if(block!=null) position += block.buffer.Length;
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
                        if (maxthreads > 4)
                        {
                            maxthreads-=2;
                        }
                        Thread.Sleep(10000);
                    }
                }
                catch (Exception ex)
                {
                    if (maxthreads > 4)
                    {
                        maxthreads-=2;
                    }
                    ApiController.Logging(true, id, "DownloadThread(" + tid + "):" + ex.Message + "\r\n" + ex.StackTrace);
                    Thread.Sleep(10000);
                }
            }
        }
       
        public bool Run()
        {
            bool ret = true;
            for (int i = 0; i < 128; i++)
            {
                threads.Add(new Thread(DownloadThread));
                threads[i].Start(i);
                Thread.Sleep(10);
            }
            try { Console.CursorVisible = false; } catch { }
            try { if(Console.WindowWidth < 80) Console.WindowWidth = 80; } catch { }
            DateTime nextstat = DateTime.MinValue;
            float latest_speed = 0;
            int latest_count = 0;
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
                    Console.Write(("\r[" + id + "] " + percent.ToString("f4") + "%, " + speed.ToString("f1") + "Mbps, " + maxthreads + " Threads").PadRight(45,' ') +
                        (diskslow ? "write queue full, disk I/O speed is slow": (name.Substring(0, 30) + "...")));
                    if (optimizedmaxthreads != -1)
                    {
                        maxthreads = optimizedmaxthreads;
                    }
                    else
                    {
                        if (latest_speed < speed - 1)
                        {
                            latest_speed = speed;
                            latest_count = 0; 
                            if (speed < 20)
                            {
                                maxthreads = (int)speed;
                            }
                            else if (speed < 100)
                            {
                                maxthreads = 20 + (int)speed / 5;
                            }
                            else if (speed < 300)
                            {
                                maxthreads = 40 + (int)speed / 10;
                            }
                            else if (speed < 500)
                            {
                                maxthreads = 70 + (int)speed / 25;
                            }
                            else
                            {
                                maxthreads = 128;
                            }
                        }
                        else if (latest_speed > speed + 1)
                        {
                            latest_count++;
                            if (latest_count > 15)
                            {
                                latest_speed = speed;
                                latest_count = 0;
                                maxthreads = (int)Math.Min(maxthreads * 0.95, maxthreads - 1);
                            }
                        }
                        if(maxthreads > limitedmaxthreads)
                        {
                            maxthreads = limitedmaxthreads;
                        }
                        if (maxthreads > 128)
                        {
                            maxthreads = 128;
                        }
                        if (maxthreads < 4)
                        {
                            maxthreads = 4;
                        }
                    }
                    if (nextstat < DateTime.Now)
                    {
                        nextstat = DateTime.Now.AddSeconds(30);
                        int status = ApiController.Stats(id, (float)percent, (float)speed);
                        if (status == -9999) // error
                        {
                            running = false;
                            ret = false;
                            writer.Dispose();
                            break;
                        }
                        else if (status > 0)
                        {
                            limitedmaxthreads = status;
                        }
                        else
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
                        ApiController.Stats(id, 100,  0);
                        break;
                    }
                }
                Thread.Sleep(1000);
            }
            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].Join();
            }
            return ret;
        }
    }
}
