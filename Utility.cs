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
        public int Duration = -1;
    }

    static class Utility
    {
        public static string ExtractVideo_Arguments(string input, string output, string samplerate, string bitrate, string channel, string startDate, string duration)
        {
            int val;

            samplerate = (Int32.TryParse(samplerate, out val)) ? " -ar " + samplerate : "";
            bitrate = (Int32.TryParse(bitrate, out val)) ? " -ab " + bitrate : "";
            channel = (Int32.TryParse(channel, out val)) ? " -ab " + channel : "";
            startDate = (!startDate.Contains("00:00:00.000")) ? " -ss " + startDate : "";
            duration = (!duration.Contains("00:00:00.000")) ? " -t " + duration : "";

            return "-i \"" + input + "\" -y -vn" + channel + samplerate + bitrate + startDate + duration + " \"" + output + "\"";
        }

        public static string RemoveAudio_Arguments(string input, string output)
        {
            return "-i \"" + input + "\" -y" + " -an \"" + output + "\"";
        }

        public static string ExtractImages_Arguments(string input, string output, int width, int height, string fps, string startDate, string duration)
        {
            startDate = (!startDate.Contains("00:00:00.000")) ? " -ss " + startDate : "";
            duration = (!duration.Contains("00:00:00.000")) ? " -t " + duration : "";
            string size = (width > 0 && height > 0) ? " -s " + width + "x" + height : "";

            return "-i \"" + input + "\" -r " + fps + duration + startDate + size + " -f image2 \"" + output + "\"";
        }

        public static string CropVideo_Arguments(string input, string output, int x, int y, bool center, int width, int height)
        {
            string area = (!center) ? ":" + x + ":" + y : "";

            return "-i \"" + input + "\" -y" + " -vf crop=" + width + ":" + height + area + " \"" + output + "\"";
        }

        public static string CreateVideo_Arguments(string input, string output, string fps)
        {
            return "-f image2 -i \"" + input + "\" -y" + " -r " + fps + " \"" + output + "\"";
        }

        public static string ResizeVideo_Arguments(string input, string output, int width, int height, string video_bitrate, string audio_bitrate)
        {
            int val;

            string size = " -vf scale=" +
                        ((width == 0) ? "w=iw" : "w=" + width) +
                        ((height == 0) ? ":h=ih" : ":h=" + height);

            video_bitrate = (Int32.TryParse(video_bitrate, out val)) ? " -b:v " + video_bitrate + "k" : "";
            audio_bitrate = (Int32.TryParse(audio_bitrate, out val)) ? " -b:a " + audio_bitrate + "k" : "";

            return "-i \"" + input + "\" -y" + video_bitrate + audio_bitrate + size + " \"" + output + "\"";
        }

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
                        int startindex = ea.Data.IndexOf("Duration: ") + ("Duration: ").Length;
                        string[] words = ea.Data.Substring(startindex, 11).Split(':');
                        words[2] = words[2].Substring(0, 2);
                        Video.Duration = 3600 * Convert.ToInt32(words[0]) + 60 * Convert.ToInt32(words[1]) + Convert.ToInt32(words[2]) + 1;
                    }
                    else if (ea.Data.Contains("Video: "))
                    {
                        string[] words = ea.Data.Split(' ');
                        int n;

                        for (int i = 0; i < words.Length; i++)
                        {
                            switch (words[i])
                            {
                                case "fps":
                                case "fps,":
                                    Double.TryParse(words[i - 1], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out Video.FPS);
                                    continue;

                                case "kb/s":
                                case "kb/s,":
                                    Int32.TryParse(words[i - 1], out Video.VideoBitRate);
                                    continue;
                            }

                            // Resolution
                            string[] tmp = words[i].Split('x');

                            if (tmp.Length == 2 && Int32.TryParse(tmp[0], out n) && Int32.TryParse(tmp[1], out n))
                            {
                                string[] ress = words[i].Split('x');
                                Video.Size = new Size(Convert.ToInt32(ress[0]), Convert.ToInt32(ress[1].Replace(",", "")));
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
                                case "mono,": Video.Channels = 1; break;
                                case "stereo,": Video.Channels = 2; break;
                                case "2.1,": Video.Channels = 3; break;
                                case "quad,": Video.Channels = 4; break;
                                case "5.0,": Video.Channels = 5; break;
                                case "5.1,": Video.Channels = 6; break;
                                case "7.1,": Video.Channels = 8; break;
                                case "Hz":
                                case "Hz,": Int32.TryParse(words[i - 1], out Video.SampleRate); break;
                                case "kb/s":
                                case "kb/s,": Int32.TryParse(words[i - 1], out Video.AudioBitRate); break;
                            }
                        }
                    }
                }
                catch {}
            };

            p.Start();
            p.BeginErrorReadLine();

            p.WaitForExit();
            p.Dispose();

            return Video;
        }
    }
}
