using System;
using System.Diagnostics;
using System.IO;
using System.Media;

namespace VideoExtractor
{
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
        public static JobInfo ExtractAudio(string input, string output, string samplerate, string bitrate, string channel, string startDate, string duration, bool overwrite)
        {
            int val;

            samplerate = (int.TryParse(samplerate, out val)) ? " -ar " + samplerate : "";
            bitrate = (int.TryParse(bitrate, out val)) ? " -ab " + bitrate : "";
            channel = (int.TryParse(channel, out val)) ? " -ab " + channel : "";
            startDate = (!startDate.Contains("00:00:00.000")) ? " -ss " + startDate : "";
            duration = (!duration.Contains("00:00:00.000")) ? " -t " + duration : "";

            string arguments = "-i \"" + input + "\" -vn" + channel + samplerate + bitrate + startDate + duration + (overwrite ? " -y" : "") + " \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.ExtractAudio);
        }

        /// <summary>
        /// Returns job to extract images
        /// </summary>
        public static JobInfo ExtractImages(string input, string output, int width, int height, string fps, string startDate, string duration, bool overwrite)
        {
            startDate = (!startDate.Contains("00:00:00.000")) ? " -ss " + startDate : "";
            duration = (!duration.Contains("00:00:00.000")) ? " -t " + duration : "";
            string size = (width > 0 && height > 0) ? " -s " + width + "x" + height : "";

            string arguments = "-i \"" + input + "\" -r " + fps + duration + startDate + size + (overwrite ? " -y" : "") + " -f image2 \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.ExtractImages);
        }

        /// <summary>
        /// Returns job to remove audio
        /// </summary>
        public static JobInfo RemoveAudio(string input, string output, bool overwrite)
        {
            string arguments = "-i \"" + input + "\" -an" + (overwrite ? " -y" : "") + " \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.RemoveAudio);
        }

        /// <summary>
        /// Returns job to create video from images
        /// </summary>
        public static JobInfo CreateVideo(string input, string output, string fps, bool overwrite)
        {
            string arguments = "-f image2 -i \"" + input + "\" -r " + fps + (overwrite ? " -y" : "") + " \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.CreateVideo);
        }

        /// <summary>
        /// Returns job to resize video
        /// </summary>
        public static JobInfo ResizeVideo(string input, string output, int width, int height, string video_bitrate, string audio_bitrate, bool overwrite)
        {
            int val;

            string size = " -vf scale=" +
                        ((width == 0) ? "w=iw" : "w=" + width) +
                        ((height == 0) ? ":h=ih" : ":h=" + height);

            video_bitrate = (int.TryParse(video_bitrate, out val)) ? " -b:v " + video_bitrate + "k" : "";
            audio_bitrate = (int.TryParse(audio_bitrate, out val)) ? " -b:a " + audio_bitrate + "k" : "";

            string arguments = "-i \"" + input + "\"" + video_bitrate + audio_bitrate + size + (overwrite ? " -y" : "") + " \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.ResizeVideo);
        }

        /// <summary>
        /// Returns job to crop video
        /// </summary>
        public static JobInfo CropVideo(string input, string output, int x, int y, bool center, int width, int height, bool overwrite)
        {
            string area = (!center) ? ":" + x + ":" + y : "";
            string arguments = "-i \"" + input + "\" -vf crop=" + width + ":" + height + area + (overwrite ? " -y" : "") + " \"" + output + "\"";

            return new JobInfo(input, output, arguments, Task.CropVideo);
        }

        /// <summary>
        /// Launch Explorer with selected file or folder
        /// </summary>
        /// <param name="path">file or folder</param>
        public static bool LaunchExplorer(string path)
        {
            // File
            if (File.Exists(path))
            {
                Process.Start("explorer.exe", @"/select, " + path);
                return true;
            }
            // Directory
            else if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", path);
                return true;
            }
            // Not found
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Remove file or folder
        /// </summary>
        /// <param name="path">file or folder</param>
        public static void RemovePath(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
