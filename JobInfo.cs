namespace VideoExtractor
{
    enum Task
    {
        ExtractAudio,
        RemoveAudio,
        ExtractImages,
        ResizeVideo,
        CropVideo,
        CreateVideo
    }

    enum Result
    {
        Success,
        Cancel,
        Error,
        NotAvailable
    }

    class JobInfo
    {
        public string Input { get; private set; }
        public string Output { get; private set; }
        public string Arguments { get; private set; }
        public Task Task { get; private set; }
        public Result Result { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="input">input file/folder</param>
        /// <param name="output">output file/folder</param>
        /// <param name="arguments">ffmpeg arguments</param>
        /// <param name="task">task type</param>
        public JobInfo(string input, string output, string arguments, Task task)
        {
            Input = input;
            Output = output;
            Task = task;
            Arguments = arguments;
            Result = Result.NotAvailable;
        }
    }
}
