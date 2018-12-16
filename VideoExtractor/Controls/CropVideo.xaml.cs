using System.Windows.Controls;
using VideoExtractor.ViewModels;

namespace VideoExtractor.Controls
{
    /// <summary>
    /// Interaction logic for CropVideo.xaml
    /// </summary>
    public partial class CropVideo : UserControl
    {
        public CropVideo()
        {
            InitializeComponent();
            DataContext = new CropVideoModel(MainWindow.AfterStartCommand);
        }
    }
}
