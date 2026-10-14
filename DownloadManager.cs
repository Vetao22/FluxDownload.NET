using System;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using FluxDownload.NET.Models;

namespace FluxDownload.NET
{
    /// <summary>
    /// High-performance asynchronous multi-file download engine with concurrency throttling and events.
    /// </summary>
    public sealed class DownloadManager : IDisposable
    {
        private static readonly HttpClient SharedHttpClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private readonly SemaphoreSlim _concurrencySemaphore;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeTasks = new ConcurrentDictionary<string, CancellationTokenSource>();

        // Events
        public event EventHandler<DownloadProgressEventArgs> OnProgressChanged;
        public event EventHandler<DownloadCompletedEventArgs> OnDownloadCompleted;

        public DownloadManager(int maxConcurrentDownloads = 4)
        {
            _concurrencySemaphore = new SemaphoreSlim(maxConcurrentDownloads, maxConcurrentDownloads);
        }

        /// <summary>
        /// Downloads a single file asynchronously with progress reporting and cancellation support.
        /// </summary>
        public async Task DownloadFileAsync(DownloadTask task)
        {
            await _concurrencySemaphore.WaitAsync();
            _activeTasks[task.Id] = task.CancellationTokenSource;
            var stopwatch = Stopwatch.StartNew();

            bool success = false;
            Exception caughtException = null;

            try
            {
                using var response = await SharedHttpClient.GetAsync(task.Url, HttpCompletionOption.ResponseHeadersRead, task.CancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                string fileName = Path.GetFileName(task.DestinationPath);

                using var contentStream = await response.Content.ReadAsStreamAsync(task.CancellationTokenSource.Token);
                
                // Ensure directory exists safely
                string dir = Path.GetDirectoryName(task.DestinationPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using var fileStream = new FileStream(task.DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                byte[] buffer = new byte[81920];
                long totalBytesRead = 0;
                int bytesRead;
                long lastBytesReadForSpeed = 0;
                var speedStopwatch = Stopwatch.StartNew();

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, task.CancellationTokenSource.Token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, task.CancellationTokenSource.Token);
                    totalBytesRead += bytesRead;

                    // Calculate speed and trigger progress event every ~200ms
                    if (speedStopwatch.ElapsedMilliseconds >= 200)
                    {
                        double elapsedSeconds = speedStopwatch.ElapsedMilliseconds / 1000.0;
                        double bytesPerSec = (totalBytesRead - lastBytesReadForSpeed) / elapsedSeconds;
                        double mbps = bytesPerSec / (1024 * 1024);

                        OnProgressChanged?.Invoke(this, new DownloadProgressEventArgs(task.Id, fileName, totalBytesRead, totalBytes, mbps));

                        lastBytesReadForSpeed = totalBytesRead;
                        speedStopwatch.Restart();
                    }
                }

                // Final 100% progress trigger
                OnProgressChanged?.Invoke(this, new DownloadProgressEventArgs(task.Id, fileName, totalBytesRead, totalBytes, 0));
                success = true;
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
            finally
            {
                stopwatch.Stop();
                _activeTasks.TryRemove(task.Id, out _);
                _concurrencySemaphore.Release();

                OnDownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs(task.Id, task.DestinationPath, success, caughtException, stopwatch.Elapsed));
            }
        }

        /// <summary>
        /// Downloads multiple files concurrently utilizing the configured parallelism limit.
        /// </summary>
        public async Task DownloadMultipleAsync(params DownloadTask[] tasks)
        {
            var downloadTasks = new Task[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                downloadTasks[i] = DownloadFileAsync(tasks[i]);
            }
            await Task.WhenAll(downloadTasks);
        }

        /// <summary>
        /// Safely aborts an ongoing download task.
        /// </summary>
        public void CancelDownload(string downloadId)
        {
            if (_activeTasks.TryGetValue(downloadId, out var cts))
            {
                cts.Cancel();
            }
        }

        public void Dispose()
        {
            _concurrencySemaphore.Dispose();
            foreach (var cts in _activeTasks.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
        }
    }
}