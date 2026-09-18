using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiociariaGuerraBot.ConsoleApp
{
    internal class Logger
    {
        public static string _logPath = ".";

        public static void Log(string messaggio, bool newLine = true)
        {
            if (newLine)
            {
                Console.WriteLine(messaggio);

                File.AppendAllText(_logPath, messaggio + Environment.NewLine);
            }
            else
            {
                Console.Write(messaggio);

                File.AppendAllText(_logPath, messaggio);
            }
        }
    }
}
