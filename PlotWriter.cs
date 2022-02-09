/* copyright @ anyplots.com, All rights reserved, visit https://anyplots.com/ for more information */
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
namespace anyplots
{
    class Block
    {
        public const int BlockSize = 1024 * 1024 * 4;
        public long position = 0;
        public int size = 0;
        public byte[] buffer;

        public Block(long pos, int size)
        {
            position = pos;
            buffer = new Byte[size];
        }
    }

    class PlotWriter : IDisposable
    {
        string dir = "";
        string fileid = "";
        string filename = "";
        Queue<Block> queue = new Queue<Block>();
        SortedList<Int64, bool> downloaded = new SortedList<Int64, bool>(1024);
        FileStream data = null;
        FileStream conf = null;
        Thread writer = null;
        bool running = true;
        public Exception Err = null;
        public Int64 FileSize;
        public PlotWriter(string dir, string fileid, string filename, Int64 size)
        {
            this.dir = dir;
            this.fileid = fileid;
            this.filename = filename;
            this.FileSize = size;
            try
            {
                data = new FileStream(dir + fileid + "." + filename + ".data" + ApiController.Ext, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                conf = new FileStream(dir + fileid + "." + filename + ".conf" + ApiController.Ext, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                if (data.Length != size)
                {
                    data.SetLength(size);
                    data.Position = 0;
                }
                else
                {
                    data.Position = DownloadedPosition;
                }
                downloaded.Add(data.Position, true);
                SaveProgress(true);
                writer = new Thread(Writing);
                writer.Start();
                ApiController.Logging(false, 0, "Using Dir: " + dir);
            }
            catch (Exception ex)
            {
                Dispose();
                try { File.Delete(dir + fileid + "." + filename + ".data" + ApiController.Ext); } catch { }
                try { File.Delete(dir + fileid + "." + filename + ".conf" + ApiController.Ext); } catch { }
                throw new Exception(ex.Message + ex.StackTrace);
            }
        }
        public void Dispose()
        {
            Err = new Exception("Disposed");
            if (running)
            {
                running = false;
            }
            if (writer != null)
            {
                try
                {
                    for (int i = 0; i < 10000 && writer.IsAlive; i++)
                    {
                        Thread.Sleep(10);
                    }
                }
                catch { };
                writer = null;
            }
            if (conf != null)
            {
                try { conf.Close(); } catch { }
                conf = null;
            }
            if (data != null)
            {
                try { data.Close(); } catch { }
                data = null;
            }
        }
        public long DownloadedPosition
        {
            get {
                Byte[] bytes_ = new byte[12];
                conf.Position = 0;
                if (conf.Read(bytes_, 0, 12) == 12)
                {
                    Int64 len = BitConverter.ToInt64(bytes_, 0);
                    if (len.GetHashCode() == BitConverter.ToInt32(bytes_, 8))
                    {
                        return len;
                    }
                    else
                    {
                        ApiController.Logging(false,0,"Hash Error");
                        return 0;
                    }
                }
                return 0;
            }
        }
        void SaveProgress(bool changed)
        {
            while (downloaded.Count > 1)
            {
                if (downloaded.Keys[0] + Block.BlockSize == downloaded.Keys[1])
                {
                    downloaded.RemoveAt(0);
                    changed = true;
                }
                else
                {
                    break;
                }
            }
            if (changed && downloaded.Count > 0)
            {
                conf.Position = 0;
                conf.Write(BitConverter.GetBytes(downloaded.Keys[0]), 0, 8);
                conf.Write(BitConverter.GetBytes(downloaded.Keys[0].GetHashCode()), 0, 4);
                conf.Flush();
            }
        }
        public double Percent = 0;
        public bool IsCompleted = false;

        public bool IsQueueFull
        {
            get
            {
                lock (queue)
                {
                    return queue.Count > 128;
                }
            }
        }
        public void Write(Block block)
        {;
            if (Err != null) { return; }
            if (!running) { Err = new Exception("writing stopped!!!"); return; }
            lock (queue)
            {
                queue.Enqueue(block);
            }
        }
        public void Finish()
        {
            ApiController.Logging(false,0,"finishing ...");
            bool isempty = false;
            for (int i = 0; i < 100000; i++)
            {
                lock (queue)
                {
                    if (queue.Count == 0)
                    {
                        isempty = true;
                        break;
                    }
                    else if(!running)
                    {
                        throw new Exception("writing stopped!!!");
                    }
                }
                Thread.Sleep(10);
            }
            if (!isempty)
            {
                throw new Exception("waiting writing error!!!");
            }
            Dispose();
            File.Move(dir + fileid + "." + filename + ".data" + ApiController.Ext, dir + filename);
            if (File.Exists(dir + filename))
            {
                File.Delete(dir + fileid + "." + filename + ".conf" + ApiController.Ext);
            }
            else
            {
                throw new Exception("rename file failed!!!");
            }
        }
        void Writing(object o)
        {
            try
            {
                int waits = 0;
                int writed = 0;
                DateTime nextflush = DateTime.Now.AddSeconds(5);
                while (running)
                {

                    List<Block> blocks = new List<Block>(200);
                    lock (queue)
                    {
                        if (queue.Count > 16 || waits > 6)
                        {
                            while (queue.Count > 0)
                            {
                                blocks.Add(queue.Dequeue());
                            }
                            waits = 0;
                        }
                        else
                        {
                            waits++;
                        }
                    }
                    if (!running) return;
                    if (blocks.Count > 0)
                    {
                        blocks.Sort(delegate (Block a, Block b) { return a.position.CompareTo(b.position); });
                        foreach (Block block in blocks)
                        {
                            if (!running) return;
                            data.Position = block.position;
                            data.Write(block.buffer, 0, block.size);
                            downloaded.Add(data.Position, true);
                            writed++;
                        }
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                    if (writed > 0 && nextflush < DateTime.Now)
                    {
                        data.Flush();
                        SaveProgress(false);
                        writed = 0;
                        nextflush = DateTime.Now.AddSeconds(5);
                    }
                    Percent = (downloaded.Keys[0] + (downloaded.Count - 1) * Block.BlockSize) * 100d / FileSize;
                    IsCompleted = downloaded.Keys[0] + Block.BlockSize >= FileSize;
                    GC.Collect();
                }
            }
            catch(Exception ex)
            {
                Err = ex;
            }
            running = false;
        }
    }
}
