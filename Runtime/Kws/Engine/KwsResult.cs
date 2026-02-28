namespace PonyuDev.SherpaOnnx.Kws.Engine
{
    /// <summary>
    /// Result of keyword spotting: the detected keyword string.
    /// </summary>
    public sealed class KwsResult
    {
        /// <summary>The detected keyword text.</summary>
        public string Keyword { get; }

        /// <summary>True when Keyword is not null or empty.</summary>
        public bool IsValid => !string.IsNullOrEmpty(Keyword);

        internal KwsResult(string keyword)
        {
            Keyword = keyword;
        }
    }
}
