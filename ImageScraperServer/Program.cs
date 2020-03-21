using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
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

            byte[] byData = System.Text.Encoding.ASCII.GetBytes(source);
            socket.Send(System.Text.Encoding.ASCII.GetBytes(byData.Length.ToString()));
            GetString(socket);
            socket.Send(byData);
            socket.Close();
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
