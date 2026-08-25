namespace DaftAppleGames.Editor.ModPublisher
{
    public struct UploadProgress
    {
        public float Progress { get; }
        public string Status { get; }

        /// <summary>
        /// Creates an immutable Nexus upload progress update
        /// </summary>
        public UploadProgress(float progress, string status)
        {
            Progress = progress;
            Status = status;
        }
    }
}
