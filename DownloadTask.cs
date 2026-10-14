using System;
using System.IO;
using System.Threading;

namespace FluxDownload.NET.Models
{
    /// <summary>
    /// Represents an individual file download task configuration and state.
    /// </summary>
    public sealed class DownloadTask
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public Uri Url { get; }
        public string DestinationPath { get; }
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        public DownloadTask(string url, string destinationPath)
        {
            Url = new Uri(url);
            DestinationPath = destinationPath;
        }
    }
}