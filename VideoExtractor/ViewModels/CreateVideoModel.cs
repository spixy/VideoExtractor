using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using VideoExtractor.Commands;

namespace VideoExtractor.ViewModels
{
    public class CreateVideoModel : ViewModelBase
    {
        private string _inputFolder;
        private string _outputFile;

        public CreateVideoModel(ICommand afterStartCommand)
        {
            StartCommand = new RelayCommand(StartButtonClick, param => CanExecute).AddPostCommand(afterStartCommand);
            InputFileCommand = new RelayCommand(InputFileButtonClick);
            OutputFileCommand = new RelayCommand(OutputFileButtonClick);

            InputFormat = InputFormats?[0];
            OutFormat = OutFormats?[0];
            Framerate = 60;
        }

        public ICommand InputFileCommand { get; protected set; }
        public ICommand OutputFileCommand { get; protected set; }

        public string InputFolder
        {
            get => _inputFolder;
            set
            {
                _inputFolder = value;
                OnPropertyChanged();
            }
        }

        public string OutputFile
        {
            get => _outputFile;
            set
            {
                _outputFile = value;
                OnPropertyChanged();
            }
        }

        public string InputFormat { get; set; }
        public string OutFormat { get; set; }
        public int Framerate { get; set; }
        public List<string> InputFormats { get; } = new List<string> { "image_%d.bmp", "image_%d.jpg", "image_%d.png" };
        public List<string> OutFormats { get; } = new List<string> { "AVI", "FLV", "MOV", "MKV", "MP4", "WEBM", "WMV" };
        public List<int> Framerates { get; } = new List<int> { 1, 2, 3, 4, 5, 10, 15, 20, 24, 25, 30, 48, 50, 60, 120 };

        public bool CanExecute => File.Exists(InputFolder) && !string.IsNullOrEmpty(OutputFile);

        public void InputFileButtonClick(object sender)
        {
            if (TryOpenFolder(out string folder))
            {
                InputFolder = folder;
            }
        }

        public void OutputFileButtonClick(object sender)
        {
            if (TrySaveFile(out string file))
            {
                OutputFile = file;
            }
        }

        public void StartButtonClick(object sender)
        {
            // TODO
        }
    }
}
