using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace VideoExtractor
{
    public partial class MainForm : Form
    {
        #region PROPERTIES

        private const string FilterVideoAndGif = "AVI|*.avi|FLV|*.flv|GIF|*.gif|MOV|*.mov|MKV|*.mkv|MP4|*.mp4|OGG|*.ogg|WEBM|*.webm|WMV|*.wmv|All files|*.*";
        private const string FilterVideo = "AVI|*.avi|FLV|*.flv|MOV|*.mov|MKV|*.mkv|MP4|*.mp4|OGG|*.ogg|WEBM|*.webm|WMV|*.wmv|All files|*.*";
        private const string FilterAudio = "MP2|*.mp2|MP3|*.mp3|MP4|*.mp4|M4A|*.m4a|WAV|*.wav|OGG|*.ogg|WMA|*.wma|All files|*.*";

        private bool openExplorer = true;
        private bool checkUpdates = true;
        private bool overwriteFiles = false;
        private bool outputEnabled = false;
        private string ffmpeg = "ffmpeg.exe";

        private readonly Logger logger = new Logger(Properties.Resources.LogFile, false);
        private readonly List<Process> processes = new List<Process>();

        private OpenFileDialog _openFileDialog;
        private OpenFileDialog OpenFileDialog => _openFileDialog ?? (_openFileDialog = new OpenFileDialog());

        private SaveFileDialog _saveFileDialog;
        private SaveFileDialog SaveFileDialog => _saveFileDialog ?? (_saveFileDialog = new SaveFileDialog());

        private FolderBrowserDialog _folderBrowserDialog;
        private FolderBrowserDialog FolderBrowserDialog => _folderBrowserDialog ?? (_folderBrowserDialog = new FolderBrowserDialog());

        #endregion

        #region GENERAL

        public MainForm()
        {
            InitializeComponent();

            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 10;
            comboBox5.SelectedIndex = 0;
            comboBox6.SelectedIndex = 1;
            comboBox9.SelectedIndex = 1;
            comboBox10.SelectedIndex = 10;

            tabControl1.TabPages.Remove(outputTab);

            label15.Text = Application.ProductVersion;

            int bits = IntPtr.Size * 8;
            label19.Text = bits + "-bit";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MinimumSize = MaximumSize = Size;      

            logger.Clear();

            LoadSettings();

            if (checkUpdates)
            {
                Updater updater = new Updater(Properties.Resources.UpdateFile);
                updater.UpdateAvailableAction = () => linkLabel3.Visible = true;
                updater.CheckForUpdateAvailableAsync();
            }
        }

        private void LoadSettings()
        {
            try
            {
                string[] lines = File.ReadAllLines(Properties.Resources.ConfigFile);

                foreach (string line in lines)
                {
                    if (line.ToLower().Contains("no windows")) checkBox1.Checked = false;
                    if (line.ToLower().Contains("no update")) checkBox2.Checked = false;
                    if (line.ToLower().Contains("file log")) checkBox3.Checked = true;
                    if (line.ToLower().Contains("output")) checkBox5.Checked = true;
                    if (line.ToLower().Contains("overwrite")) checkBox7.Checked = true;
                    if (line.ToLower().Contains("ffmpeg ")) textBox9.Text = line.ToLower().Replace("ffmpeg ", "").Trim();
                    if (line.ToLower().Contains("last tab "))
                    {
                        if (int.TryParse(line.ToLower().Replace("last tab ", "").Trim(), out int index) && index >= 0 && index <= 5)
                            tabControl1.SelectedIndex = index;
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void SaveSettings()
        {
            using (StreamWriter file = new StreamWriter(Properties.Resources.ConfigFile, false))
            {
                if (!checkBox1.Checked) file.WriteLine("no windows");
                if (!checkBox2.Checked) file.WriteLine("no update");
                if (checkBox3.Checked) file.WriteLine("file log");
                if (checkBox5.Checked) file.WriteLine("output");
                if (checkBox6.Checked) file.WriteLine("last tab " + tabControl1.SelectedIndex);
                if (checkBox7.Checked) file.WriteLine("overwrite");
                file.WriteLine("ffmpeg " + textBox9.Text);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSettings();

            foreach (Process p in processes)
            {
                try
                {
                    p.Kill();
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ((e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.TaskManagerClosing) &&
                processes.Count > 0 &&
                MessageBox.Show("Process is still running. Do you want to cancel process?", "Warning", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void OutputTabLog(string text)
        {
            if (!outputEnabled)
                return;

            if (outputLog.InvokeRequired)
            {
                outputLog.Invoke((MethodInvoker)delegate
                {
                    outputLog.Text += text + Environment.NewLine;
                });
            }
            else
            {
                outputLog.Text += text + Environment.NewLine;
            }
        }

        private void Log(string text)
        {
            OutputTabLog(text);
            logger.Log(text);
        }

        private void RunAsync(JobInfo jobInfo)
        {
            if (File.Exists(ffmpeg))
            {
                tabControl1.SelectTab(queueTab);

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (obj, ea) => bw_DoWork(ea, jobInfo);
                bw.RunWorkerCompleted += bw_RunWorkerCompleted;
                bw.RunWorkerAsync();
            }
            else
            {
                Log(ffmpeg + " not found");
                OnError();
            }  
        }

        private void bw_DoWork(DoWorkEventArgs e, JobInfo jobInfo)
        {
            Process p = jobInfo.CreateProcess(ffmpeg);

            p.OutputDataReceived += (s, ea) => Log(ea.Data);
            p.ErrorDataReceived += (s, ea) => Log(ea.Data);

            var item = AddToQueue(jobInfo);

            logger.Log(p.StartInfo.FileName + " " + p.StartInfo.Arguments);

            p.Start();

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();

            RemoveFromQueue(item);

            switch (p.ExitCode)
            {
                // Kill()
                case -1:
                    jobInfo.Result = JobInfo.EResult.Cancel;
                    break;

                // OK
                case 0:
                    jobInfo.Result = JobInfo.EResult.Success;
                    break;

                // ffmpeg error
                default:
                    jobInfo.Result = JobInfo.EResult.Error;
                    break;
            }

            e.Result = jobInfo;
            p.Dispose();
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            JobInfo jobInfo = (JobInfo) e.Result;

            if (jobInfo == null)
            {
                OnError();
                return;
            }

            switch (jobInfo.Result)
            {
                case JobInfo.EResult.Success:
                    OnSuccess(jobInfo);
                    break;

                case JobInfo.EResult.Error:
                    OnError(jobInfo);
                    break;

                case JobInfo.EResult.Cancel:
                    OnCancel(jobInfo);
                    break;
            }
        }

        private void OnSuccess(JobInfo jobInfo)
        {
            if (openExplorer && !Utility.LaunchExplorer(jobInfo.Output))
            {
                SystemSounds.Hand.Play();
            }
        }

        private void OnError(JobInfo jobInfo = null)
        {
            SystemSounds.Hand.Play();

            if (jobInfo != null)
                OnCancel(jobInfo);
        }

        private void OnCancel(JobInfo jobInfo)
        {
            Utility.RemovePath(jobInfo.Output);
        }

        private void SetTimeText(Control textControl, TrackBar trackBar)
        {
            string ms = (textControl.Text.Length == 12) ? textControl.Text.Remove(0, 9) : "000";

            textControl.Text = Utility.GetTimeSpanText(trackBar.Value) + "." + ms;
        }

        private void GetSavePath(string filter, Control textControl)
        {
            SaveFileDialog.Filter = filter;
            GetPath(SaveFileDialog, textControl);
        }

        private void GetOpenPath(string filter, Control textControl)
        {
            OpenFileDialog.Filter = filter;
            GetPath(OpenFileDialog, textControl);
        }

        private void GetPath(CommonDialog dialog, Control textControl)
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textControl.Text = OpenFileDialog.FileName;
            }
        }

        #endregion

        #region VALIDATION

        private void comboBox_Validating(object sender, CancelEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;

            bool valid = comboBox.SelectedIndex == 0 || (int.TryParse(comboBox.Text, out int val) && val > 0);

            comboBox.BackColor = (valid) ? SystemColors.Window : Color.Red;
        }

        private void comboBox_double_Validating(object sender, CancelEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;

            bool valid = (double.TryParse(comboBox.Text, out double val) && val > 0);

            comboBox.BackColor = (valid) ? SystemColors.Window : Color.Red;
        }

        private void textBox_Validating(object sender, CancelEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string text = textBox.Text;
            int val;

            bool valid = text.Length == 12 &&
                text[2] == ':' && text[5] == ':' && text[8] == '.' &&
                int.TryParse(text.Substring(0, 2), out val) &&
                int.TryParse(text.Substring(3, 2), out val) &&
                int.TryParse(text.Substring(6, 2), out val) &&
                int.TryParse(text.Substring(9, 3), out val);

            textBox.BackColor = (valid) ? SystemColors.Window : Color.Red;
        }

        #endregion

        #region EXTRACT AUDIO

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(textBox1.Text);

            textBox2.Text = fi.FullName.Replace(fi.Extension, ".mp3");
            button3.Enabled = fi.Exists;

            VideoInfo videoInfo = VideoInfo.LoadVideoInfo(ffmpeg, textBox1.Text);

            if (videoInfo.Channels > 0)
                comboBox5.Items[0] = "Default (" + videoInfo.Channels + ")";

            if (videoInfo.SampleRate > 0)
                comboBox1.Items[0] = "Default (" + videoInfo.SampleRate + ")";

            if (videoInfo.AudioBitRate > 0)
                comboBox2.Items[0] = "Default (" + videoInfo.AudioBitRate + ")";

            if (videoInfo.Duration > 0)
            {
                trackBar3.Maximum = videoInfo.Duration;
                trackBar4.Maximum = videoInfo.Duration;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(textBox2.Text);
            comboBox8.Text = fi.Extension.Remove(0, 1).ToUpper();
        }

        private void comboBox2_TextChanged(object sender, EventArgs e)
        {
            if (comboBox8.Text.ToUpper() == "MP3" && comboBox2.Text != "Default")
            {
                if (int.TryParse(comboBox2.Text, out int val) && val > 320)
                    comboBox2.Text = "320";
            }
        }

        private void comboBox8_TextChanged(object sender, EventArgs e)
        {
            try
            {
                FileInfo fi = new FileInfo(textBox1.Text);
                string str = fi.FullName.Replace(fi.Extension, "") + "." + comboBox8.Text.ToLower();

                if (textBox2.Text != str)
                    textBox2.Text = str;
            }
            catch
            {
                // ignored
            }

            if (comboBox8.Text.ToUpper() == "MP3" && comboBox2.Text != "Default")
            {
                if (int.TryParse(comboBox2.Text, out int val) && val > 320)
                    comboBox2.Text = "320";
            }
        }

        private void textBox16_TextChanged(object sender, EventArgs e)
        {
            try
            {
                trackBar3.Value = Utility.GetTotalSeconds(textBox16.Text);
            }
            catch
            {
                // ignored
            }
        }

        private void textBox17_TextChanged(object sender, EventArgs e)
        {
            try
            {
                trackBar4.Value = Utility.GetTotalSeconds(textBox17.Text);
            }
            catch
            {
                // ignored
            }
        }

        private void trackBar3_ValueChanged(object sender, EventArgs e)
        {
            SetTimeText(textBox16, trackBar3);
        }

        private void trackBar4_ValueChanged(object sender, EventArgs e)
        {
            SetTimeText(textBox17, trackBar4);
            label50.Visible = (trackBar4.Value == 0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GetOpenPath(FilterVideo, textBox1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            GetSavePath(FilterAudio, textBox2);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            JobInfo jobInfo = Utility.ExtractAudio(textBox1.Text, textBox2.Text,
                comboBox1.Text, comboBox2.Text, comboBox5.Text, textBox16.Text, textBox17.Text, overwriteFiles);

            RunAsync(jobInfo);
        }
   
        #endregion

        #region EXTRACT IMAGES

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(textBox3.Text);

            textBox4.Text = fi.FullName.Replace(fi.Extension, "") + "_Images";
            button4.Enabled = fi.Exists;

            VideoInfo videoInfo = VideoInfo.LoadVideoInfo(ffmpeg, textBox3.Text);

            if (videoInfo.Duration > 0)
            {
                trackBar1.Maximum = videoInfo.Duration;
                trackBar2.Maximum = videoInfo.Duration;
            }

            if (videoInfo.Fps > 0)
            {
                comboBox3.Text = videoInfo.Fps.ToString(CultureInfo.InvariantCulture);
            }

            if (videoInfo.Resolution.Width * videoInfo.Resolution.Height > 0)
            {
                numericUpDown2.Value = videoInfo.Resolution.Width;
                numericUpDown8.Value = videoInfo.Resolution.Height;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            GetOpenPath(FilterVideo, textBox3);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog.SelectedPath = textBox3.Text;

            if (FolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                textBox4.Text = FolderBrowserDialog.SelectedPath;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string output = textBox4.Text;

            JobInfo jobInfo = Utility.ExtractImages(textBox3.Text, output,
                (int)numericUpDown2.Value, (int)numericUpDown8.Value, comboBox3.Text.Replace(',', '.'), textBox5.Text, textBox8.Text, overwriteFiles);

            if (!Directory.Exists(output))
            {
                Directory.CreateDirectory(output);
                logger.Log("Directory created: " + output);
            }

            RunAsync(jobInfo);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            SetTimeText(textBox5, trackBar1);
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            SetTimeText(textBox8, trackBar2);
            label11.Visible = (trackBar2.Value == 0);
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            try
            {
                trackBar1.Value = Utility.GetTotalSeconds(textBox5.Text);
            }
            catch
            {
                // ignored
            }
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            try
            {
                trackBar2.Value = Utility.GetTotalSeconds(textBox8.Text);
            }
            catch
            {
                // ignored
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            label55.Text = (numericUpDown2.Value == 0) ? "px (default)" : "px";
        }

        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {
            label56.Text = (numericUpDown8.Value == 0) ? "px (default)" : "px";
        }

        #endregion

        #region REMOVE AUDIO

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(textBox6.Text);

            textBox7.Text = fi.FullName.Replace(fi.Extension, "") + "_noaudio" + fi.Extension;
            button9.Enabled = fi.Exists;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            GetOpenPath(FilterVideo, textBox6);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            GetSavePath(FilterVideo, textBox7);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            JobInfo jobInfo = Utility.RemoveAudio(textBox6.Text, textBox7.Text, overwriteFiles);

            RunAsync(jobInfo);
        }

        #endregion

        #region CREATE VIDEO

        private void textBox14_TextChanged(object sender, EventArgs e)
        {
            textBox15.Text = textBox14.Text + "\\MyVideo.avi";

            LoadImages();
        }

        private void textBox15_TextChanged(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(textBox15.Text);
            string str = fi.Extension.Remove(0, 1).ToUpper();

            if (comboBox4.Text != str)
                comboBox4.Text = str;
        }

        private void comboBox6_TextChanged(object sender, EventArgs e)
        {
            string text = comboBox6.Text;

            if (!comboBox6.Items.Contains(text))
            {
                string file = text.Split('.')[0];

                if (!text.Contains("%d"))
                    file += "%d";

                comboBox6.Items[0] = file + ".bmp";
                comboBox6.Items[1] = file + ".jpg";
                comboBox6.Items[2] = file + ".png";
            }

            LoadImages();
        }

        private void comboBox4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                FileInfo fi = new FileInfo(textBox15.Text);
                textBox15.Text = fi.FullName.Replace(fi.Extension, "") + "." + comboBox4.Text.ToLower();
            }
            catch
            {
                // ignored
            }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog.SelectedPath = textBox14.Text;

            if (FolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                textBox14.Text = FolderBrowserDialog.SelectedPath;
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            GetSavePath(FilterVideoAndGif, textBox15);
        }

        private void button25_Click(object sender, EventArgs e)
        {
            JobInfo jobInfo = Utility.CreateVideo(textBox14.Text + "\\" + comboBox6.Text, textBox15.Text, comboBox10.Text, overwriteFiles);

            RunAsync(jobInfo);
        }

        private void LoadImages()
        {
            if (!Directory.Exists(textBox14.Text))
            {
                label42.Text = "Images in folder: 0";
                button25.Enabled = false;
            }
            else
            {
                string pattern = comboBox6.Text.Replace("%d", "*");
                FileInfo[] fis = new DirectoryInfo(textBox14.Text).GetFiles(pattern);
                int count = fis.Length;

                label42.Text = "Images in folder: " + count;
                button25.Enabled = (count > 0);
            }
        }

        #endregion

        #region RESIZE VIDEO

        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(textBox10.Text);

            textBox11.Text = fi.FullName.Replace(fi.Extension, "") + "_resized" + fi.Extension;
            button16.Enabled = fi.Exists;

            VideoInfo videoInfo = VideoInfo.LoadVideoInfo(ffmpeg, textBox10.Text);

            if (fi.Exists)
            {
                comboBox7.Text = fi.Extension.Remove(0, 1).ToUpper();
            }

            if (videoInfo.Resolution.Width * videoInfo.Resolution.Height > 0)
            {
                numericUpDown6.Value = videoInfo.Resolution.Width;
                numericUpDown7.Value = videoInfo.Resolution.Height;
            }

            if (videoInfo.VideoBitRate > 0)
            {
                comboBox11.Items[0] = "Default (" + videoInfo.VideoBitRate + ")";
            }

            if (videoInfo.AudioBitRate > 0)
            {
                comboBox12.Items[0] = "Default (" + videoInfo.AudioBitRate + ")";
            }
        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(textBox11.Text);
            comboBox7.Text = fi.Extension.Remove(0, 1).ToUpper();
        }

        private void comboBox7_TextChanged(object sender, EventArgs e)
        {
            try
            {
                FileInfo fi = new FileInfo(textBox11.Text);
                string str = fi.FullName.Replace(fi.Extension, "") + "." + comboBox7.Text.ToLower();

                if (textBox11.Text != str)
                    textBox11.Text = str;
            }
            catch
            {
                // ignored
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            GetOpenPath(FilterVideo, textBox10);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            GetSavePath(FilterVideo, textBox11);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            JobInfo jobInfo = Utility.ResizeVideo(textBox10.Text, textBox11.Text,
                (int)numericUpDown6.Value, (int)numericUpDown7.Value, comboBox11.Text, comboBox12.Text, overwriteFiles);

            RunAsync(jobInfo);
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            label33.Text = (numericUpDown6.Value == 0) ? "px (default)" : "px";
        }

        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            label34.Text = (numericUpDown7.Value == 0) ? "px (default)" : "px";
        }

        #endregion

        #region CROP VIDEO

        private void textBox12_TextChanged(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(OpenFileDialog.FileName);

            textBox13.Text = fi.FullName.Replace(fi.Extension, "") + "_new" + fi.Extension;
            button20.Enabled = fi.Exists;
        }

        private void button19_Click(object sender, EventArgs e)
        {
            GetOpenPath(FilterVideo, textBox12);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            GetSavePath(FilterVideo, textBox13);
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown1.Enabled = numericUpDown3.Enabled = !checkBox4.Checked;
        }

        private void button20_Click(object sender, EventArgs e)
        {
            JobInfo jobInfo = Utility.CropVideo(textBox12.Text, textBox13.Text,
                (int)numericUpDown1.Value, (int)numericUpDown3.Value, checkBox4.Checked, (int)numericUpDown5.Value, (int)numericUpDown4.Value, overwriteFiles);

            RunAsync(jobInfo);
        }

        #endregion

        #region QUEUE

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            queueStopButton.Enabled = listView1.SelectedItems.Count > 0;
        }

        private ListViewItem AddToQueue(JobInfo jobInfo)
        {
            ListViewItem item = new ListViewItem
            {
                Group = listView1.Groups[(int) jobInfo.Task],
                Tag = jobInfo.Process,
                Text = jobInfo.Input
            };
            item.SubItems.Add(jobInfo.Output);

            if (listView1.InvokeRequired)
            {
                listView1.Invoke((MethodInvoker) delegate
                {
                    listView1.Items.Add(item);
                });
            }
            else
            {
                listView1.Items.Add(item);
            }

            processes.Add(jobInfo.Process);

            return item;
        }

        private void RemoveFromQueue(ListViewItem item)
        {
            processes.Remove((Process)item.Tag);

            if (listView1.InvokeRequired)
            {
                listView1.Invoke((MethodInvoker)delegate
                {
                    listView1.Items.Remove(item);
                });
            }
            else
            {
                listView1.Items.Remove(item);
            }
        }

        private void queueStopButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                try
                {
                    Process process = (Process) item.Tag;
                    process.Kill();
                }
                catch
                {
                    // ignored
                }
                listView1.Items.Remove(item);
            }
        }

        #endregion

        #region SETTINGS

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            openExplorer = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            checkUpdates = checkBox2.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            logger.Enabled = checkBox3.Checked;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            outputEnabled = checkBox5.Checked;

            if (outputEnabled)
            {
                tabControl1.TabPages.Insert(7, outputTab);
            }
            else
            {
                tabControl1.TabPages.Remove(outputTab);
            }
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            overwriteFiles = checkBox7.Checked;
        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            ffmpeg = textBox9.Text;
        }
        
        #endregion

        #region ABOUT

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Properties.Resources.HomePage);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://" + linkLabel2.Text);
        }

        #endregion

        #region TABCONTROL

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (outputEnabled && tabControl1.SelectedTab == outputTab)
            {
                MaximumSize = new Size(0, 0);
            }
            else
            {
                MaximumSize = MinimumSize;
            }

            linkLabel3.Parent = tabControl1.SelectedTab;
        }

        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Brush _textBrush;
            Font _tabFont = new Font("Arial", (float)11.25, FontStyle.Bold, GraphicsUnit.Pixel);

            // Get the item from the collection.
            TabPage _tabPage = tabControl1.TabPages[e.Index];

            // Get the real bounds for the tab rectangle.
            Rectangle _tabBounds = tabControl1.GetTabRect(e.Index);

            if (e.State == DrawItemState.Selected)
            {
                // Draw a different background color, and don't paint a focus rectangle.
                _textBrush = new SolidBrush(Color.White);
                g.FillRectangle(Brushes.RoyalBlue, e.Bounds);
                //_tabFont = new Font("Arial", (float)11.5, FontStyle.Bold, GraphicsUnit.Pixel);
            }
            else
            {
                _textBrush = new SolidBrush(e.ForeColor);
                e.DrawBackground();
            }

            // Draw string. Center the text.
            StringFormat _stringFlags = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(_tabPage.Text, _tabFont, _textBrush, _tabBounds, new StringFormat(_stringFlags));
        }

        private void tabPage1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length == 0)
                return;

            FileInfo fi = new FileInfo(files[0]);

            switch (tabControl1.TabPages.IndexOf((TabPage)sender))
            {
                case 0:
                    if (fi.Exists)
                        textBox1.Text = files[0];
                    break;
                case 1:
                    if (fi.Exists)
                        textBox6.Text = files[0];
                    break;
                case 2:
                    if (fi.Exists)
                        textBox3.Text = files[0];
                    break;
                case 3:
                    if (fi.Exists)
                        textBox10.Text = files[0];
                    break;
                case 4:
                    if (fi.Exists)
                        textBox12.Text = files[0];
                    break;
                case 5:
                    if (fi.Exists)
                        textBox14.Text = files[0];
                    break;
            }
        }

        private void tabPage1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effect = DragDropEffects.All;
        }

        #endregion
    }
}