using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Forms;

namespace VideoExtractor
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        enum Task
        {
            ExtractAudio,
            RemoveAudio,
            ExtractImages,
            ResizeVideo,
            CropVideo,
            MakeVideo
        }

        const string HomePage = "https://github.com/spixy/VideoExtractor";
        const string ConfigFile = "settings.ini";
        const string LogFile = "log.txt";
        const string UpdateFile = "https://raw.githubusercontent.com/spixy/VideoExtractor/master/lastversion";

        bool OpenExplorer = true;
        bool CheckUpdates = true;
        bool OutputEnabled = false;
        string ffmpeg = "ffmpeg.exe";

        Logger logger = new Logger(LogFile, false);
        TabPage outputTab;

        List<Process> processes = new List<Process>();
        Process extractAudio, removeAudio, extractImages, resizeVideo, cropVideo, makeVideo;

        // GENERAL

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 10;
            comboBox5.SelectedIndex = 0;
            comboBox6.SelectedIndex = 1;
            comboBox9.SelectedIndex = 1;
            comboBox10.SelectedIndex = 10;

            outputTab = tabPage3;
            tabControl1.TabPages.Remove(outputTab);

            MinimumSize = Size;
            MaximumSize = Size;

            label15.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            int Bits = IntPtr.Size * 8;
            label19.Text = Bits + "-bit";

            logger.Clear();

            LoadSettings();

            if (CheckUpdates)
            {
                Updater updater = new Updater(UpdateFile);
                updater.UpdateAvailableAction = () =>
                {
                    linkLabel3.Visible = true;
                };
                updater.IsUpdateAvailableAsync();
            }
        }

        private void LoadSettings()
        {
            try
            {
                string[] lines = File.ReadAllLines(ConfigFile);

                foreach (string line in lines)
                {
                    if (line.ToLower().Contains("no windows")) checkBox1.Checked = false;
                    if (line.ToLower().Contains("no update")) checkBox2.Checked = false;
                    if (line.ToLower().Contains("file log")) checkBox3.Checked = true;
                    if (line.ToLower().Contains("output")) checkBox5.Checked = true;
                    if (line.ToLower().Contains("ffmpeg ")) textBox9.Text = line.ToLower().Replace("ffmpeg ", "");
                    if (line.ToLower().Contains("last tab "))
                    {
                        int index;
                        if (Int32.TryParse(line.ToLower().Replace("last tab ", ""), out index) && index >= 0 && index <= 5)
                            tabControl1.SelectedIndex = index;
                    }
                }
            }
            catch {}
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            using (StreamWriter file = new StreamWriter(ConfigFile, false))
            {
                if (!checkBox1.Checked) file.WriteLine("no windows");
                if (!checkBox2.Checked) file.WriteLine("no update");
                if (checkBox3.Checked) file.WriteLine("file log");
                if (checkBox5.Checked) file.WriteLine("output");
                file.WriteLine("ffmpeg " + textBox9.Text);

                if (checkBox6.Checked)
                {
                    file.WriteLine("last tab " + tabControl1.SelectedIndex);
                }
            }

            foreach (Process p in processes)
            {
                try
                {
                    p.Kill();
                }
                catch { }
            }
        }

        private void WriteToOutput(string text)
        {
            if (OutputEnabled)
            {
                if (outputLog.InvokeRequired)
                {
                    outputLog.Invoke((MethodInvoker)delegate
                    {
                        outputLog.Text += text + "\r\n";
                    });
                }
                else
                {
                    outputLog.Text += text + "\r\n";
                }
            }
        }

        private void bw_DoWork(string arguments, Task task)
        {
            if (!File.Exists(ffmpeg))
            {
                string error = ffmpeg + " not found";

                WriteToOutput(error);
                logger.Log(error);

                return;
            }

            Process p = new Process();
            p.StartInfo.FileName = ffmpeg;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            p.OutputDataReceived += (s, ea) =>
            {
                WriteToOutput(ea.Data);
                logger.Log(ea.Data);
            };

            p.ErrorDataReceived += (s, ea) =>
            {
                WriteToOutput(ea.Data);
                logger.Log(ea.Data);
            };

            p.Start();
            processes.Add(p);

            switch (task)
            {
                case Task.ExtractAudio: extractAudio = p; break;
                case Task.RemoveAudio: removeAudio = p; break;
                case Task.ExtractImages: extractImages = p; break;
                case Task.ResizeVideo: resizeVideo = p; break;
                case Task.CropVideo: cropVideo = p; break;
                case Task.MakeVideo: makeVideo = p; break;
            }

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();
            processes.Remove(p);
            p.Dispose();
        }

        private void LaunchExplorer(string file)
        {
            if (File.Exists(file))
            {
                if (OpenExplorer)
                    Process.Start("explorer.exe", @"/select, " + file);
            }
            else if (Directory.Exists(file))
            {
                if (OpenExplorer)
                    Process.Start("explorer.exe", file);
            }
            else
            {
                SystemSounds.Hand.Play();

                if (OutputEnabled)
                {
                    tabControl1.SelectedIndex = 6;
                    outputLog.SelectionLength = 0;
                }
            }
        }

        // VALIDATION

        private void comboBox_Validating(object sender, CancelEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            int val;

            bool valid = comboBox.SelectedIndex == 0 || (Int32.TryParse(comboBox.Text, out val) && val > 0);

            comboBox.BackColor = (valid) ? SystemColors.Window : Color.Red;
        }

        private void comboBox_double_Validating(object sender, CancelEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            double val;

            bool valid = (Double.TryParse(comboBox.Text, out val) && val > 0);

            comboBox.BackColor = (valid) ? SystemColors.Window : Color.Red;
        }

        private void textBox_Validating(object sender, CancelEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string Text = textBox.Text;
            int val;

            bool valid = Text.Length == 12 &&
                Text[2] == ':' && Text[5] == ':' && Text[8] == '.' &&
                Int32.TryParse(Text.Substring(0, 2), out val) &&
                Int32.TryParse(Text.Substring(3, 2), out val) &&
                Int32.TryParse(Text.Substring(6, 2), out val) &&
                Int32.TryParse(Text.Substring(9, 3), out val);

            textBox.BackColor = (valid) ? SystemColors.Window : Color.Red;
        }

        // EXTRACT AUDIO

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button3.Enabled = File.Exists(textBox1.Text);
            VideoInfo videoInfo = Utility.LoadVideoInfo(ffmpeg, textBox1.Text);

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
                int val;
                if (Int32.TryParse(comboBox2.Text, out val) && val > 320)
                    comboBox2.Text = "320";
            }
        }

        private void comboBox8_TextChanged(object sender, EventArgs e)
        {
            try
            {
                FileInfo fi = new FileInfo(textBox1.Text);
                textBox2.Text = fi.FullName.Replace(fi.Extension, "") + "." + comboBox8.Text.ToLower();
            }
            catch { }

            if (comboBox8.Text.ToUpper() == "MP3" && comboBox2.Text != "Default")
            {
                int val;
                if (Int32.TryParse(comboBox2.Text, out val) && val > 320)
                    comboBox2.Text = "320";
            }
        }

        private void textBox16_TextChanged(object sender, EventArgs e)
        {
            try
            {
                TimeSpan ts = new TimeSpan(
                    Convert.ToInt32(textBox16.Text.Remove(2, 10)),
                    Convert.ToInt32(textBox16.Text.Remove(5, 7).Remove(0, 3)),
                    Convert.ToInt32(textBox16.Text.Remove(8, 4).Remove(0, 6))
                );
                trackBar3.Value = (int)ts.TotalSeconds;
            }
            catch { }
        }

        private void textBox17_TextChanged(object sender, EventArgs e)
        {
            try
            {
                TimeSpan ts = new TimeSpan(
                    Convert.ToInt32(textBox17.Text.Remove(2, 10)),
                    Convert.ToInt32(textBox17.Text.Remove(5, 7).Remove(0, 3)),
                    Convert.ToInt32(textBox17.Text.Remove(8, 4).Remove(0, 6))
                );
                trackBar4.Value = (int)ts.TotalSeconds;
            }
            catch { }
        }

        private void trackBar3_ValueChanged(object sender, EventArgs e)
        {
            string ms = (textBox16.Text.Length == 12) ? textBox16.Text.Remove(0, 9) : "000";

            TimeSpan ts = new TimeSpan(0, 0, trackBar3.Value);
            textBox16.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ms);
        }

        private void trackBar4_ValueChanged(object sender, EventArgs e)
        {
            string ms = (textBox17.Text.Length == 12) ? textBox17.Text.Remove(0, 9) : "000";

            TimeSpan ts = new TimeSpan(0, 0, trackBar4.Value);
            textBox17.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ms);

            label50.Visible = (trackBar4.Value == 0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;

                FileInfo fi = new FileInfo(openFileDialog1.FileName);
                textBox2.Text = fi.FullName.Replace(fi.Extension, ".mp3");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "MP2|*.mp2|MP3|*.mp3|MP4|*.mp4|M4A|*.m4a|WAV|*.wav|OGG|*.ogg|WMA|*.wma|All files|*.*";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = saveFileDialog1.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button10.Enabled = true;

            string argument = Utility.ExtractVideo_Arguments(textBox1.Text, textBox2.Text,
                comboBox1.Text, comboBox2.Text, comboBox5.Text, textBox16.Text, textBox17.Text);

            logger.Log("ffmpeg.exe " + argument);

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (obj, ea) => bw_DoWork(argument, Task.ExtractAudio);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.RunWorkerAsync(argument);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (!extractAudio.HasExited)
                extractAudio.Kill();
        }

        private void bw_RunWorkerCompleted(object sender, AsyncCompletedEventArgs e)
        {
            button3.Enabled = true;
            button10.Enabled = false;

            LaunchExplorer(textBox2.Text);
        }       

        // EXTRACT IMAGES

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            button4.Enabled = File.Exists(textBox3.Text);
            VideoInfo videoInfo = Utility.LoadVideoInfo(ffmpeg, textBox3.Text);

            if (videoInfo.Duration > 0)
            {
                trackBar1.Maximum = videoInfo.Duration;
                trackBar2.Maximum = videoInfo.Duration;
            }

            if (videoInfo.FPS > 0)
            {
                comboBox3.Text = videoInfo.FPS.ToString();
            }

            if (videoInfo.Size != null && videoInfo.Size.Width * videoInfo.Size.Height > 0)
            {
                numericUpDown2.Value = videoInfo.Size.Width;
                numericUpDown8.Value = videoInfo.Size.Height;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox3.Text = openFileDialog1.FileName;

                FileInfo fi = new FileInfo(openFileDialog1.FileName);
                textBox4.Text = fi.FullName.Replace(fi.Extension, "") + "_Images";
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.SelectedPath = textBox3.Text;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox4.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            button4.Enabled = false;
            button12.Enabled = true;

            string output = (textBox4.Text + "\\image_%d." + comboBox9.Text.ToLower()).Replace(@"\\", @"\");

            string argument = Utility.ExtractImages_Arguments(textBox3.Text, output,
                (int)numericUpDown2.Value, (int)numericUpDown8.Value, comboBox3.Text.Replace(',','.'), textBox5.Text, textBox8.Text);

            if (!Directory.Exists(textBox4.Text))
            {
                Directory.CreateDirectory(textBox4.Text);
                logger.Log(textBox4.Text + " directory created");
            }

            logger.Log("ffmpeg.exe " + argument);

            BackgroundWorker bw2 = new BackgroundWorker();
            bw2.DoWork += (obj, ea) => bw_DoWork(argument, Task.ExtractImages);
            bw2.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw2_RunWorkerCompleted);
            bw2.RunWorkerAsync(argument);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (!extractImages.HasExited)
                extractImages.Kill();
        }

        private void bw2_RunWorkerCompleted(object sender, AsyncCompletedEventArgs e)
        {
            button4.Enabled = true;
            button12.Enabled = false;

            LaunchExplorer(textBox4.Text);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            string ms = (textBox5.Text.Length == 12) ? textBox5.Text.Remove(0, 9) : "000";

            TimeSpan ts = new TimeSpan(0, 0, trackBar1.Value);
            textBox5.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ms);
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            string ms = (textBox8.Text.Length == 12) ? textBox8.Text.Remove(0, 9) : "000";

            TimeSpan ts = new TimeSpan(0, 0, trackBar2.Value);
            textBox8.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ms);

            label11.Visible = (trackBar2.Value == 0);
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            try
            {
                TimeSpan ts = new TimeSpan(
                    Convert.ToInt32(textBox5.Text.Remove(2, 10)),
                    Convert.ToInt32(textBox5.Text.Remove(5, 7).Remove(0, 3)),
                    Convert.ToInt32(textBox5.Text.Remove(8, 4).Remove(0, 6))
                );
                trackBar1.Value = (int)ts.TotalSeconds;
            }
            catch { }
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            try
            {
                TimeSpan ts = new TimeSpan(
                    Convert.ToInt32(textBox8.Text.Remove(2, 10)),
                    Convert.ToInt32(textBox8.Text.Remove(5, 7).Remove(0, 3)),
                    Convert.ToInt32(textBox8.Text.Remove(8, 4).Remove(0, 6))
                );
                trackBar2.Value = (int)ts.TotalSeconds;
            }
            catch { }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            label55.Text = (numericUpDown2.Value == 0) ? "px (default)" : "px";
        }

        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {
            label56.Text = (numericUpDown8.Value == 0) ? "px (default)" : "px";
        }

        // REMOVE AUDIO

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            button9.Enabled = File.Exists(textBox6.Text);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox6.Text = openFileDialog1.FileName;

                FileInfo fi = new FileInfo(openFileDialog1.FileName);
                textBox7.Text = fi.FullName.Replace(fi.Extension, "") + "_noaudio" + fi.Extension;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "AVI|*.avi|FLV|*.flv|MOV|*.mov|MKV|*.mkv|MP4|*.mp4|OGG|*.ogg|WEBM|*.webm|WMV|*.wmv|All files|*.*";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox7.Text = saveFileDialog1.FileName;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            button9.Enabled = false;
            button11.Enabled = true;

            string argument = Utility.RemoveAudio_Arguments(textBox6.Text, textBox7.Text);

            logger.Log("ffmpeg.exe " + argument);

            BackgroundWorker bw3 = new BackgroundWorker();
            bw3.DoWork += (obj, ea) => bw_DoWork(argument, Task.RemoveAudio);
            bw3.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw3_RunWorkerCompleted);
            bw3.RunWorkerAsync(argument);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (!removeAudio.HasExited)
                removeAudio.Kill();
        }

        private void bw3_RunWorkerCompleted(object sender, AsyncCompletedEventArgs e)
        {
            button9.Enabled = true;
            button12.Enabled = false;

            LaunchExplorer(textBox7.Text);
        }

        // CREATE VIDEO

        private void textBox14_TextChanged(object sender, EventArgs e)
        {
            LoadImages();
        }

        private void textBox15_TextChanged(object sender, EventArgs e)
        {
            textBox15.Text = textBox15.Text.Replace(@"\\", @"\");
        }

        private void comboBox6_TextChanged(object sender, EventArgs e)
        {
            string text = comboBox6.Text;

            if (comboBox6.Items.Contains(text))
                return;

            string file = text.Split('.')[0];

            if (!text.Contains("%d"))
                file += "%d";

            comboBox6.Items[0] = file + ".bmp";
            comboBox6.Items[1] = file + ".jpg";
            comboBox6.Items[2] = file + ".png";

            LoadImages();
        }

        private void comboBox4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                FileInfo fi = new FileInfo(textBox15.Text);
                textBox15.Text = fi.FullName.Replace(fi.Extension, "") + "." + comboBox4.Text.ToLower();
            }
            catch { }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.SelectedPath = textBox14.Text;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox14.Text = folderBrowserDialog1.SelectedPath;
                textBox15.Text = folderBrowserDialog1.SelectedPath + "\\MyVideo.avi";
                comboBox4.Text = "AVI";
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "MP2|*.mp2|MP3|*.mp3|MP4|*.mp4|M4A|*.m4a|WAV|*.wav|OGG|*.ogg|WMA|*.wma|All files|*.*";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox15.Text = saveFileDialog1.FileName;
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            button25.Enabled = false;
            button24.Enabled = true;

            string input = textBox14.Text + "\\" + comboBox6.Text;

            string argument = Utility.CreateVideo_Arguments(input, textBox15.Text, comboBox10.Text);

            logger.Log("ffmpeg.exe " + argument);

            BackgroundWorker bw6 = new BackgroundWorker();
            bw6.DoWork += (obj, ea) => bw_DoWork(argument, Task.MakeVideo);
            bw6.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw6_RunWorkerCompleted);
            bw6.RunWorkerAsync(argument);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            if (!makeVideo.HasExited)
                makeVideo.Kill();
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

        private void bw6_RunWorkerCompleted(object sender, AsyncCompletedEventArgs e)
        {
            button25.Enabled = true;
            button24.Enabled = false;

            LaunchExplorer(textBox15.Text);
        }

        // RESIZE VIDEO

        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            button16.Enabled = File.Exists(textBox10.Text);

            VideoInfo videoInfo = Utility.LoadVideoInfo(ffmpeg, textBox10.Text);

            FileInfo fi = new FileInfo(textBox10.Text);

            if (fi.Exists)
            {
                comboBox7.Text = fi.Extension.Remove(0, 1).ToUpper();
            }

            if (videoInfo.Size != null)
            {
                numericUpDown6.Value = videoInfo.Size.Width;
                numericUpDown7.Value = videoInfo.Size.Height;
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

        private void comboBox7_TextChanged(object sender, EventArgs e)
        {
            try
            {
                FileInfo fi = new FileInfo(textBox11.Text);
                textBox11.Text = fi.FullName.Replace(fi.Extension, "") + "." + comboBox7.Text.ToLower();
            }
            catch { }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox10.Text = textBox11.Text = openFileDialog1.FileName;

                FileInfo fi = new FileInfo(openFileDialog1.FileName);
                textBox11.Text = fi.FullName.Replace(fi.Extension, "") + "_resized" + fi.Extension;
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "AVI|*.avi|FLV|*.flv|MOV|*.mov|MKV|*.mkv|MP4|*.mp4|OGG|*.ogg|WEBM|*.webm|WMV|*.wmv|All files|*.*";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox11.Text = saveFileDialog1.FileName;
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            button16.Enabled = false;
            button17.Enabled = true;

            string argument = Utility.ResizeVideo_Arguments(textBox10.Text, textBox11.Text,
                (int)numericUpDown6.Value, (int)numericUpDown7.Value, comboBox11.Text, comboBox12.Text);

            logger.Log("ffmpeg.exe " + argument);

            BackgroundWorker bw4 = new BackgroundWorker();
            bw4.DoWork += (obj, ea) => bw_DoWork(argument, Task.ResizeVideo);
            bw4.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw4_RunWorkerCompleted);
            bw4.RunWorkerAsync(argument);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (!resizeVideo.HasExited)
                resizeVideo.Kill();
        }

        private void bw4_RunWorkerCompleted(object sender, AsyncCompletedEventArgs e)
        {
            button16.Enabled = true;
            button17.Enabled = false;

            LaunchExplorer(textBox11.Text);
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            label33.Text = (numericUpDown6.Value == 0) ? "px (default)" : "px";
        }

        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            label34.Text = (numericUpDown7.Value == 0) ? "px (default)" : "px";
        }

        // CROP VIDEO

        private void textBox12_TextChanged(object sender, EventArgs e)
        {
            button20.Enabled = File.Exists(textBox12.Text);
        }

        private void button19_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox12.Text = textBox12.Text = openFileDialog1.FileName;

                FileInfo fi = new FileInfo(openFileDialog1.FileName);
                textBox13.Text = fi.FullName.Replace(fi.Extension, "") + "_new" + fi.Extension;
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "AVI|*.avi|FLV|*.flv|MOV|*.mov|MKV|*.mkv|MP4|*.mp4|OGG|*.ogg|WEBM|*.webm|WMV|*.wmv|All files|*.*";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox13.Text = saveFileDialog1.FileName;
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown1.Enabled = numericUpDown3.Enabled = !checkBox4.Checked;
        }

        private void button20_Click(object sender, EventArgs e)
        {
            button20.Enabled = false;
            button21.Enabled = true;

            string argument = Utility.CropVideo_Arguments(textBox12.Text, textBox13.Text,
                (int)numericUpDown1.Value, (int)numericUpDown3.Value, checkBox4.Checked, (int)numericUpDown5.Value, (int)numericUpDown4.Value);

            logger.Log("ffmpeg.exe " + argument);

            BackgroundWorker bw5 = new BackgroundWorker();
            bw5.DoWork += (obj, ea) => bw_DoWork(argument, Task.CropVideo);
            bw5.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw5_RunWorkerCompleted);
            bw5.RunWorkerAsync(argument);
        }

        private void button21_Click(object sender, EventArgs e)
        {
            if (!cropVideo.HasExited)
                cropVideo.Kill();
        }

        private void bw5_RunWorkerCompleted(object sender, AsyncCompletedEventArgs e)
        {
            button20.Enabled = true;
            button21.Enabled = false;

            LaunchExplorer(textBox13.Text);
        }

        // SETTINGS

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            OpenExplorer = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            CheckUpdates = checkBox2.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            logger.Enabled = checkBox3.Checked;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            OutputEnabled = checkBox5.Checked;

            if (OutputEnabled)
            {
                tabControl1.TabPages.Insert(6, outputTab);
            }
            else
            {
                tabControl1.TabPages.Remove(outputTab);
            }
        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            ffmpeg = textBox9.Text;
        }

        // ABOUT

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(HomePage);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://" + linkLabel2.Text);
        }

        // TABCONTROL

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (OutputEnabled && tabControl1.SelectedIndex == 6)
                MaximumSize = new Size(0, 0);
            else
                MaximumSize = MinimumSize;

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
                _textBrush = new System.Drawing.SolidBrush(e.ForeColor);
                e.DrawBackground();
            }

            // Draw string. Center the text.
            StringFormat _stringFlags = new StringFormat();
            _stringFlags.Alignment = StringAlignment.Center;
            _stringFlags.LineAlignment = StringAlignment.Center;
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
                    textBox1.Text = files[0];
                    textBox2.Text = files[0].Replace(fi.Extension, "") + ".mp3";
                    break;
                case 1:
                    textBox6.Text = files[0];
                    textBox7.Text = fi.FullName.Replace(fi.Extension, "") + "_noaudio" + fi.Extension;
                    break;
                case 2:
                    textBox3.Text = files[0];
                    textBox4.Text = fi.FullName.Replace(fi.Extension, "") + "_Images";
                    break;
                case 3:
                    textBox10.Text = files[0];
                    textBox11.Text = fi.FullName.Replace(fi.Extension, "") + "_resized" + fi.Extension;
                    break;
                case 4:
                    textBox12.Text = files[0];
                    textBox13.Text = fi.FullName.Replace(fi.Extension, "") + "_new" + fi.Extension;
                    break;
                case 5:
                    textBox14.Text = files[0];
                    textBox15.Text = files[0] + "\\MyVideo.avi";
                    break;
            }
        }

        private void tabPage1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effect = DragDropEffects.All;
        }

    }
}