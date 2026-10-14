using System;

namespace FluxDownload.NET.Models
{
    /// <summary>
    /// Event arguments containing real-time download metrics.
    /// </summary>
    public sealed class DownloadProgressEventArgs : EventArgs
    {
        public string DownloadId { get; }
        public string FileName { get; }
        public long BytesReceived { get; }
        public long? TotalBytes { get; }
        public double Percentage => TotalBytes.HasValue && TotalBytes.Value > 0 
            ? (double)BytesReceived / TotalBytes.Value * 100.0 
            : 0.0;
        public double MegabytesPerSecond { get; }

        public DownloadProgressEventArgs(string downloadId, string fileName, long bytesReceived, long? totalBytes, double mbps)
        {
            DownloadId = downloadId;
            FileName = fileName;
            BytesReceived = bytesReceived;
            TotalBytes = totalBytes;
            MegabytesPerSecond = mbps;
        }
    }
}