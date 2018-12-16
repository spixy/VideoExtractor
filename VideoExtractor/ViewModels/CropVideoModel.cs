using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using JetBrains.Annotations;
using VideoExtractor.Commands;

namespace VideoExtractor.ViewModels
{
    public class CropVideoModel : ViewModelBase
    {
        [NotNull]
        private static readonly List<object> defaultItem = new List<object> { "Default" };

        private string _inputFile;
        private string _outputFile;
        private bool _isInCenter;

        public CropVideoModel(ICommand afterStartCommand)
        {
            StartCommand = new RelayCommand(StartButtonClick, param => CanExecute).CreateNextCommand(afterStartCommand);
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

        public int? X { get; set; }
        public int? Y { get; set; }

        public bool IsInCenter
        {
            get => _isInCenter;
            set
            {
                _isInCenter = value;
                OnPropertyChanged(nameof(IsInCenter));
                OnPropertyChanged(nameof(PositionEnabled));
            }
        }

        public bool PositionEnabled => !_isInCenter;

        public int? Width { get; set; }
        public int? Height { get; set; }

        public bool CanExecute => File.Exists(InputFile) && !string.IsNullOrEmpty(OutputFile);

        public IEnumerable<object> Channels { get; } = defaultItem;

        public IEnumerable<object> Bitrates { get; } = defaultItem;

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
