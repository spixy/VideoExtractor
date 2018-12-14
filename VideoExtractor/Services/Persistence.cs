namespace VideoExtractor.Services
{
    public class Persistence
    {
        /// <summary>
        /// Save file path
        /// </summary>
        public string FilePath { get; }

        public Persistence(string filePath)
        {
            FilePath = filePath;
        }
    }
}
