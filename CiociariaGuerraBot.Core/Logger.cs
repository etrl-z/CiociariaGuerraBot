using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiociariaGuerraBot.Core
{
    public class Logger
    {
        public static string _logPath = ".";

        public static void Log(string message, bool newLine = true)
        {
            if (newLine)
            {
                Console.WriteLine(message);

                File.AppendAllText(_logPath, message + Environment.NewLine);
            }
            else
            {
                Console.Write(message);

                File.AppendAllText(_logPath, message);
            }
        }
    }
}
