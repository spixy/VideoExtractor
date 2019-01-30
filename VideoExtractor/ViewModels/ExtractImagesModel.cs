using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using VideoExtractor.Commands;
using VideoExtractor.Services;

namespace VideoExtractor.ViewModels
{
    public class ExtractImagesModel : ViewModelBase
    {
        private const string TimeSpanFormat = @"hh\:mm\:ss\.fff";

        private string _inputFile;
        private string _outputFolder;
        private int _durationSec;
        private int _startingTimeSec;
        private string _startingTime;
        private string _durationTime;

        public ExtractImagesModel(ICommand afterStartCommand)
        {
            StartCommand = new RelayCommand(StartButtonClick, param => CanExecute).AddPostCommand(afterStartCommand);
            InputFileCommand = new RelayCommand(InputFileButtonClick);
            OutputFileCommand = new RelayCommand(OutputFileButtonClick);

            Format = Formats?[0];
            Framerate = 30;
            StartingTimeSec = 0;
            DurationSec = 0;
        }

        public ICommand InputFileCommand { get; protected set; }
        public ICommand OutputFileCommand { get; protected set; }

        public string InputFile
        {
            get => _inputFile;
            set
            {
                _inputFile = value;
                OnPropertyChanged();
            }
        }

        public string OutputFolder
        {
            get => _outputFolder;
            set
            {
                _outputFolder = value;
                OnPropertyChanged();
            }
        }

        public int StartingTimeSec
        {
            get => _startingTimeSec;
            set
            {
                _startingTimeSec = value;
                _startingTime = Utility.GetTimeSpanText(_startingTime, value, TimeSpanFormat);
                OnPropertyChanged();
            }
        }

        public string StartingTime
        {
            get => _startingTime;
            set
            {
                _startingTime = value;
                if (Utility.TryGetTotalSeconds(value, out _startingTimeSec))
                {
                    OnPropertyChanged();
                }
            }
        }

        public int DurationSec
        {
            get => _durationSec;
            set
            {
                _durationSec = value;
                _durationTime = Utility.GetTimeSpanText(_durationTime, value, TimeSpanFormat);
                OnPropertyChanged();
            }
        }

        public string DurationTime
        {
            get => _durationTime;
            set
            {
                _durationTime = value;
                if (Utility.TryGetTotalSeconds(value, out _durationSec))
                {
                    OnPropertyChanged();
                }
            }
        }

        public int? Width { get; set; } = null;
        public int? Height { get; set; } = null;
        public string Format { get; set; }
        public int Framerate { get; set; }

        public bool CanExecute => File.Exists(InputFile) && DurationSec > 0 && !string.IsNullOrEmpty(OutputFolder);

        public List<string> Formats { get; } = new List<string> { "BMP", "JPG", "PNG" };

        public List<int> Framerates { get; } = new List<int> { 1,2,3,4,5,10,15,20,24,25,30,48,50,60,120 };

        public void InputFileButtonClick(object sender)
        {
            if (TryOpenFile(out string file))
            {
                InputFile = file;
            }
        }

        public void OutputFileButtonClick(object sender)
        {
            if (TryOpenFolder(out string file))
            {
                OutputFolder = file;
            }
        }

        public void StartButtonClick(object sender)
        {
            // TODO
        }
    }
}
