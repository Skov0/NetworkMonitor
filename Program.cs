using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace SimpleNetMonitor
{
    class Program
    {
        // console strings
        public static readonly string _DEBUG    = "[DEBUG] ";
        public static readonly string _STATUS   = "[STATUS] ";
        public static readonly string _ERROR    = "[ERROR] ";

        // app vars
        public static int runTimes = 0;
        public static int runTime = 0;
        public static double downTime = 0;
        public static int totalLogEnt = 0;
        public static DateTime timedate;

        // test ips array
        public static string[] IPs = { "8.8.8.8", "8.8.4.4" }; // add 31.13.70.1
        public static string usingIP = string.Empty;

        #region DisableClose
        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        #endregion

        static void Main(string[] args)
        {
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnExitEvent);
            Setup();
        }

        private static void OnExitEvent(object sender, EventArgs e)
        {
            // calc downtime
            TimeSpan down = TimeSpan.FromSeconds(downTime);
            string downResult = down.ToString(@"hh\:mm\:ss");

            // calc rumTime
            TimeSpan run = TimeSpan.FromSeconds(Convert.ToDouble(runTime));
            string runResult = run.ToString(@"hh\:mm\:ss");

            // get start time
            string startTime = timedate.ToString();

            // get stop time
            string stopTime = DateTime.Now.ToString();

            // format string
            string result = String.Format("NetworkMonitor v0.1 written by Mat S. results: " +
                Environment.NewLine + "---------------------------------------------------" +
                Environment.NewLine + "Start time           : {0}." +
                Environment.NewLine + "Stop time            : {1}." +
                Environment.NewLine + "Total runtime        : {2}." +
                Environment.NewLine + "Total downtime       : {3}." +
                Environment.NewLine + "Total log entries    : {4}." +
                Environment.NewLine + "---------------------------------------------------\r\n\r\n", 
                startTime, stopTime, runResult, downResult, totalLogEnt);

            // if file is not created yet, exit.
            if (!File.Exists("log.txt")) { return; }

            // write final log file
            string currentContent = File.ReadAllText("log.txt");
            currentContent = result + "\n\n" + currentContent;
            File.WriteAllText("log.txt", currentContent);
        }

        private static void Setup()
        {
            // setup console
            Console.Title = "NetworkMonitor dev. 001";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("NetworkMonitor v0.1 loaded... (build date. 5/11/2016)");

            // set ip
            Random ipRnd = new Random();
            usingIP = IPs[ipRnd.Next(0, IPs.Count())].ToString();

            // detect old log file
            if (File.Exists("log.txt"))
            {
                Random rnd = new Random();
                string newName = "log" + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString() + rnd.Next(1, 100) + ".txt";
                File.Move("log.txt", newName);
                Console.WriteLine(_DEBUG + "Old log file found.. Renamed to {0}. Current log file is log.txt..", newName);
            }

            // set start time
            timedate = DateTime.Now;

            // start timer
            Timer monitor = new Timer();
            monitor.Elapsed += new ElapsedEventHandler(MonitorEvent);
            monitor.Interval = 5000;
            monitor.Enabled = true;
            Console.WriteLine(_DEBUG + "Monitor timer started and ready...");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine(_DEBUG + "Currently using server: " + usingIP + ".");

            Console.WriteLine("Press \'q\' and then ENTER to quit the Monitor..");
            Console.WriteLine();
            while (Console.Read() != 'q');
        }

        private static void MonitorEvent(object sender, ElapsedEventArgs e)
        {
            StartMonitor();
        }

        private static void StartMonitor()
        {           
            runTimes++;
            runTime = runTime + 5;
            try
            {
                Random ipRnd = new Random();
                Ping ping = new Ping();            
                IPAddress ip = IPAddress.Parse(IPs[ipRnd.Next(0, IPs.Count())].ToString());
                PingReply reply = ping.Send(ip);

                if (reply.Status == IPStatus.Success)
                {
                    string print = _STATUS + "Test ran successfully... " + "(" + runTimes.ToString() + ")";
                    Console.WriteLine(print);
                    Log(print);
                }
                else
                {
                    downTime = downTime + 5;
                    string print = _ERROR + "Test was unsuccessfull.. Problem detected.. " + "(" + runTimes.ToString() + ")";
                    Console.WriteLine(print);
                    Log(print);
                }
            }
            catch
            {
                downTime = downTime + 5;
                string print = _DEBUG + "Unable to run monitor... Retrying in 5 secounds... " + "(" + runTimes.ToString() + ")";
                Console.WriteLine(print);
                Log(print);
            }         
        }

        private static void Log(string message)
        {
            totalLogEnt++;
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                w.WriteLine(message);
                w.WriteLine("-------------------------------------");
            }
        }
    }
}
