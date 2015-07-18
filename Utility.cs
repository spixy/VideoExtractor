using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace VideoExtractor
{
    class VideoInfo
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
        /// Transforms hh:mm:ss string to number of seconds
        /// </summary>
        public static double GetTotalSeconds(string text)
        {
            return new TimeSpan(Convert.ToInt32(text.Substring(0, 2)),
                Convert.ToInt32(text.Substring(3, 2)),
                Convert.ToInt32(text.Substring(6, 2))).TotalSeconds;
        }

        /// <summary>
        /// Transforms seconds to hh:mm:ss string
        /// </summary>
        public static string GetTimeSpanText(int s)
        {
            TimeSpan ts = new TimeSpan(0, 0, s);
            return string.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
        }

        /// <summary>
        /// Returns ffmpeg arguments to extract audio
        /// </summary>
        public static string ExtractAudio_Arguments(string input, string output, string samplerate, string bitrate, string channel, string startDate, string duration)
        {
            int val;

            samplerate = (int.TryParse(samplerate, out val)) ? " -ar " + samplerate : "";
            bitrate = (int.TryParse(bitrate, out val)) ? " -ab " + bitrate : "";
            channel = (int.TryParse(channel, out val)) ? " -ab " + channel : "";
            startDate = (!startDate.Contains("00:00:00.000")) ? " -ss " + startDate : "";
            duration = (!duration.Contains("00:00:00.000")) ? " -t " + duration : "";

            return "-i \"" + input + "\" -y -vn" + channel + samplerate + bitrate + startDate + duration + " \"" + output + "\"";
        }

        /// <summary>
        /// Returns ffmpeg arguments to remove audio
        /// </summary>
        public static string RemoveAudio_Arguments(string input, string output)
        {
            return "-i \"" + input + "\" -y" + " -an \"" + output + "\"";
        }

        /// <summary>
        /// Returns ffmpeg arguments to extract images
        /// </summary>
        public static string ExtractImages_Arguments(string input, string output, int width, int height, string fps, string startDate, string duration)
        {
            startDate = (!startDate.Contains("00:00:00.000")) ? " -ss " + startDate : "";
            duration = (!duration.Contains("00:00:00.000")) ? " -t " + duration : "";
            string size = (width > 0 && height > 0) ? " -s " + width + "x" + height : "";

            return "-i \"" + input + "\" -r " + fps + duration + startDate + size + " -f image2 \"" + output + "\"";
        }

        /// <summary>
        /// Returns ffmpeg arguments to crop video
        /// </summary>
        public static string CropVideo_Arguments(string input, string output, int x, int y, bool center, int width, int height)
        {
            string area = (!center) ? ":" + x + ":" + y : "";
            return "-i \"" + input + "\" -y" + " -vf crop=" + width + ":" + height + area + " \"" + output + "\"";
        }

        /// <summary>
        /// Returns ffmpeg arguments to create video from images
        /// </summary>
        public static string CreateVideo_Arguments(string input, string output, string fps)
        {
            return "-f image2 -i \"" + input + "\" -y" + " -r " + fps + " \"" + output + "\"";
        }

        /// <summary>
        /// Returns ffmpeg arguments to resize video
        /// </summary>
        public static string ResizeVideo_Arguments(string input, string output, int width, int height, string video_bitrate, string audio_bitrate)
        {
            int val;

            string size = " -vf scale=" +
                        ((width == 0) ? "w=iw" : "w=" + width) +
                        ((height == 0) ? ":h=ih" : ":h=" + height);

            video_bitrate = (int.TryParse(video_bitrate, out val)) ? " -b:v " + video_bitrate + "k" : "";
            audio_bitrate = (int.TryParse(audio_bitrate, out val)) ? " -b:a " + audio_bitrate + "k" : "";

            return "-i \"" + input + "\" -y" + video_bitrate + audio_bitrate + size + " \"" + output + "\"";
        }

        /// <summary>
        /// Get video information from ffmpeg
        /// </summary>
        public static VideoInfo LoadVideoInfo(string ffmpeg, string filename)
        {
            VideoInfo Video = new VideoInfo();

            if (!File.Exists(filename) || !File.Exists(ffmpeg))
                return Video;

            Process p = new Process();
            p.StartInfo.FileName = ffmpeg;
            p.StartInfo.Arguments = "-i \"" + filename + "\"";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            p.ErrorDataReceived += (s, ea) =>
            {
                if (ea.Data == null)
                    return;

                try
                {
                    if (ea.Data.Contains("Duration: "))
                    {
                        // Video length
                        int startindex = ea.Data.IndexOf("Duration: ", StringComparison.Ordinal) + ("Duration: ").Length;
                        string[] words = ea.Data.Substring(startindex, 11).Split(':');
                        words[2] = words[2].Substring(0, 2);
                        Video.Duration = 3600 * Convert.ToInt32(words[0]) + 60 * Convert.ToInt32(words[1]) + Convert.ToInt32(words[2]) + 1;
                    }
                    else if (ea.Data.Contains("Video: "))
                    {
                        string[] words = ea.Data.Split(' ');
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
                    else if (ea.Data.Contains("Audio: "))
                    {
                        string[] words = ea.Data.Split(' ');

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
