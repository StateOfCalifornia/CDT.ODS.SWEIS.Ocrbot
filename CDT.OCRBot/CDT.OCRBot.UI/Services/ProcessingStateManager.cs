using System;
using System.Threading;

namespace CDT.OCRBot.UI.Services
{
    /// <summary>
    /// Manages processing state and cancellation
    /// </summary>
    public class ProcessingStateManager
    {
        private bool _isProcessing;
        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Gets whether processing is currently active
        /// </summary>
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// Gets the current cancellation token source (null if not processing)
        /// </summary>
        public CancellationTokenSource? CancellationTokenSource => _cancellationTokenSource;

        /// <summary>
        /// Starts a new processing session
        /// </summary>
        /// <returns>Cancellation token to use for the processing operation</returns>
        public CancellationToken StartProcessing()
        {
            if (_isProcessing)
                throw new InvalidOperationException("Processing is already in progress");

            _isProcessing = true;
            _cancellationTokenSource = new CancellationTokenSource();
            return _cancellationTokenSource.Token;
        }

        /// <summary>
        /// Requests cancellation of the current processing operation
        /// </summary>
        public void CancelProcessing()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Completes the processing session and cleans up resources
        /// </summary>
        public void CompleteProcessing()
        {
            _isProcessing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        /// <summary>
        /// Gets whether the user can start processing (has files and not already processing)
        /// </summary>
        public bool CanStartProcessing(int fileCount)
        {
            return fileCount > 0 && !_isProcessing;
        }
    }
}


