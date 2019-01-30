using System.Media;
using System.Windows;
using System.Windows.Input;
using VideoExtractor.Commands;
using VideoExtractor.Services;

namespace VideoExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ICommand AfterStartCommand { get; private set; }

        public MainWindow()
        {
            AfterStartCommand = new RelayCommand(OnExecuted);
            InitializeComponent();
        }

        private void OnExecuted(object arg)
        {
            JobInfo jobInfo = (JobInfo)arg;

            if (jobInfo == null)
            {
                OnError();
                return;
            }

            switch (jobInfo.Result)
            {
                case JobInfo.EResult.Success:
                    OnSuccess(jobInfo);
                    break;

                case JobInfo.EResult.Error:
                    OnError(jobInfo);
                    break;

                case JobInfo.EResult.Cancel:
                    OnCancel(jobInfo);
                    break;
            }
        }

        public void OnSuccess(JobInfo jobInfo)
        {
            /*if (openExplorer && !Utility.LaunchExplorer(jobInfo.Output))
            {
                SystemSounds.Hand.Play();
            }*/
        }

        public void OnError(JobInfo jobInfo = null)
        {
            SystemSounds.Hand.Play();

            if (jobInfo != null)
            {
                OnCancel(jobInfo);
            }
        }

        public void OnCancel(JobInfo jobInfo)
        {
            //Utility.RemovePath(jobInfo.Output);
        }
    }
}
