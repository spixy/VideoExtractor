using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Input;
using JetBrains.Annotations;

using FileDialog = Microsoft.Win32.FileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace VideoExtractor.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
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
        /// Opens OpenFileDialog
        /// </summary>
        /// <param name="folder">FileName if success</param>
        /// <returns>if success</returns>
        protected bool TryOpenFolder(out string folder)
        {
            return TryGetFolderName(new FolderBrowserDialog(), out folder);
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

        private static bool TryGetFolderName([NotNull]FolderBrowserDialog dialog, out string folder)
        {
            var result = dialog.ShowDialog();
            if (result == DialogResult.Yes || result == DialogResult.OK)
            {
                folder = dialog.SelectedPath;
                return true;
            }

            folder = null;
            return false;
        }
    }
}
