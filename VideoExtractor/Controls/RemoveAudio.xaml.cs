using System.Windows.Controls;
using VideoExtractor.ViewModels;

namespace VideoExtractor.Controls
{
    /// <summary>
    /// Interaction logic for RemoveAudio.xaml
    /// </summary>
    public partial class RemoveAudio : UserControl
    {
        public RemoveAudio()
        {
            InitializeComponent();
            DataContext = new RemoveAudioModel(MainWindow.AfterStartCommand);
        }
    }
}
