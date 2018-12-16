using System;

namespace VideoExtractor.Services
{
    public static class Utility
    {
        /// <summary>
        /// Transforms seconds to string
        /// </summary>
        /// <param name="oldString">old value string (from TimeSpan.ToString)</param>
        /// <param name="newSeconds">new value in seconds</param>
        /// <param name="timeSpanFormat">TimeSpan string format</param>
        /// <returns>"hh:mm:ss" string</returns>
        public static string GetTimeSpanText(string oldString, int newSeconds, string timeSpanFormat)
        {
            int ms = TimeSpan.TryParseExact(oldString, timeSpanFormat, null, out TimeSpan ts) ? ts.Milliseconds : 0;
            return new TimeSpan(0, 0, 0, newSeconds, ms).ToString(timeSpanFormat);
        }

        /// <summary>
        /// Transforms string to seconds
        /// </summary>
        /// <param name="text">"hh:mm:ss" string</param>
        /// <param name="result">number of seconds</param>
        /// <returns>if success</returns>
        public static bool TryGetTotalSeconds(string text, out int result)
        {
            try
            {
                result = (int)new TimeSpan(int.Parse(text.Substring(0, 2)),
                                           int.Parse(text.Substring(3, 2)),
                                           int.Parse(text.Substring(6, 2))).TotalSeconds;
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }
}
