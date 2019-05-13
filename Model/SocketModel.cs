using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Model
{
    public class SocketModel
    {
        public SocketModel()
        {

        }

        public SocketModel(byte[] c)
        {
            this.Content = c;
        }

        private const char cStart = (char)0x02;

        private const char cEnd = (char)0x03;

        private int ContentLength
        {
            get
            {
                return Content == null ? 0 : Content.Length;
            }
        }

        public byte[] Content { get; set; }

        public byte[] ToByteArray()
        {
            using (MemoryStream memoryStream = new MemoryStream()) //创建内存流
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream)) //以二进制写入器往这个流里写内容
                {
                    binaryWriter.Write(cStart); //写入协议一级标志，占1个字节
                    binaryWriter.Write(ContentLength); //写入实际消息长度，占4个字节

                    if (ContentLength > 0)
                    {
                        binaryWriter.Write(Content); //写入实际消息内容
                    }

                    binaryWriter.Write(cEnd); //写入协议一级标志，占1个字节
                }

                return memoryStream.ToArray(); //将流内容写入自定义字节数组
            }
        }
    }
}
