namespace DaftAppleGames.Editor.ModPublisher
{
    public struct UploadProgress
    {
        public float Progress { get; }
        public string Status { get; }

        /// <summary>
        /// Creates an immutable publishing progress update
        /// </summary>
        public UploadProgress(float progress, string status)
        {
            Progress = progress;
            Status = status;
        }
    }
}
