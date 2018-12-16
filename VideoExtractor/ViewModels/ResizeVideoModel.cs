using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using VideoExtractor.Commands;

namespace VideoExtractor.ViewModels
{
    public class ResizeVideoModel : ViewModelBase
    {
        private string _inputFile;
        private string _outputFile;

        public ResizeVideoModel(ICommand afterStartCommand)
        {
            StartCommand = new RelayCommand(StartButtonClick, param => CanExecute).CreateNextCommand(afterStartCommand);
            InputFileCommand = new RelayCommand(InputFileButtonClick);
            OutputFileCommand = new RelayCommand(OutputFileButtonClick);

            Format = Formats?[0];
        }

        public ICommand InputFileCommand { get; protected set; }
        public ICommand OutputFileCommand { get; protected set; }

        public string InputFile
        {
            get => _inputFile;
            set
            {
                _inputFile = value;
                OnPropertyChanged(nameof(InputFile));
            }
        }

        public string OutputFile
        {
            get => _outputFile;
            set
            {
                _outputFile = value;
                OnPropertyChanged(nameof(OutputFile));
            }
        }

        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? VideoBitrate { get; set; }
        public int? AudioBitrate { get; set; }

        public string Format { get; set; }

        public List<string> Formats { get; set; } = new List<string> { "AVI", "FLV", "MOV", "MKV", "MP4", "WEBM", "WMV" };

        public bool CanExecute => File.Exists(InputFile) && !string.IsNullOrEmpty(OutputFile);

        public void InputFileButtonClick(object sender)
        {
            if (TryOpenFile(out string file))
            {
                InputFile = file;
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
