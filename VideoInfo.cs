using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace VideoExtractor
{
    struct VideoInfo
    {
        public Size Size;
        public int Channels;
        public int SampleRate;
        public int AudioBitRate;
        public int VideoBitRate;
        public double FPS;
        public int Duration;

        /// <summary>
        /// Get video information from ffmpeg
        /// </summary>
        /// <param name="ffmpeg">ffmpeg file path</param>
        /// <param name="filename">video file path</param>
        public static VideoInfo LoadVideoInfo(string ffmpeg, string filename)
        {
            VideoInfo Video = new VideoInfo();

            if (!File.Exists(filename) || !File.Exists(ffmpeg))
                return Video;

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
                    loadVideoInfo(ea.Data, ref Video);
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

            return Video;
        }

        /// <summary>
        /// Parse line and store data to structure
        /// </summary>
        private static void loadVideoInfo(string line, ref VideoInfo Video)
        {
            if (line.Contains("Duration: "))
            {
                string[] words = line.Split(' ');

                for (int i = 0; i < words.Length; i++)
                {
                    switch (words[i])
                    {
                        // Total length
                        case "Duration:":
                            string[] times = words[i + 1].Split(':');
                            times[2] = times[2].Substring(0, 2);
                            Video.Duration = 3600 * Convert.ToInt32(times[0]) + 60 * Convert.ToInt32(times[1]) + Convert.ToInt32(times[2]) + 1;
                            break;
                    }
                }
            }
            else if (line.Contains("Video: "))
            {
                string[] words = line.Split(' ');
                int w, h;

                for (int i = 0; i < words.Length; i++)
                {
                    switch (words[i])
                    {
                        // FPS
                        case "fps":
                        case "fps,":
                            double.TryParse(words[i - 1], NumberStyles.Any, CultureInfo.InvariantCulture, out Video.FPS);
                            continue;

                        // Video bitrate
                        case "kb/s":
                        case "kb/s,":
                            int.TryParse(words[i - 1], out Video.VideoBitRate);
                            continue;
                    }

                    // Resolution
                    string[] ress = words[i].Replace(",", "").Split('x');

                    if (ress.Length == 2 && int.TryParse(ress[0], out w) && int.TryParse(ress[1], out h))
                    {
                        Video.Size = new Size(w, h);
                    }
                }

            }
            else if (line.Contains("Audio: "))
            {
                string[] words = line.Split(' ');

                for (int i = 0; i < words.Length; i++)
                {
                    switch (words[i])
                    {
                        // audio channels
                        case "mono,": Video.Channels = 1; break;
                        case "stereo,": Video.Channels = 2; break;
                        case "2.1,": Video.Channels = 3; break;
                        case "quad,": Video.Channels = 4; break;
                        case "5.0,": Video.Channels = 5; break;
                        case "5.1,": Video.Channels = 6; break;
                        case "7.1,": Video.Channels = 8; break;
                        // audio sample rate
                        case "Hz":
                        case "Hz,": int.TryParse(words[i - 1], out Video.SampleRate); break;
                        // audio bitrate
                        case "kb/s":
                        case "kb/s,": int.TryParse(words[i - 1], out Video.AudioBitRate); break;
                    }
                }
            }
        }
    }
}
