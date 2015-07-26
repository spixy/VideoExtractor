﻿using System;
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

        private bool OpenExplorer = true;
        private bool CheckUpdates = true;
        private bool OutputEnabled = false;
        private string ffmpeg = "ffmpeg.exe";

        private readonly Logger logger = new Logger(Properties.Resources.LogFile, false);
        private readonly List<Process> processes = new List<Process>();

        // GENERAL

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 10;
            comboBox5.SelectedIndex = 0;
            comboBox6.SelectedIndex = 1;
            comboBox9.SelectedIndex = 1;
            comboBox10.SelectedIndex = 10;

            tabControl1.TabPages.Remove(outputTab);

            MinimumSize = Size;
            MaximumSize = Size;

            label15.Text = Application.ProductVersion;

            int Bits = IntPtr.Size * 8;
            label19.Text = Bits + "-bit";

            logger.Clear();

            LoadSettings();

            if (CheckUpdates)
            {
                Updater updater = new Updater(Properties.Resources.UpdateFile)
                {
                    UpdateAvailableAction = () => { linkLabel3.Visible = true; }
                };
                updater.IsUpdateAvailableAsync();
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
                    if (line.ToLower().Contains("ffmpeg ")) textBox9.Text = line.ToLower().Replace("ffmpeg ", "");
                    if (line.ToLower().Contains("last tab "))
                    {
                        int index;
                        if (int.TryParse(line.ToLower().Replace("last tab ", ""), out index) && index >= 0 && index <= 5)
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
                file.WriteLine("ffmpeg " + textBox9.Text);

                if (checkBox6.Checked)
                {
                    file.WriteLine("last tab " + tabControl1.SelectedIndex);
                }
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

        private void WriteToOutput(string text)
        {
            if (!OutputEnabled)
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

        private void bw_DoWork(object sender, DoWorkEventArgs e, JobInfo jobInfo)
        {
            if (!File.Exists(ffmpeg))
            {
                string error = ffmpeg + " not found";

                WriteToOutput(error);
                logger.Log(error);

                return;
            }

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = ffmpeg,
                    Arguments = jobInfo.Arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

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

            var item = AddToQueue(jobInfo, p);
            processes.Add(p);

            p.Start();

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();

            listView1.Invoke((MethodInvoker) delegate
            {
                listView1.Items.Remove(item);
            });
            processes.Remove(p);

            switch (p.ExitCode)
            {
                // Kill()
                case -1:
                    jobInfo.Result = Result.Cancel;
                    break;

                // OK
                case 0:
                    jobInfo.Result = Result.Success;
                    break;

                // ffmpeg error
                default:
                    jobInfo.Result = Result.Error;
                    break;
            }

            e.Result = jobInfo;
            p.Dispose();
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            JobInfo jobInfo = (JobInfo) e.Result;

            switch (jobInfo.Result)
            {
                case Result.Success:
                    LaunchExplorer(jobInfo.Output);
                    break;

                case Result.Error:
                    SystemSounds.Hand.Play();
                    goto case Result.Cancel;

                case Result.Cancel:
                    if (Directory.Exists(jobInfo.Output))
                        Directory.Delete(jobInfo.Output, true);
                    if (File.Exists(jobInfo.Output))
                        File.Delete(jobInfo.Output);
                    break;
            }
        }    

        private void LaunchExplorer(string file)
        {
            // File
            if (File.Exists(file))
            {
                if (OpenExplorer)
                    Process.Start("explorer.exe", @"/select, " + file);
            }
            // Directory
            else if (Directory.Exists(file))
            {
                if (OpenExplorer)
                    Process.Start("explorer.exe", file);
            }
            // Not found
            else
            {
                SystemSounds.Hand.Play();
            }
        }

        // VALIDATION

        private void comboBox_Validating(object sender, CancelEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            int val;

            bool valid = comboBox.SelectedIndex == 0 || (int.TryParse(comboBox.Text, out val) && val > 0);

            comboBox.BackColor = (valid) ? SystemColors.Window : Color.Red;
        }

        private void comboBox_double_Validating(object sender, CancelEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            double val;

            bool valid = (double.TryParse(comboBox.Text, out val) && val > 0);

            comboBox.BackColor = (valid) ? SystemColors.Window : Color.Red;
        }

        private void textBox_Validating(object sender, CancelEventArgs e)
        {
            TextBox textBox = sender as TextBox;
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
                if (int.TryParse(comboBox2.Text, out val) && val > 320)
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
            catch
            {
                // ignored
            }

            if (comboBox8.Text.ToUpper() == "MP3" && comboBox2.Text != "Default")
            {
                int val;
                if (int.TryParse(comboBox2.Text, out val) && val > 320)
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
            string ms = (textBox16.Text.Length == 12) ? textBox16.Text.Remove(0, 9) : "000";

            textBox16.Text = Utility.GetTimeSpanText(trackBar3.Value) + "." + ms;
        }

        private void trackBar4_ValueChanged(object sender, EventArgs e)
        {
            string ms = (textBox17.Text.Length == 12) ? textBox17.Text.Remove(0, 9) : "000";

            textBox17.Text = Utility.GetTimeSpanText(trackBar4.Value) + "." + ms;

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
            JobInfo jobInfo = Utility.ExtractAudio(textBox1.Text, textBox2.Text,
                comboBox1.Text, comboBox2.Text, comboBox5.Text, textBox16.Text, textBox17.Text);

            logger.Log("ffmpeg.exe " + jobInfo.Arguments);

            tabControl1.SelectTab(queueTab);

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (obj, ea) => bw_DoWork(obj, ea, jobInfo);
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.RunWorkerAsync();
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
                comboBox3.Text = videoInfo.FPS.ToString(CultureInfo.InvariantCulture);
            }

            if (videoInfo.Size.Width * videoInfo.Size.Height > 0)
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
            string output = (textBox4.Text + "\\image_%d." + comboBox9.Text.ToLower()).Replace(@"\\", @"\");

            JobInfo jobInfo = Utility.ExtractImages(textBox3.Text, output,
                (int)numericUpDown2.Value, (int)numericUpDown8.Value, comboBox3.Text.Replace(',','.'), textBox5.Text, textBox8.Text);

            if (!Directory.Exists(textBox4.Text))
            {
                Directory.CreateDirectory(textBox4.Text);
                logger.Log(textBox4.Text + " directory created");
            }

            logger.Log("ffmpeg.exe " + jobInfo.Arguments);

            tabControl1.SelectTab(queueTab);

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (obj, ea) => bw_DoWork(obj, ea, jobInfo);
            bw.RunWorkerCompleted += bw2_RunWorkerCompleted;
            bw.RunWorkerAsync();
        }

        private void bw2_RunWorkerCompleted(object sender, AsyncCompletedEventArgs e)
        {
            button4.Enabled = true;

            LaunchExplorer(textBox4.Text);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            string ms = (textBox5.Text.Length == 12) ? textBox5.Text.Remove(0, 9) : "000";

            textBox5.Text = Utility.GetTimeSpanText(trackBar1.Value) + "." + ms;
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            string ms = (textBox8.Text.Length == 12) ? textBox8.Text.Remove(0, 9) : "000";

            textBox8.Text = Utility.GetTimeSpanText(trackBar2.Value) + "." + ms;

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
            JobInfo jobInfo = Utility.RemoveAudio(textBox6.Text, textBox7.Text);

            logger.Log("ffmpeg.exe " + jobInfo.Arguments);

            tabControl1.SelectTab(queueTab);

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (obj, ea) => bw_DoWork(obj, ea, jobInfo);
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.RunWorkerAsync();
        }

        // CREATE VIDEO

        private void textBox14_TextChanged(object sender, EventArgs e)
        {
            LoadImages();
        }

        private void textBox15_TextChanged(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(textBox15.Text);
            comboBox4.Text = fi.Extension.Remove(0, 1).ToUpper();
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
            catch
            {
                // ignored
            }
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
            string input = textBox14.Text + "\\" + comboBox6.Text;

            JobInfo jobInfo = Utility.CreateVideo(input, textBox15.Text, comboBox10.Text);

            logger.Log("ffmpeg.exe " + jobInfo.Arguments);

            tabControl1.SelectTab(queueTab);

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (obj, ea) => bw_DoWork(obj, ea, jobInfo);
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.RunWorkerAsync();
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

            if (videoInfo.Size.Width * videoInfo.Size.Height > 0)
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
            catch
            {
                // ignored
            }
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
            JobInfo jobInfo = Utility.ResizeVideo(textBox10.Text, textBox11.Text,
                (int)numericUpDown6.Value, (int)numericUpDown7.Value, comboBox11.Text, comboBox12.Text);

            logger.Log("ffmpeg.exe " + jobInfo.Arguments);

            tabControl1.SelectTab(queueTab);

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (obj, ea) => bw_DoWork(obj, ea, jobInfo);
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.RunWorkerAsync();
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
            JobInfo jobInfo = Utility.CropVideo(textBox12.Text, textBox13.Text,
                (int)numericUpDown1.Value, (int)numericUpDown3.Value, checkBox4.Checked, (int)numericUpDown5.Value, (int)numericUpDown4.Value);

            logger.Log("ffmpeg.exe " + jobInfo.Arguments);

            tabControl1.SelectTab(queueTab);

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (obj, ea) => bw_DoWork(obj, ea, jobInfo);
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.RunWorkerAsync();
        }

        // QUEUE

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            queueStopButton.Enabled = listView1.SelectedItems.Count > 0;
        }

        private ListViewItem AddToQueue(JobInfo jobInfo, Process ffmpegProcess)
        {
            ListViewItem item = new ListViewItem();
            item.Group = listView1.Groups[(int)jobInfo.Task];
            item.Tag = ffmpegProcess;
            item.Text = jobInfo.Input;
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

            return item;
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
                tabControl1.TabPages.Insert(7, outputTab);
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
            Process.Start(Properties.Resources.HomePage);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://" + linkLabel2.Text);
        }

        // TABCONTROL

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == outputTab && OutputEnabled)
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
                    if (!fi.Exists)
                        return;
                    textBox1.Text = files[0];
                    textBox2.Text = files[0].Replace(fi.Extension, "") + ".mp3";
                    break;
                case 1:
                    if (!fi.Exists)
                        return;
                    textBox6.Text = files[0];
                    textBox7.Text = fi.FullName.Replace(fi.Extension, "") + "_noaudio" + fi.Extension;
                    break;
                case 2:
                    if (!fi.Exists)
                        return;
                    textBox3.Text = files[0];
                    textBox4.Text = fi.FullName.Replace(fi.Extension, "") + "_Images";
                    break;
                case 3:
                    if (!fi.Exists)
                        return;
                    textBox10.Text = files[0];
                    textBox11.Text = fi.FullName.Replace(fi.Extension, "") + "_resized" + fi.Extension;
                    break;
                case 4:
                    if (!fi.Exists)
                        return;
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