using System.Windows.Controls;
using VideoExtractor.ViewModels;

namespace VideoExtractor.Controls
{
    /// <summary>
    /// Interaction logic for ExtractAudio.xaml
    /// </summary>
    public partial class ExtractAudio : UserControl
    {
        public ExtractAudio()
        {
            InitializeComponent();
            DataContext = new ExtractAudioModel(MainWindow.AfterStartCommand);
        }
    }
}
