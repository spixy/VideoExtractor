using System;
using System.IO;

namespace VideoExtractor
{
    public class Logger
    {
        public string LogFile { get; set; }
        public bool Enabled { get; set; }
        public bool Rewrite { get; set; }

        public Logger()
        { }

        public Logger(string file, bool enabled = true)
        {
            LogFile = file;
            Enabled = enabled;
        }

        public void Clear()
        {
            File.WriteAllBytes(LogFile, new Byte[0]);
        }

        public void Log(string err)
        {
            if (Enabled)
            {
                using (StreamWriter file = new StreamWriter(LogFile, !Rewrite))
                    file.WriteLine(err);
            }
        }

        public void Log(Exception ex)
        {
            Log(ex.Message);
        }
    }
}
