using System.Windows.Controls;
using VideoExtractor.ViewModels;

namespace VideoExtractor.Controls
{
    /// <summary>
    /// Interaction logic for ExtractImages.xaml
    /// </summary>
    public partial class ExtractImages : UserControl
    {
        public ExtractImages()
        {
            InitializeComponent();
            DataContext = new ExtractImagesModel(MainWindow.AfterStartCommand);
        }
    }
}
