using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace ImageScraperServer
{
    class Program
    {

        private const int Port = 1221;
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(Port);
            Console.WriteLine($"Listening at: {Port}");
            while (true)
            {
                server.Start();
                Socket newSocket = server.AcceptSocket();
                new Thread(() => ManageSocket(newSocket)).Start();
                Console.WriteLine("Connected!");
            }
        }

        private static void ManageSocket(Socket socket)
        {

            string url = GetString(socket);
            try
            {
                new Uri(url);
            }
            catch (Exception e)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                socket.Close();
                return;
            }
            Console.WriteLine(url);
            string source = null;
            IWebDriver driver = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                driver = new ChromeDriver("/var/www/pixtopost/");
            }
            else
            {
                driver = new ChromeDriver();
            }

            try
            {
                
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(500);
                source = driver.PageSource;
            }
            catch (Exception e)
            {

            }
            finally
            {
                driver?.Quit();
                driver?.Dispose();
            }

            try
            {
                byte[] byData = Encoding.ASCII.GetBytes(GetLinks(source,url).Trim());
                socket.Send(Encoding.ASCII.GetBytes(byData.Length.ToString()));
                GetString(socket);
                socket.Send(byData);
                socket.Close();
            }
            catch (Exception e)
            {
                
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            try
            {
                Thread.CurrentThread.Abort();
            }
            catch (Exception e)
            {
                
            }
        }

        private static string GetLinks(string source, string website)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(source);
            string toReturn = "";
            foreach (HtmlNode link in document.DocumentNode.SelectNodes("//img"))
            {
                string url = link.GetAttributeValue("src", "nothing").Replace("\u0022", "");
                if (!UrlExist(url))
                {
                    url = (website[^1] == '/' ? website : website + "/") + url;
                    if (!UrlExist(url)) url = null;
                }

                if (url == null)
                    continue;
                toReturn += url + "\n";
            }

            return toReturn;
        }

        private static bool UrlExist(string path)
        {
            HttpWebResponse response = null;
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(path);
            }
            catch (Exception e)
            {
                return false;
            }
            request.Method = "HEAD";
            bool result = false;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                result = true;
            }
            catch (WebException ex)
            {
                /* A WebException will be thrown if the status of the response is not `200 OK` */
            }
            finally
            {
                // Don't forget to close your response.
                if (response != null)
                {
                    response.Close();
                }
            }

            return result;
        }
        private static string GetString(Socket socket)
        {
            byte[] buffer = new byte[1024];
            int iRx = socket.Receive(buffer);
            char[] chars = new char[iRx];

            System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
            int charLen = d.GetChars(buffer, 0, iRx, chars, 0);
            return new System.String(chars);
        }
    }
}
