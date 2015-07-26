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
    }

    static class Utility
    {
        /// <summary>
        /// Transforms string to seconds
        /// </summary>
        /// <param name="text">"hh:mm:ss" string</param>
        /// <returns>number of seconds</returns>
        public static int GetTotalSeconds(string text)
        {
            return (int)new TimeSpan(Convert.ToInt32(text.Substring(0, 2)),
                Convert.ToInt32(text.Substring(3, 2)),
                Convert.ToInt32(text.Substring(6, 2))).TotalSeconds;
        }

        /// <summary>
        /// Transforms seconds to string
        /// </summary>
        /// <param name="s">seconds</param>
        /// <returns>"hh:mm:ss" string</returns>
        public static string GetTimeSpanText(int s)
        {
            TimeSpan ts = new TimeSpan(0, 0, s);
            return string.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
        }

        /// <summary>
        /// Returns job to extract audio
        /// </summary>
        public static JobInfo ExtractAudio(string input, string output, string samplerate, string bitrate, string channel, string startDate, string duration)
        {
            int val;

            samplerate = (int.TryParse(samplerate, out val)) ? " -ar " + samplerate : "";
            bitrate = (int.TryParse(bitrate, out val)) ? " -ab " + bitrate : "";
            channel = (int.TryParse(channel, out val)) ? " -ab " + channel : "";
            startDate = (!startDate.Contains("00:00:00.000")) ? " -ss " + startDate : "";
            duration = (!duration.Contains("00:00:00.000")) ? " -t " + duration : "";

            string arguments = "-i \"" + input + "\" -y -vn" + channel + samplerate + bitrate + startDate + duration + " \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.ExtractAudio);
        }

        /// <summary>
        /// Returns job to extract images
        /// </summary>
        public static JobInfo ExtractImages(string input, string output, int width, int height, string fps, string startDate, string duration)
        {
            startDate = (!startDate.Contains("00:00:00.000")) ? " -ss " + startDate : "";
            duration = (!duration.Contains("00:00:00.000")) ? " -t " + duration : "";
            string size = (width > 0 && height > 0) ? " -s " + width + "x" + height : "";

            string arguments = "-i \"" + input + "\" -r " + fps + duration + startDate + size + " -f image2 \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.ExtractImages);
        }

        /// <summary>
        /// Returns job to remove audio
        /// </summary>
        public static JobInfo RemoveAudio(string input, string output)
        {
            string arguments = "-i \"" + input + "\" -y" + " -an \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.RemoveAudio);
        }

        /// <summary>
        /// Returns job to create video from images
        /// </summary>
        public static JobInfo CreateVideo(string input, string output, string fps)
        {
            string arguments = "-f image2 -i \"" + input + "\" -y" + " -r " + fps + " \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.CreateVideo);
        }

        /// <summary>
        /// Returns job to resize video
        /// </summary>
        public static JobInfo ResizeVideo(string input, string output, int width, int height, string video_bitrate, string audio_bitrate)
        {
            int val;

            string size = " -vf scale=" +
                        ((width == 0) ? "w=iw" : "w=" + width) +
                        ((height == 0) ? ":h=ih" : ":h=" + height);

            video_bitrate = (int.TryParse(video_bitrate, out val)) ? " -b:v " + video_bitrate + "k" : "";
            audio_bitrate = (int.TryParse(audio_bitrate, out val)) ? " -b:a " + audio_bitrate + "k" : "";

            string arguments = "-i \"" + input + "\" -y" + video_bitrate + audio_bitrate + size + " \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.ResizeVideo);
        }

        /// <summary>
        /// Returns job to crop video
        /// </summary>
        public static JobInfo CropVideo(string input, string output, int x, int y, bool center, int width, int height)
        {
            string area = (!center) ? ":" + x + ":" + y : "";
            string arguments = "-i \"" + input + "\" -y" + " -vf crop=" + width + ":" + height + area + " \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.CropVideo);
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
    }
}
