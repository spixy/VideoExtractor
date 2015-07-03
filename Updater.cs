using System;
using System.ComponentModel;
using System.Net;
using System.Reflection;

namespace VideoExtractor
{
    public class Updater
    {
        public string CurrentVersion { get; set; }

        public string UpdateFile { get; set; }

        public Action UpdateAvailableAction { get; set; }


        public Updater(string updateFile)
        {
            UpdateFile = updateFile;
            CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public void IsUpdateAvailableAsync()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += IsUpdateAvailableBackground;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.RunWorkerAsync();
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (((bool)e.Result) && UpdateAvailableAction != null)
            {
                UpdateAvailableAction();
            }
        }

        private void IsUpdateAvailableBackground(object sender, DoWorkEventArgs e)
        {
            e.Result = IsUpdateAvailable();
        }
        
        public bool IsUpdateAvailable()
        {
            try
            {
                WebClient update = new WebClient();
                string new_ver = update.DownloadString(UpdateFile);
                update.Dispose();

                return (new_ver != CurrentVersion);
            }
            catch
            {
                return false;
            }
        }
    }
}
