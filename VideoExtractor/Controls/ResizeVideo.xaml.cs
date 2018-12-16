using System.Windows.Controls;
using VideoExtractor.ViewModels;

namespace VideoExtractor.Controls
{
    /// <summary>
    /// Interaction logic for ResizeVideo.xaml
    /// </summary>
    public partial class ResizeVideo : UserControl
    {
        public ResizeVideo()
        {
            InitializeComponent();
            DataContext = new ResizeVideoModel(MainWindow.AfterStartCommand);
        }
    }
}
