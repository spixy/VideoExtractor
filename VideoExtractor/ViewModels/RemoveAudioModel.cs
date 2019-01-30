using System.IO;
using System.Windows.Input;
using VideoExtractor.Commands;

namespace VideoExtractor.ViewModels
{
    public class RemoveAudioModel : ViewModelBase
    {
        private string _inputFile;
        private string _outputFile;

        public RemoveAudioModel(ICommand afterStartCommand)
        {
            StartCommand = new RelayCommand(StartButtonClick, param => CanExecute).AddPostCommand(afterStartCommand);
            InputFileCommand = new RelayCommand(InputFileButtonClick);
            OutputFileCommand = new RelayCommand(OutputFileButtonClick);
        }

        public ICommand InputFileCommand { get; protected set; }
        public ICommand OutputFileCommand { get; protected set; }

        public bool CanExecute => File.Exists(InputFile) && !string.IsNullOrEmpty(OutputFile);

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
