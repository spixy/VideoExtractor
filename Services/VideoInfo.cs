using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace VideoExtractor
{
    public class VideoInfo
    {
        public Size Resolution { get; }
        public int Channels { get; }
        public int SampleRate { get; }
        public int AudioBitRate { get; }
        public int VideoBitRate { get; }
        public double Fps { get; }
        public int Duration { get; }

        /// <summary>
        /// Get video information from ffmpeg
        /// </summary>
        /// <param name="ffmpeg">ffmpeg file path</param>
        /// <param name="filename">video file path</param>
        public static VideoInfo LoadVideoInfo(string ffmpeg, string filename)
        {
            VideoInfo video = null;

            if (!File.Exists(filename) || !File.Exists(ffmpeg))
                return null;

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = ffmpeg,
                    Arguments = "-i \"" + filename + "\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            p.ErrorDataReceived += (s, ea) =>
            {
                if (ea.Data == null)
                    return;

                try
                {
                    video = new VideoInfo(ea.Data);
                }
                catch
                {
                    // ignored
                }
            };

            p.Start();
            p.BeginErrorReadLine();

            p.WaitForExit();
            p.Dispose();

            return video;
        }

        /// <summary>
        /// Parse line and store data to structure
        /// </summary>
        private VideoInfo(string line)
        {
            if (line.Contains("Duration: "))
            {
                string[] words = line.Split(' ');
                int length = words.Length;

                for (int i = 0; i < length; i++)
                {
                    switch (words[i])
                    {
                        // Total length
                        case "Duration:":
                            string[] times = words[i + 1].Split(':');
                            times[2] = times[2].Substring(0, 2);
                            Duration = 3600 * Convert.ToInt32(times[0]) + 60 * Convert.ToInt32(times[1]) + Convert.ToInt32(times[2]) + 1;
                            break;
                    }
                }
            }
            else if (line.Contains("Video: "))
            {
                string[] words = line.Split(' ');
                int length = words.Length;

                for (int i = 0; i < length; i++)
                {
                    switch (words[i])
                    {
                        // FPS
                        case "fps":
                        case "fps,":
                            if (double.TryParse(words[i - 1], NumberStyles.Any, CultureInfo.InvariantCulture, out double fps))
                                Fps = fps;
                            continue;

                        // Video bitrate
                        case "kb/s":
                        case "kb/s,":
                            if (int.TryParse(words[i - 1], out int videoBitRate))
                                VideoBitRate = videoBitRate;
                            continue;
                    }

                    // Resolution
                    string[] ress = words[i].Replace(",", "").Split('x');
                    if (ress.Length == 2 && int.TryParse(ress[0], out int w) && int.TryParse(ress[1], out int h))
                    {
                        Resolution = new Size(w, h);
                    }
                }

            }
            else if (line.Contains("Audio: "))
            {
                string[] words = line.Split(' ');
                int length = words.Length;

                for (int i = 0; i < length; i++)
                {
                    switch (words[i])
                    {
                        // audio channels
                        case "mono,": Channels = 1; break;
                        case "stereo,": Channels = 2; break;
                        case "2.1,": Channels = 3; break;
                        case "quad,": Channels = 4; break;
                        case "5.0,": Channels = 5; break;
                        case "5.1,": Channels = 6; break;
                        case "7.1,": Channels = 8; break;

                        // audio sample rate
                        case "Hz":
                        case "Hz,":
                            if (int.TryParse(words[i - 1], out int sampleRate))
                                SampleRate = sampleRate;
                            break;

                        // audio bitrate
                        case "kb/s":
                        case "kb/s,":
                            if (int.TryParse(words[i - 1], out int audioBitRate))
                                AudioBitRate = audioBitRate;

                               break;
                    }
                }
            }
        }
    }
}
