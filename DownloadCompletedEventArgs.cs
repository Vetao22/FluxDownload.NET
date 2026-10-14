using System;

namespace FluxDownload.NET.Models
{
    /// <summary>
    /// Event arguments fired when a file download finishes (success or failure).
    /// </summary>
    public sealed class DownloadCompletedEventArgs : EventArgs
    {
        public string DownloadId { get; }
        public string FilePath { get; }
        public bool IsSuccess { get; }
        public Exception Error { get; }
        public TimeSpan ElapsedTime { get; }

        public DownloadCompletedEventArgs(string downloadId, string filePath, bool isSuccess, Exception error, TimeSpan elapsedTime)
        {
            DownloadId = downloadId;
            FilePath = filePath;
            IsSuccess = isSuccess;
            Error = error;
            ElapsedTime = elapsedTime;
        }
    }
}