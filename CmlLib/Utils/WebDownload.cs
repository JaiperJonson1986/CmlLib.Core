﻿using CmlLib.Core.Downloader;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CmlLib.Utils
{
    class WebDownload
    {
        static int DefaultBufferSize = 1024 * 64; // 64kb

        public event EventHandler<FileDownloadProgress> FileDownloadProgressChanged;
        public event ProgressChangedEventHandler DownloadProgressChangedEvent;

        public void DownloadFile(string url, string path)
        {
            var req = WebRequest.CreateHttp(url); // Request
            var response = req.GetResponse();
            var filesize = long.Parse(response.Headers.Get("Content-Length")); // Get File Length

            var webStream = response.GetResponseStream(); // Get NetworkStream
            var fileStream = File.Open(path, FileMode.Create); // Open FileStream

            var bufferSize = DefaultBufferSize; // Make buffer
            var buffer = new byte[bufferSize];
            var length = 0;

            var fireEvent = filesize > DefaultBufferSize;
            var processedBytes = 0;

            while ((length = webStream.Read(buffer, 0, bufferSize)) > 0) // read to end and write file
            {
                fileStream.Write(buffer, 0, length);

                // raise event
                if (fireEvent)
                {
                    processedBytes += length;
                    ProgressChanged(processedBytes, filesize);
                }
            }

            buffer = null;
            webStream.Dispose(); // Close streams
            fileStream.Dispose();
        }

        public async Task DownloadFileAsync(DownloadFile file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file.Path));
            using (var wc = new WebClient())
            {
                long lastBytes = 0;

                wc.DownloadProgressChanged += (s, e) =>
                {
                    var progressedBytes = e.BytesReceived - lastBytes;
                    lastBytes = e.BytesReceived;

                    var progress = new FileDownloadProgress(
                        file, e.TotalBytesToReceive, progressedBytes, e.BytesReceived, e.ProgressPercentage);
                    FileDownloadProgressChanged?.Invoke(this, progress);
                };
                await wc.DownloadFileTaskAsync(file.Url, file.Path);
            }
        }

        public void DownloadFileLimit(string url, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var req = WebRequest.CreateHttp(url);
            req.Method = "GET";
            req.Timeout = 5000;
            req.ReadWriteTimeout = 5000;
            req.ContinueTimeout = 5000;
            var res = req.GetResponse();

            using (var httpStream = res.GetResponseStream())
            using (var fs = File.OpenWrite(path))
            {
                httpStream.CopyTo(fs);
            }
        }

        public async Task DownloadFileLimitTaskAsync(string url, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var req = WebRequest.CreateHttp(url);
            req.Method = "GET";
            req.Timeout = 5000;
            req.ReadWriteTimeout = 5000;
            req.ContinueTimeout = 5000;
            var res = await req.GetResponseAsync();
            
            using (var httpStream = res.GetResponseStream())
            using (var fs = File.OpenWrite(path))
            {
                await httpStream.CopyToAsync(fs);
            }
        }

        void ProgressChanged(long value, long max)
        {
            var percentage = (float)value / max * 100;

            var e = new ProgressChangedEventArgs((int)percentage, null);
            DownloadProgressChangedEvent?.Invoke(this, e);
        }
    }
}
