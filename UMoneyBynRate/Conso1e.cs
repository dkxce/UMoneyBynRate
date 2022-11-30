///////////////////////////////////
//     dkxce System.Conso1e      //
///////////////////////////////////

namespace System
{
    internal class Conso1e
    {
        private static Thread inputThread;
        private static AutoResetEvent getInput, gotInput;
        private static string input;

        static Conso1e()
        {
            getInput = new AutoResetEvent(false);
            gotInput = new AutoResetEvent(false);
            inputThread = new Thread(reader);
            inputThread.IsBackground = true;
            inputThread.Start();
        }

        private static void reader()
        {
            while (true)
            {
                getInput.WaitOne();
                input = Console.ReadLine();
                gotInput.Set();
            }
        }

        public static string ReadLine()
        {
            return Console.ReadLine();
        }

        public static string ReadLine(int timeOutMillisecs)
        {
            getInput.Set();
            bool success = gotInput.WaitOne(timeOutMillisecs);
            if (success)
                return input;
            else
                return "";
        }

        public static string ReadLine(int timeOutMillisecs, out bool success)
        {
            getInput.Set();
            success = gotInput.WaitOne(timeOutMillisecs);
            if (success)
                return input;
            else
                return "";
        }

        public static void AutoClose(int timeOutMillisecs, string text = "Done, Press Enter to close (autoclose in {0} seconds)...")
        {
            int wait = timeOutMillisecs / 1000;
            (new Thread(() => {
                int top = Console.CursorTop;
                while (wait > 0)
                {
                    Console.SetCursorPosition(0, top);
                    Console.WriteLine(text, wait--);
                    Thread.Sleep(1000);
                };
            })).Start();
            Conso1e.ReadLine(timeOutMillisecs);
            wait = 0;
        }
    }    
}
