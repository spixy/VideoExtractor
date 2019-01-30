using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using JetBrains.Annotations;
using VideoExtractor.Commands;
using VideoExtractor.Services;

namespace VideoExtractor.ViewModels
{
    public class ExtractAudioModel : ViewModelBase
    {
        private const string TimeSpanFormat = @"hh\:mm\:ss\.fff";
        [NotNull]
        private static readonly List<string> defaultItem = new List<string> {"Default"};

        private string _inputFile;
        private string _outputFile;
        private int _durationSec;
        private int _startingTimeSec;
        private string _startingTime;
        private string _durationTime;

        public ExtractAudioModel(ICommand afterStartCommand)
        {
            StartCommand = new RelayCommand(StartButtonClick, param => CanExecute).AddPostCommand(afterStartCommand);
            InputFileCommand = new RelayCommand(InputFileButtonClick);
            OutputFileCommand = new RelayCommand(OutputFileButtonClick);

            ChannelCount = ChannelCounts?[0];
            SampleRate = SampleRates?[0];
            Bitrate = Bitrates?[0];
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

        public string OutputFile
        {
            get => _outputFile;
            set
            {
                _outputFile = value;
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
                _durationTime = Utility.GetTimeSpanText(_durationTime, value,  TimeSpanFormat);
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

        public string ChannelCount { get; set; }
        public string SampleRate { get; set; }
        public string Bitrate { get; set; }
        public List<string> ChannelCounts { get; } = defaultItem.Union(Enumerable.Range(1, 12).Select(x => x.ToString())).ToList();
        public List<string> SampleRates { get; } = defaultItem.Union(new List<int> {11_025, 16_000, 22_050, 44_100, 48_000, 88_200, 96_000, 192_000}.Select(x => x.ToString())).ToList();
        public List<string> Bitrates { get; } = defaultItem.Union(new List<int> {64, 96, 112, 128, 160, 192, 224, 256, 320}.Select(x => x.ToString())).ToList();

        public bool CanExecute => File.Exists(InputFile) && DurationSec > 0 && !string.IsNullOrEmpty(OutputFile);

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
            JobInfo jobInfo = JobInfo.ExtractAudio(InputFile, OutputFile, SampleRate, Bitrate, ChannelCount, StartingTime, DurationTime, false);
        }
    }
}
