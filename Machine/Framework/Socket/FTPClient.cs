using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Machine
{
    class FTPClient
    {
        /// <summary>
        /// 获取请求实例
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="ftpUser"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private static FtpWebRequest GetRequest(string uri, string ftpUser, string password)
        {
            //根据服务器信息FtpWebRequest创建类的对象
            FtpWebRequest result = (FtpWebRequest)FtpWebRequest.Create(uri);
            //提供身份验证信息
            result.Credentials = new System.Net.NetworkCredential(ftpUser, password);
            //设置请求完成之后是否保持到FTP服务器的控制连接，默认值为true
            result.KeepAlive = false;
            return result;
        }

        /// <summary>
        /// 获取响应实例的字符串
        /// </summary>
        /// <param name="ftp"></param>
        /// <returns></returns>
        private static string GetStringResponse(FtpWebRequest ftp)
        {
            //Get the result, streaming to a string
            string result = "";
            using(FtpWebResponse response = (FtpWebResponse)ftp.GetResponse())
            {
                long size = response.ContentLength;
                using(Stream datastream = response.GetResponseStream())
                {
                    using(StreamReader sr = new StreamReader(datastream, System.Text.Encoding.Default))
                    {
                        result = sr.ReadToEnd();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="fileinfo">需要上传的文件</param>
        /// <param name="ftpDir">ftp文件路径</param>
        /// <param name="ftpUser">ftp用户名</param>
        /// <param name="password">ftp密码</param>
        public static bool UploadFile(FileInfo fileinfo, string ftpDir, string ftpUser, string password)
        {
            // 获取响应实例
            string uri = $@"FTP://{ftpDir}/{Guid.NewGuid().ToString()}";
            FtpWebRequest ftp = null;
            try
            {
                ftp = GetRequest(uri, ftpUser, password);

                // 设置FTP命令 设置所要执行的FTP命令，
                ftp.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
                //指定文件传输的数据类型
                ftp.UseBinary = true;
                ftp.UsePassive = true;
                // 告诉ftp文件大小
                ftp.ContentLength = fileinfo.Length;
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("FTPClient.UploadFile().GetRequest", $"{fileinfo.FullName} error {ex.Message}", HelperLibrary.LogType.Error);
                return false;
            }
            // 打开一个文件流 (System.IO.FileStream) 去读上传的文件
            using(FileStream fs = fileinfo.OpenRead())
            {
                // 把上传的文件写入流
                using(Stream rs = ftp.GetRequestStream())
                {
                    try
                    {
                        // 缓冲大小设置为2KB
                        const int BufferSize = 2048;
                        byte[] content = new byte[BufferSize - 1 + 1];
                        int dataRead;
                        do
                        {
                            //每次读文件流的2KB
                            dataRead = fs.Read(content, 0, BufferSize);
                            rs.Write(content, 0, dataRead);
                        } while(dataRead >= BufferSize);
                    }
                    catch(System.Exception ex)
                    {
                        Def.WriteLog("FTPClient.UploadFile().UploadFile", $"{fileinfo.FullName} error {ex.Message}", HelperLibrary.LogType.Error);
                        return false;
                    }
                }
            }
            try
            {
                // 设置FTP命令：改名
                ftp = GetRequest(uri, ftpUser, password);
                ftp.Method = System.Net.WebRequestMethods.Ftp.Rename;
                ftp.RenameTo = fileinfo.Name;
                ftp.GetResponse();
                //Def.WriteLog("FTPClient.UploadFile()", $"{fileinfo.FullName}", HelperLibrary.LogType.Success);
            }
            catch(Exception ex)
            {
                // 设置FTP命令：删除
                ftp = GetRequest(uri, ftpUser, password);
                ftp.Method = WebRequestMethods.Ftp.DeleteFile;
                ftp.GetResponse();
                Def.WriteLog("FTPClient.UploadFile().Rename", $"{fileinfo.FullName} error {ex.Message}", HelperLibrary.LogType.Error);
                return false;
            }
            ftp = null;
            return true;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="localDir">下载至本地路径</param>
        /// <param name="ftpDir">ftp文件路径</param>
        /// <param name="ftpFile">从ftp要下载的文件名</param>
        /// <param name="ftpUser">ftp用户名</param>
        /// <param name="password">ftp密码</param>
        public static bool DownloadFile(string localDir, string ftpDir, string ftpFile, string ftpUser, string password)
        {
            string uri = $@"FTP://{ftpDir}/{ftpFile}";
            string localfile = $@"{localDir}\{Guid.NewGuid().ToString()}";

            System.Net.FtpWebRequest ftp = null;
            try
            {
                // 设置FTP命令：下载
                ftp = GetRequest(uri, ftpUser, password);
                ftp.Method = System.Net.WebRequestMethods.Ftp.DownloadFile;
                ftp.UseBinary = true;
                ftp.UsePassive = false;
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("FTPClient.DownloadFile().GetRequest", $"{uri} error {ex.Message}", HelperLibrary.LogType.Error);
                return false;
            }

            using(FtpWebResponse response = (FtpWebResponse)ftp.GetResponse())
            {
                using(Stream rs = response.GetResponseStream())
                {
                    using(FileStream fs = new FileStream(localfile, FileMode.CreateNew))
                    {
                        try
                        {
                            byte[] buffer = new byte[2048];
                            int read = 0;
                            do
                            {
                                read = rs.Read(buffer, 0, buffer.Length);
                                fs.Write(buffer, 0, read);
                            } while(read != 0);
                            fs.Flush();
                        }
                        catch(Exception ex)
                        {
                            // 下载出错，删除本地文件
                            File.Delete(localfile);
                            Def.WriteLog("FTPClient.DownloadFile().DownloadFile", $"DownloadFile {uri} error {ex.Message}", HelperLibrary.LogType.Error);
                            return false;
                        }
                    }
                }
            }
            try
            {
                string locFile = $@"{localDir}\{ftpFile}";
                if(File.Exists(locFile))
                {
                    locFile = $@"{localDir}\{ftpFile}({DateTime.Now.ToString("yyyy-MM-dd HHmmss")})";
                }
                //File.Delete(locFile);
                File.Move(localfile, locFile);
            }
            catch(Exception ex)
            {
                File.Delete(localfile);
                Def.WriteLog("FTPClient.DownloadFile().File.Move", $"Move local file {uri} error {ex.Message}", HelperLibrary.LogType.Error);
                return false;
            }
            ftp = null;
            return true;
        }

        /// <summary>
        /// 在FTP服务器上创建目录
        /// </summary>
        /// <param name="ftpDir">ftp地址</param>
        /// <param name="dirName">创建的目录名称</param>
        /// <param name="ftpUser">用户名</param>
        /// <param name="password">密码</param>
        public static bool MakeDirectory(string ftpDir, string ftpUser, string password)
        {
            try
            {
                string uri = $@"FTP://{ftpDir}";
                System.Net.FtpWebRequest ftp = GetRequest(uri, ftpUser, password);
                ftp.Method = WebRequestMethods.Ftp.MakeDirectory;

                FtpWebResponse response = (FtpWebResponse)ftp.GetResponse();
                response.Close();
                return true;
            }
            catch(Exception ex)
            {
                Def.WriteLog("FTPClient.MakeDir()", $@"{ftpDir} error {ex.Message}", HelperLibrary.LogType.Error);
            }
            return false;
        }

        /// <summary>
        /// 判断FTP服务器上该目录是否存在
        /// </summary>
        /// <param name="dirName"></param>
        /// <param name="ftpDir"></param>
        /// <param name="ftpUser"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool FtpDirIsExists(string ftpDir, string ftpUser, string password)
        {
            bool flag = false;
            try
            {
                string uri = $@"FTP://{ftpDir}";
                System.Net.FtpWebRequest ftp = GetRequest(uri, ftpUser, password);
                ftp.Method = WebRequestMethods.Ftp.ListDirectory;

                FtpWebResponse response = (FtpWebResponse)ftp.GetResponse();
                response.Close();
                flag = true;
            }
            catch(Exception ex)
            {
                Def.WriteLog("FTPClient.FtpDirIsExists()", $"{ftpDir} error {ex.Message}", HelperLibrary.LogType.Error);
                flag = false;
            }
            return flag;
        }

        /// <summary>
        /// 搜索远程文件
        /// </summary>
        /// <param name="targetDir"></param>
        /// <param name="hostname"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="SearchPattern"></param>
        /// <returns></returns>
        public static List<string> ListDirectory(string ftpDir, string ftpUser, string password, string searchPattern)
        {
            List<string> result = new List<string>();
            try
            {
                string uri = $@"FTP://{ftpDir}/{searchPattern}";

                FtpWebRequest ftp = GetRequest(uri, ftpUser, password);
                ftp.Method = WebRequestMethods.Ftp.ListDirectory;
                ftp.UsePassive = true;
                ftp.UseBinary = true;


                string str = GetStringResponse(ftp);
                str = str.Replace("\r\n", "\r").TrimEnd('\r');
                str = str.Replace("\n", "\r");
                if(str != string.Empty)
                    result.AddRange(str.Split('\r'));

                return result;
            }
            catch { }
            return null;
        }
    }
}
