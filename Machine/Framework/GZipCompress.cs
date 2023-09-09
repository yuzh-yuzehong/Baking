using System.IO;
using System.IO.Compression;

namespace Machine
{
    /// <summary>
    /// 字符压缩类
    /// </summary>
    public class GZipCompress
    {
        /// <summary>
        /// GZip压缩
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] rawData, int size)
        {
            try
            {
                using(MemoryStream ms = new MemoryStream())
                {
                    using(GZipStream zipStream = new GZipStream(ms, CompressionMode.Compress, true))
                    {
                        zipStream.Write(rawData, 0, size);
                        zipStream.Close();
                        return ms.ToArray();
                    }
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("GZipCompress.Compress()", $"{ex.Message}\r\n{ex.StackTrace}", HelperLibrary.LogType.Error);
            }
            return null;
        }

        /// <summary>
        /// GZIP解压
        /// </summary>
        /// <param name="zippedData"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] zippedData)
        {
            try
            {
                using(MemoryStream ms = new MemoryStream(zippedData))
                {
                    using(GZipStream zipStream = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        using(MemoryStream outBuffer = new MemoryStream())
                        {
                            byte[] block = new byte[1024];
                            while(true)
                            {
                                int bytesRead = zipStream.Read(block, 0, block.Length);
                                if(bytesRead <= 0)
                                    break;
                                else
                                    outBuffer.Write(block, 0, bytesRead);
                            }
                            zipStream.Close();
                            return outBuffer.ToArray();
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("GZipCompress.Decompress()", $"{ex.Message}\r\n{ex.StackTrace}", HelperLibrary.LogType.Error);
            }
            return null;
        }
    }
}
