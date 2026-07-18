#nullable enable
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OWCE.Network
{
    public class CustomProgressableStreamContent : HttpContent
    {
        readonly FileStream _fileStream;
        readonly int _bufferSize = 4096;
        readonly IProgress<double> _progress;
        int _lastProgress;

        public CustomProgressableStreamContent(FileStream fileStream, IProgress<double> progress)
        {
            //ArgumentNullException.ThrowIfNull(fileStream);
            //ArgumentNullException.ThrowIfNull(progress);

            _fileStream = fileStream;
            _progress = progress;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return SerializeToStreamAsync(stream, default);
        }

        protected async Task SerializeToStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[_bufferSize];
            long size = _fileStream.Length;
            int uploaded = 0;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                int length = await _fileStream.ReadAsync(buffer.AsMemory(0, _bufferSize), cancellationToken).ConfigureAwait(false);

                if (length <= 0)
                {
                    break;
                }

                uploaded += length;

                await stream.WriteAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);



                // Only report progress when we have actually gone up a percent
                double currentProgress = (double)uploaded / size;
                int currentProgressInt = (int)(currentProgress * 100);
                if (_lastProgress != currentProgressInt)
                {
                    _progress.Report(currentProgress);
                    _lastProgress = currentProgressInt;
                }
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _fileStream.Length;
            return true;
        }
    }

}

