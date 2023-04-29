using System;
using System.Collections.Generic;

namespace mchost.Logging
{
    public static class Logger
    {
        private static string _prefix = "[mchost] ";

        private static List<string> Logs = new List<string>();

        public static void Log(string message)
        {
            string log = _prefix + GetTimePrefix() + " " + message;
            
            Logs.Add(log);
            Console.WriteLine(log);
        }

        public static void ContinueWriteLog()
        {
            int i = _index;
            try
            {
                using (StreamWriter sw = File.AppendText("mchost.log"))
                {
                    for (; i < Logs.Count; i++)
                    {
                        sw.WriteLine(Logs[i]);
                    }
                }
            }
            catch(Exception)
            {

            }

            _index = i;
        }

        private static string GetTimePrefix() => DateTime.Now.ToString("HH:mm:ss");

        public static void SetPrefix(string prefix) => _prefix = prefix;

        public static List<string> GetLogs() => Logs;

        private static int _index = 0;
    }
}