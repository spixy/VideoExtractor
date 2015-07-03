using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace VideoExtractor
{
    class VideoInfo
    {
        public Size Size;
        public string Channels;
        public string SampleRate;
        public string AudioBitRate;
        public string VideoBitRate;
        public int Duration = -1;
    }

    class Utility
    {

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
                            if (words[i] == "kb/s" || words[i] == "kb/s,")
                            {
                                Video.VideoBitRate = words[i - 1];
                                break;
                            }

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
                        Video.SampleRate = words[words.Length - 6];

                        switch (words[words.Length - 4])
                        {
                            case "mono,": Video.Channels = "1"; break;
                            case "stereo,": Video.Channels = "2"; break;
                            case "2.1,": Video.Channels = "3"; break;
                            case "quad,": Video.Channels = "4"; break;
                            case "5.0,": Video.Channels = "5"; break;
                            case "5.1,": Video.Channels = "6"; break;
                            case "7.1,": Video.Channels = "8"; break;
                            default: Video.Channels = "Default"; break;
                        }
                        Video.AudioBitRate = words[words.Length - 2];
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
