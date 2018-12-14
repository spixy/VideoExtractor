using System;
using System.ComponentModel;
using System.Net;

namespace VideoExtractor.Services
{
    /// <summary>
    /// Legacy updater
    /// </summary>
    public class Updater
    {
        private readonly string updateFileUrl;

#if WINDOWS_UWP
		private readonly Version _currentAppVersion = Windows.ApplicationModel.Package.Current.Version;
#else
        private readonly Version _currentAppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
#endif

        /// <summary>
        /// Latest application version
        /// </summary>
        public string LatestVersion { get; private set; }

        /// <summary>
        /// Asynchronous action occurs when update is available after calling IsUpdateAvailableAsync()
        /// </summary>
        public Action UpdateAvailableAction { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="updateFileUrl">File with latest application version</param>
        public Updater(string updateFileUrl)
        {
            this.updateFileUrl = updateFileUrl;
        }

        /// <summary>
        /// Asynchroniously check if update is available
        /// </summary>
        public void CheckForUpdateAvailableAsync()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += IsUpdateAvailableBackground;
            bw.RunWorkerCompleted += Bw_RunWorkerCompleted;
            bw.RunWorkerAsync();
        }

        private void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((bool)e.Result)
            {
                UpdateAvailableAction?.Invoke();
            }
        }

        private void IsUpdateAvailableBackground(object sender, DoWorkEventArgs e)
        {
            e.Result = IsUpdateAvailable();
        }

        /// <summary>
        /// Check if update is available
        /// </summary>
        public bool IsUpdateAvailable()
        {
            try
            {
                using (WebClient update = new WebClient())
                {
                    LatestVersion = update.DownloadString(updateFileUrl).Trim();
                }

                string currentVersion = _currentAppVersion.ToString().Trim();
                return currentVersion != LatestVersion;
            }
            catch
            {
                return false;
            }
        }
    }
}
