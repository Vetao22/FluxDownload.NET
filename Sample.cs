using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluxDownload.NET;
using FluxDownload.NET.Models;

namespace FluxDownloadDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing FluxDownload.NET Engine...\n");

            // Initialize manager allowing up to 3 parallel downloads simultaneously
            using var downloader = new DownloadManager(maxConcurrentDownloads: 3);

            // Hook up events for progress and completion
            downloader.OnProgressChanged += (sender, e) =>
            {
                Console.CursorLeft = 0;
                Console.Write($"[{e.FileName}] Progress: {e.Percentage:F1}% | Speed: {e.MegabytesPerSecond:F2} MB/s ({e.BytesReceived} bytes)");
            };

            downloader.OnDownloadCompleted += (sender, e) =>
            {
                Console.WriteLine();
                if (e.IsSuccess)
                {
                    Console.WriteLine($"[SUCCESS] Download completed: {e.FilePath} in {e.ElapsedTime.TotalSeconds:F2}s");
                }
                else
                {
                    Console.WriteLine($"[ERROR] Download failed for {e.FilePath}: {e.Error?.Message}");
                }
            };

            // Setup multiple sample files to download
            string downloadsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");
            
            var task1 = new DownloadTask("https://speed.hetzner.de/100MB.bin", Path.Combine(downloadsDir, "file_100mb.bin"));
            var task2 = new DownloadTask("https://speed.hetzner.de/10MB.bin", Path.Combine(downloadsDir, "file_10mb.bin"));
            var task3 = new DownloadTask("https://speed.hetzner.de/1MB.bin", Path.Combine(downloadsDir, "file_1mb.bin"));

            Console.WriteLine("Starting concurrent batch download of 3 files...\n");

            // Example of safely canceling a specific task after 1.5 seconds just for demo purposes
            var cancelTimer = new Timer(_ =>
            {
                Console.WriteLine($"\n[CANCELLATION DEMO] Aborting task 1 ({task1.Id})...");
                downloader.CancelDownload(task1.Id);
            }, null, 1500, Timeout.Infinite);

            // Execute parallel batch download
            await downloader.DownloadMultipleAsync(task1, task2, task3);

            cancelTimer.Dispose();
            Console.WriteLine("\nAll download operations processed.");
        }
    }
}