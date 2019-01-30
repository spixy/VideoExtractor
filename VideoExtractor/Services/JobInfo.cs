using System.Diagnostics;

namespace VideoExtractor.Services
{
    public class JobInfo
    {
        public enum ETask
        {
            ExtractAudio,
            RemoveAudio,
            ExtractImages,
            ResizeVideo,
            CropVideo,
            CreateVideo
        }

        public enum EResult
        {
            Success,
            Cancel,
            Error,
            NotAvailable
        }

        public string Input { get; private set; }
        public string Output { get; private set; }
        public string Arguments { get; private set; }
        public Process Process { get; private set; }
        public ETask Task { get; private set; }
        public EResult Result { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="input">input file/folder</param>
        /// <param name="output">output file/folder</param>
        /// <param name="arguments">ffmpeg arguments</param>
        /// <param name="task">task type</param>
        public JobInfo(string input, string output, string arguments, ETask task)
        {
            Input = input;
            Output = output;
            Task = task;
            Arguments = arguments;
            Result = EResult.NotAvailable;
        }

        /// <summary>
        /// Creates FFmpeg process
        /// </summary>
        /// <param name="ffmpegPath">FFmpeg path</param>
        /// <returns>FFmpeg process</returns>
        public Process CreateProcess(string ffmpegPath)
        {
            Process = new Process
            {
                StartInfo =
                {
                    FileName = ffmpegPath,
                    Arguments = Arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            return Process;
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
            return new JobInfo(input, output, arguments, ETask.ExtractAudio);
        }
    }
}
