///////////////////////////////////
// dkxce UMoney BYN Rate Grabber //
///////////////////////////////////

using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Reflection;

namespace UMoneyBynRate
{
    public static class UG
    {
        private const string url = "https://yoomoney.ru/account/exchange-rates?lang=ru";

        #region WinAPI

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion WinAPI

        private static void WriteHeader()
        {
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("-- dkxce UMoney BYN Rate Grabber --");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("");
            Console.WriteLine(url);
            Console.WriteLine("");
        }

        public static void MainProc()
        {
            WriteHeader();

            // Init
            double? rate = 0;
            bool ok = false;

            // First Rate

            try { rate = GetRate(); ok = rate.HasValue; }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); };

            Console.WriteLine($"Current Rate is: {rate}");
            if (!ok)
            {
                Console.WriteLine("Press Enter to Exit");
                Console.ReadLine();
                return;
            }
            else if(rate.HasValue)
                AppendRate(rate.Value);

            Console.WriteLine();

            // Get Parameters

            bool entered = false;

            Console.Write("Enter Update period (default 30 min) : ");
            string perRate = Conso1e.ReadLine(10000, out entered).Replace(",", ".").Trim();
            if (!entered) Console.WriteLine();
            if (!double.TryParse(perRate, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double perVal)) perVal = 30.0;
            
            Console.Write("Enter Min Alarm Rate (default -inf)  : ");
            string minRate = Conso1e.ReadLine(10000, out entered).Replace(",",".").Trim();
            if (!entered) Console.WriteLine();
            if (!double.TryParse(minRate, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double minVal)) minVal = double.MinValue;
            
            Console.Write("Enter Max Alarm Rate (default +inf)  : ");
            string maxRate = Conso1e.ReadLine(10000, out entered).Replace(",", ".").Trim();
            if (!entered) Console.WriteLine();
            if (!double.TryParse(maxRate, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double maxVal)) maxVal = double.MaxValue;

            if (perVal < 1) perVal = 1;
            if (perVal > 1440) perVal = 1440;

            // Write Parameters

            Console.WriteLine();
            Console.WriteLine($"Update Interval (m) : {perVal}");
            Console.WriteLine($"Min Alarm Set to    : {minVal}");
            Console.WriteLine($"Max Alarm Set to    : {maxVal}");

            Console.WriteLine();
            Console.WriteLine("Starting watcher...");
            
            // Loop

            while (true)
            {
                int ctr = (int)perVal * 60;
                int left = Console.CursorLeft;
                
                while(ctr-- > 0)
                {                    
                    Console.Write($"Next check in {ctr} sec: {DateTime.Now.AddSeconds(ctr)} ");
                    System.Threading.Thread.Sleep(1000);
                    Console.CursorLeft = left;
                };

                Console.Write($"{DateTime.Now} Checking rate...                  ");
                Console.CursorLeft = left;
                
                ok = false;
                bool writeIfNoErr = true;
                try { rate = GetRate(); ok = rate.HasValue; }
                catch (Exception ex) { Console.WriteLine($"{DateTime.Now} Error checking rate: {ex.Message}"); writeIfNoErr = false; };

                if (ok)
                {
                    ConsoleColor cc = Console.ForegroundColor;
                    if (rate.Value <= minVal)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        try { SetForegroundWindow(GetConsoleWindow()); } catch { };
                        try { FlashWindow.Flash(GetConsoleWindow(), 8); } catch { };
                    };
                    if (rate.Value >= maxVal)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        try { SetForegroundWindow(GetConsoleWindow()); } catch { };
                        try { FlashWindow.Flash(GetConsoleWindow(), 8); } catch { };
                    };
                    Console.WriteLine($"{DateTime.Now} Current Rate is: {rate}");                    
                    Console.ForegroundColor = cc;
                    AppendRate(rate.Value);
                }
                else if (writeIfNoErr) Console.WriteLine($"{DateTime.Now} Current Rate is: Empty!");
            };
        }

        private static double? GetRate()
        {            
            HttpWebRequest wreq = (HttpWebRequest)HttpWebRequest.Create(url);
            wreq.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
            string response = "";
            int result = 0;
            try
            {
                HttpWebResponse wres = (HttpWebResponse)wreq.GetResponse();
                Encoding enc = Encoding.UTF8;
                using (StreamReader streamReader = new StreamReader(wres.GetResponseStream(), enc))
                    response = streamReader.ReadToEnd();
                result = (int)wres.StatusCode;
            }
            catch (WebException ex)
            {
                HttpWebResponse wres = (HttpWebResponse)ex.Response;
                Encoding enc = Encoding.UTF8;
                using (StreamReader streamReader = new StreamReader(wres.GetResponseStream(), enc))
                    response = streamReader.ReadToEnd();
                result = (int)wres.StatusCode;
            }
            catch (Exception ex) { throw ex; };
            if (string.IsNullOrEmpty(response)) return null;

            int iof = response.IndexOf("{\"currencyCode\":\"BYN\"");
            if (iof < 0) throw new Exception("Exchange rate not found");
            int lof = response.IndexOf("}", iof);
            string data = response.Substring(iof, lof - iof + 1);

            JObject jo = (JObject)JsonConvert.DeserializeObject(data);
            double rate = jo["sellRate"].ToObject<double>();
            return rate;
        }

        private static void AppendRate(double rate)
        {            
            FileStream fs = null;
            try
            {
                string fName = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "rates_stat.txt");
                fs = new FileStream(fName, FileMode.Append, FileAccess.Write);
                fs.Position = fs.Length;                
                StreamWriter sw = new StreamWriter(fs, Encoding.ASCII);
                if (fs.Position == 0) sw.WriteLine("### UMoney BYN Rate ###\r\nDateTime\tRate");
                sw.WriteLine($"{DateTime.Now}\t{rate}");
                sw.Close();
            }
            finally
            {
                if (fs != null) fs.Close();
            };
        }
    }

    public class Currency
    {
        public string currencyCode;
        public string countryCode;
        public string name;
        public string pluralName;
        public bool isForeign;
        public bool isOpened;
        public double balance;
        public double buyRate;
        public double sellRate;
        public double exchangeMin;
        public double weight;
        public bool isDecimalDisallowed;
    }

    public static class FlashWindow
    {
        #region WinAPI

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            /// <summary>
            /// The size of the structure in bytes.
            /// </summary>
            public uint cbSize;
            /// <summary>
            /// A Handle to the Window to be Flashed. The window can be either opened or minimized.
            /// </summary>
            public IntPtr hwnd;
            /// <summary>
            /// The Flash Status.
            /// </summary>
            public uint dwFlags;
            /// <summary>
            /// The number of times to Flash the window.
            /// </summary>
            public uint uCount;
            /// <summary>
            /// The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
            /// </summary>
            public uint dwTimeout;
        }

        /// <summary>
        /// Stop flashing. The system restores the window to its original stae.
        /// </summary>
        public const uint FLASHW_STOP = 0;

        /// <summary>
        /// Flash the window caption.
        /// </summary>
        public const uint FLASHW_CAPTION = 1;

        /// <summary>
        /// Flash the taskbar button.
        /// </summary>
        public const uint FLASHW_TRAY = 2;

        /// <summary>
        /// Flash both the window caption and taskbar button.
        /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
        /// </summary>
        public const uint FLASHW_ALL = 3;

        /// <summary>
        /// Flash continuously, until the FLASHW_STOP flag is set.
        /// </summary>
        public const uint FLASHW_TIMER = 4;

        /// <summary>
        /// Flash continuously until the window comes to the foreground.
        /// </summary>
        public const uint FLASHW_TIMERNOFG = 12;

        #endregion WinAPI

        private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
        {
            FLASHWINFO fi = new FLASHWINFO();
            fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi));
            fi.hwnd = handle;
            fi.dwFlags = flags;
            fi.uCount = count;
            fi.dwTimeout = timeout;
            return fi;
        }

        /// <summary>
        ///     Flash the spacified Window (Form) until it recieves focus.
        /// </summary>
        /// <param name="handle">The Form (Window) to Flash.</param>
        /// <returns></returns>
        public static bool Flash(IntPtr handle)
        {
            // Make sure we're running under Windows 2000 or later
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(handle, FLASHW_ALL | FLASHW_TIMERNOFG, uint.MaxValue, 0);
                return FlashWindowEx(ref fi);
            };
            return false;
        }

        /// <summary>
        /// Flash the specified Window (form) for the specified number of times
        /// </summary>
        /// <param name="handle">The Form (Window) to Flash.</param>
        /// <param name="count">The number of times to Flash.</param>
        /// <returns></returns>
        public static bool Flash(IntPtr handle, uint count)
        {
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(handle, FLASHW_ALL, count, 0);
                return FlashWindowEx(ref fi);
            };
            return false;
        }

        /// <summary>
        /// Start Flashing the specified Window (form)
        /// </summary>
        /// <param name="handle">The Form (Window) to Flash.</param>
        /// <returns></returns>
        public static bool Start(IntPtr handle)
        {
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(handle, FLASHW_ALL, uint.MaxValue, 0);
                return FlashWindowEx(ref fi);
            };
            return false;
        }

        /// <summary>
        /// Stop Flashing the specified Window (form)
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static bool Stop(IntPtr handle)
        {
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(handle, FLASHW_STOP, uint.MaxValue, 0);
                return FlashWindowEx(ref fi);
            };
            return false;
        }

        /// <summary>
        /// A boolean value indicating whether the application is running on Windows 2000 or later.
        /// </summary>
        private static bool Win2000OrLater
        {
            get { return System.Environment.OSVersion.Version.Major >= 5; }
        }
    }
}
