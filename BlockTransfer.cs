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
    static class BlockTransfer
    {
        static Int32 ReadInt32(byte[] buffer, int offset) //prevent  Big-Endian, Little-Endian problem
        {
            return Convert.ToInt32(Encoding.ASCII.GetString(buffer, offset, 8), 16);
        }
        static Byte[] Int32Bytes(int v) //prevent  Big-Endian, Little-Endian problem
        {
            return Encoding.ASCII.GetBytes(v.ToString("x8"));
        }
        static Queue<KeyValuePair<Int64, Int64>> stats = new Queue<KeyValuePair<Int64, Int64>>(2000);
        static Int64 counter = 0;
        public static float Speed
        {
            get
            {
                lock (stats)
                {
                    Int64 ticks = DateTime.Now.Ticks - 3000000000;
                    while (stats.Count > 0 && stats.Peek().Key < ticks)
                    {
                        stats.Dequeue();
                    }
                    if (stats.Count > 0)
                    {
                        float taken = (DateTime.Now.Ticks - stats.Peek().Key) / 10000000.0f;
                        float speed = (float)((counter - stats.Peek().Value) * 8 / taken ) / 1024 / 1024;
                        if(taken > 120)
                        {
                            if (speed < 20)
                            {
                                buffersize = 1 << 18;
                            }
                            else if (speed < 50)
                            {
                                buffersize = 1 << 19;
                            }
                            else if (speed < 100)
                            {
                                buffersize = 1 << 20;
                            }
                            else if (speed < 300)
                            {
                                buffersize = 1 << 21;
                            }
                            else 
                            {
                                buffersize = 1 << 22;
                            }
                        }
                        return speed;
                    }
                }
                return 0f;
            }
        }
        static int buffersize = 1<<20;
        public static bool GetBlock(string url, Block block)
        {
            block.size = 0;
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {

                try
                {
                    socket.NoDelay = true;
                    socket.ReceiveTimeout = 300000;
                    socket.SendTimeout = 10000;
                    socket.ReceiveBufferSize = buffersize;
                    string[] items = url.Split('/');
                    IPAddress ip = IPAddress.Parse(items[0].Split(':')[0]);
                    Int32 port = Int32.Parse(items[0].Split(':')[1]);
                    IAsyncResult ar = socket.BeginConnect(ip, port, null, null);
                    using (var w = ar.AsyncWaitHandle)
                    {
                        if (w.WaitOne(10000))
                        {
                            socket.EndConnect(ar);
                            if (socket.Connected)
                            {
                                string request = "GET\n" + items[1] + "\n" + block.position.ToString() + "\n" + (block.position + block.buffer.Length - 1).ToString() + "\n";
                                Byte[] data_ = Encoding.ASCII.GetBytes(request.PadRight(248, ' '));
                                if (socket.Send(data_) != 248)
                                {
                                    ApiController.Logging(false,0,"send request error!");
                                    return false;
                                }
                                if (socket.Send(Int32Bytes(CRC(data_, data_.Length))) != 8)
                                {
                                    ApiController.Logging(false,0,"send crc error!");
                                    return false;

                                }
                                int received = 0;
                                while (received < block.buffer.Length)
                                {
                                    int ret = socket.Receive(block.buffer, received, block.buffer.Length - received, SocketFlags.None);
                                    if (ret > 0)
                                    {
                                        received += ret;
                                        lock (stats)
                                        {
                                            counter += ret;
                                            stats.Enqueue(new KeyValuePair<Int64, Int64>(DateTime.Now.Ticks, counter));
                                            while (stats.Count > 1000)
                                            {
                                                stats.Dequeue();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                Byte[] crc = new byte[8];
                                if (socket.Receive(crc) != 8 || CRC(block.buffer, received) != ReadInt32(crc, 0))
                                {
                                    ApiController.Logging(false,0,"crc error!");
                                    return false;
                                }
                                block.size = block.buffer.Length;
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ApiController.Logging(false,0,ex.Message + "\r\n" + ex.StackTrace);
                }
                finally
                {
                    try { socket.Shutdown(SocketShutdown.Both); } catch { }
                }
            }
            return false;
        }

        private const UInt32 magic = 0xedb88320u;

        private static UInt32[] table = new UInt32[16 * 256];

        static BlockTransfer()
        {
            for (UInt32 i = 0; i < 256; i++)
            {
                UInt32 res = i;
                for (int t = 0; t < 16; t++)
                {
                    for (int k = 0; k < 8; k++) res = (res & 1) == 1 ? magic ^ (res >> 1) : (res >> 1);
                    table[(t * 256) + i] = res;
                }
            }
        }
        static Int32 CRC(byte[] buffer, int length)
        {
            UInt32 value = 0xffffffff;
            int offset = 0;
            while (length >= 16)
            {
                var a = table[(3 * 256) + buffer[offset + 12]]
                    ^ table[(2 * 256) + buffer[offset + 13]]
                    ^ table[(1 * 256) + buffer[offset + 14]]
                    ^ table[(0 * 256) + buffer[offset + 15]];

                var b = table[(7 * 256) + buffer[offset + 8]]
                    ^ table[(6 * 256) + buffer[offset + 9]]
                    ^ table[(5 * 256) + buffer[offset + 10]]
                    ^ table[(4 * 256) + buffer[offset + 11]];

                var c = table[(11 * 256) + buffer[offset + 4]]
                    ^ table[(10 * 256) + buffer[offset + 5]]
                    ^ table[(9 * 256) + buffer[offset + 6]]
                    ^ table[(8 * 256) + buffer[offset + 7]];

                var d = table[(15 * 256) + ((byte)value ^ buffer[offset])]
                    ^ table[(14 * 256) + ((byte)(value >> 8) ^ buffer[offset + 1])]
                    ^ table[(13 * 256) + ((byte)(value >> 16) ^ buffer[offset + 2])]
                    ^ table[(12 * 256) + ((value >> 24) ^ buffer[offset + 3])];

                value = d ^ c ^ b ^ a;
                offset += 16;
                length -= 16;
            }

            while (--length >= 0)
                value = table[(byte)(value ^ buffer[offset++])] ^ value >> 8;

            return (Int32)(value & 0x7fffffff);
        }
        static string MD5(string content)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(result).Replace("-", "");
            }
        }
    }

}
