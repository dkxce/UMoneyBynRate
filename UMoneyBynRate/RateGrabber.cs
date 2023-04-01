///////////////////////////////////
//      dkxce Rate Grabber       //
///////////////////////////////////

using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

namespace UMoneyBynRate
{
    public interface IRateGrabber
    {
        /// <summary>
        ///     sell, buy
        /// </summary>
        /// <param name="ex"></param>
        /// <returns>sell, buy</returns>
        (double?, double?) GetRates(out Exception ex);
    }
    
    public abstract class RateGrabber : IRateGrabber
    {
        #region WinAPI

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion WinAPI

        private static List<RateGrabber> Grabbers = new List<RateGrabber>();
        protected string url = string.Empty;
        protected string name = string.Empty;

        public RateGrabber() { }
        public RateGrabber(string url) { this.url = url; }

        public virtual (double?, double?) GetRates(out Exception ex) { ex = null; return (null, null); }

        private static void WriteHeader()
        {
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("-------  dkxce Rate Grabber -------");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("");
        }

        private static void WriteGrabbers()
        {
            foreach (RateGrabber g in Grabbers)
                Console.WriteLine($"using: {g.name}");
            Console.WriteLine("");
        }

        private static void WriteCurrentRates()
        {
            foreach (RateGrabber rg in Grabbers)
            {
                (double? sell, double? buy) = rg.GetRates(out Exception ex);
                if (ex != null || ((!sell.HasValue) && (!buy.HasValue)))
                    Console.WriteLine($"Current Rate NOT FOUND [{ex?.Message}]");
                else
                {
                    string sText = $"{sell.Value:000.00000000}";
                    string bText = $"{buy.Value:000.00000000}";
                    Console.Write("SELL ");
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write(sText);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" BUY ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(bText);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($" {rg.name}");
                    AppendRate(rg.name, sell.Value, buy.Value);
                };
            };
            Console.WriteLine("");
        }

        private static void GetUpdatePeriod(out double period)
        {
            bool entered = false;

            Console.Write($"Enter Update period (default 30 min) : ");
            string perRate = Conso1e.ReadLine(9900, out entered).Replace(",", ".").Trim();
            if (!entered) Console.WriteLine();
            if (!double.TryParse(perRate, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double perVal)) perVal = 30.0;
            if (perVal < 1) perVal = 1;
            if (perVal > 1440) perVal = 1440;
            period = perVal;
        }

        private static void GetParameters(string forElement, out double min, out double max)
        {
            bool entered = false;

            Console.Write($"Enter Min Alarm Rate for {forElement} (default -inf)  : ");
            string minRate = Conso1e.ReadLine(9900, out entered).Replace(",", ".").Trim();
            if (!entered) Console.WriteLine();
            if (!double.TryParse(minRate, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double minVal)) minVal = double.MinValue;

            Console.Write($"Enter Max Alarm Rate for {forElement} (default +inf)  : ");
            string maxRate = Conso1e.ReadLine(9900, out entered).Replace(",", ".").Trim();
            if (!entered) Console.WriteLine();
            if (!double.TryParse(maxRate, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double maxVal)) maxVal = double.MaxValue;

            min = minVal;
            max = maxVal;
        }

        private static void WriteParameters(string forElement, double minVal, double maxVal)
        {
            string min = minVal == double.MinValue ? "-inf" : $"{minVal:000.00000000}";
            string max = maxVal == double.MaxValue ? "+inf" : $"{maxVal:000.00000000}";
            Console.WriteLine($"Min {forElement} Set to: {min}");
            Console.WriteLine($"Max {forElement} Set to: {max}");
        }

        public static void Start()
        {
            double updatePeriod, minSell, maxSell, minBuy, maxBuy;

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            WriteHeader();
            WriteGrabbers();
            WriteCurrentRates();
            GetUpdatePeriod(out updatePeriod);
            GetParameters("SELL", out minSell, out maxSell);
            GetParameters("BUY", out minBuy, out maxBuy);
            Console.WriteLine();
            Console.WriteLine($"Update Interval (m) : {updatePeriod}");
            WriteParameters("SELL", minSell, maxSell);
            WriteParameters("BUY", minBuy, maxBuy);
            Console.WriteLine();
            Console.WriteLine("Starting watcher...");
            Loop(updatePeriod, minSell, maxSell, minBuy, maxBuy);
        }

        private static void Loop(double perVal, double minSell, double maxSell, double minBuy, double maxBuy)
        {
            while (true)
            {
                int ctr = (int)perVal * 60;
                int left = Console.CursorLeft;

                while (ctr-- > 0)
                {
                    Console.Write($"Next check in {ctr} sec: {DateTime.Now.AddSeconds(ctr)} ");
                    System.Threading.Thread.Sleep(1000);
                    Console.CursorLeft = left;
                };

                Console.Write($"{DateTime.Now} Checking rate...                  ");
                Console.CursorLeft = left;

                foreach (RateGrabber rg in Grabbers)
                {
                    double? sell = null, buy = null;
                    Exception ex;
                    bool ok = false;
                    bool writeIfNoErr = true;


                    try { (sell, buy) = rg.GetRates(out ex); ok = sell.HasValue | buy.HasValue; }
                    catch (Exception e) { Console.WriteLine($"{DateTime.Now} Error checking rate for {rg.name}: {e.Message}"); writeIfNoErr = false; };

                    if (ok)
                    {
                        bool flash = false;
                        string sText = sell.HasValue ? $"{sell.Value:000.00000000}" : "UNKNOWN";
                        string bText = buy.HasValue ? $"{buy.Value:000.00000000}" : "UNKNOWN";

                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write($"{DateTime.Now} ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("SELL ");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        if (sell.HasValue && sell.Value <= minSell) { Console.ForegroundColor = ConsoleColor.Green; flash = true; };
                        if (sell.HasValue && sell.Value >= maxSell) { Console.ForegroundColor = ConsoleColor.Red; flash = true; };
                        Console.Write(sText);
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(" BUY ");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        if (buy.HasValue && buy.Value <= minBuy) { Console.ForegroundColor = ConsoleColor.Green; flash = true; };
                        if (buy.HasValue && buy.Value >= maxBuy) { Console.ForegroundColor = ConsoleColor.Red; flash = true; };
                        Console.Write(bText);
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($" {rg.name}");

                        if (flash)
                        {
                            try { SetForegroundWindow(GetConsoleWindow()); } catch { };
                            try { FlashWindow.Flash(GetConsoleWindow(), 8); } catch { };
                        };

                        AppendRate(rg.name, sell.Value, buy.Value);
                    }
                    else if (writeIfNoErr)
                    {
                        Console.Write($"{DateTime.Now} ");
                        Console.WriteLine($"Current Rate NOT FOUND");
                    };
                };
            };
        }

        private static void AppendRate(string service, double sell, double buy)
        {
            FileStream fs = null;
            try
            {
                string fName = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "rates_stat.txt");
                fs = new FileStream(fName, FileMode.Append, FileAccess.Write);
                fs.Position = fs.Length;
                StreamWriter sw = new StreamWriter(fs, Encoding.ASCII);
                if (fs.Position == 0) sw.WriteLine("### dkxce BYN Rate ###\r\nDateTime\tSell\tBuy\tService");
                sw.WriteLine($"{DateTime.Now}\t{sell}\t{buy}\t{service}");
                sw.Close();
            }
            finally
            {
                if (fs != null) fs.Close();
            };
        }

        public static void AddGrabber(params RateGrabber[] grabber) { Grabbers.AddRange(grabber); }
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
