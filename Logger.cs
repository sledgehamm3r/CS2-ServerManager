using System;
using System.IO;

namespace CS2ServerManager
{
    public static class Logger
    {
        private static readonly string logFilePath;

        static Logger()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string baseFolder = Path.Combine(documentsPath, "CS2ServerManager");
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }
            logFilePath = Path.Combine(baseFolder, "debugs.txt");
        }

        public static void Log(string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(logFilePath, logEntry);
            }
            catch
            {
            }
        }
    }
}
