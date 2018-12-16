using System.Windows.Controls;
using VideoExtractor.ViewModels;

namespace VideoExtractor.Controls
{
    /// <summary>
    /// Interaction logic for CreateVideo.xaml
    /// </summary>
    public partial class CreateVideo : UserControl
    {
        public CreateVideo()
        {
            InitializeComponent();
            DataContext = new CreateVideoModel(MainWindow.AfterStartCommand);
        }
    }
}
