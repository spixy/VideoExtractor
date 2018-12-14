using System.ComponentModel;
using System.Windows.Input;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace VideoExtractor.ViewModels
{
    public abstract class ActionTabViewModelBase : INotifyPropertyChanged
    {
        private const string FilterVideo = "AVI|*.avi|FLV|*.flv|MOV|*.mov|MKV|*.mkv|MP4|*.mp4|OGG|*.ogg|WEBM|*.webm|WMV|*.wmv|All files|*.*";

        public ICommand StartCommand { get; protected set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Opens OpenFileDialog
        /// </summary>
        /// <param name="file">FileName if success</param>
        /// <returns>if success</returns>
        protected bool TryOpenFile(out string file)
        {
            return TryGetFileName(new OpenFileDialog {Filter = FilterVideo}, out file);
        }

        /// <summary>
        /// Opens SaveFileDialog
        /// </summary>
        /// <param name="file">FileName if success</param>
        /// <returns>if success</returns>
        protected bool TrySaveFile(out string file)
        {
            return TryGetFileName(new SaveFileDialog {Filter = FilterVideo}, out file);
        }
        
        private static bool TryGetFileName([NotNull]FileDialog dialog, out string file)
        {
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                file = dialog.FileName;
                return true;
            }

            file = null;
            return false;
        }
    }
}
