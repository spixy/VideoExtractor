using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using JetBrains.Annotations;
using VideoExtractor.Commands;

namespace VideoExtractor.ViewModels
{
    public class ExtractAudioViewModel : ActionTabViewModelBase
    {
        private const string TimeSpanFormat = @"hh\:mm\:ss\.fff";
        [NotNull]
        private static readonly List<object> defaultItem = new List<object> {"Default"};

        private string _inputFile;
        private string _outputFile;
        private int _durationSec;
        private int _startingTimeSec;
        private string _startingTime;
        private string _durationTime;

        public ExtractAudioViewModel()
        {
            StartCommand = new RelayCommand(StartButtonClick, param => CanExecute);
            InputFileCommand = new RelayCommand(InputFileButtonClick);
            OutputFileCommand = new RelayCommand(OutputFileButtonClick);
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

        public int StartingTimeSec
        {
            get => _startingTimeSec;
            set
            {
                _startingTimeSec = value;
                _startingTime = GetTimeSpanText(_startingTime, value);
                OnPropertyChanged(nameof(StartingTime));
            }
        }

        public string StartingTime
        {
            get => _startingTime;
            set
            {
                _startingTime = value;
                if (TryGetTotalSeconds(value, out _startingTimeSec))
                {
                    OnPropertyChanged(nameof(StartingTimeSec));
                }
            }
        }

        public int DurationSec
        {
            get => _durationSec;
            set
            {
                _durationSec = value;
                _durationTime = GetTimeSpanText(_durationTime, value);
                OnPropertyChanged(nameof(DurationTime));
            }
        }

        public string DurationTime
        {
            get => _durationTime;
            set
            {
                _durationTime = value;
                if (TryGetTotalSeconds(value, out _durationSec))
                {
                    OnPropertyChanged(nameof(DurationSec));
                }
            }
        }

        public int ChannelCountIndex { get; set; }
        public int SampleRateIndex { get; set; }
        public int BitrateIndex { get; set; }

        public bool CanExecute => File.Exists(InputFile) && DurationSec > 0 && !string.IsNullOrEmpty(OutputFile);

        public IEnumerable<object> Channels { get; }
            = defaultItem.Union(Enumerable.Range(1, 12).Cast<object>());

        public IEnumerable<object> SampleRates { get; }
            = defaultItem.Union(new List<object> {11_025, 16_000, 22_050, 44_100, 48_000, 88_200, 96_000, 192_000});

        public IEnumerable<object> Bitrates { get; }
            = defaultItem.Union(new List<object> {64, 96, 112, 128, 160, 192, 224, 256, 320});

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

        /// <summary>
        /// Transforms seconds to string
        /// </summary>
        /// <param name="oldString">old value string (from TimeSpan.ToString)</param>
        /// <param name="newSeconds">new value in seconds</param>
        /// <returns>"hh:mm:ss" string</returns>
        private static string GetTimeSpanText(string oldString, int newSeconds)
        {
            int ms = TimeSpan.TryParseExact(oldString, TimeSpanFormat, null, out TimeSpan ts) ? ts.Milliseconds : 0;
            return new TimeSpan(0, 0, 0, newSeconds, ms).ToString(TimeSpanFormat);
        }

        /// <summary>
        /// Transforms string to seconds
        /// </summary>
        /// <param name="text">"hh:mm:ss" string</param>
        /// <param name="result">number of seconds</param>
        /// <returns>if success</returns>
        private static bool TryGetTotalSeconds(string text, out int result)
        {
            try
            {
                result = (int) new TimeSpan(int.Parse(text.Substring(0, 2)),
                                            int.Parse(text.Substring(3, 2)),
                                            int.Parse(text.Substring(6, 2))).TotalSeconds;
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }
}
