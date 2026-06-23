namespace CDT.OCRBot.Domain.Models
{
    /// <summary>
    /// Represents the current processing phase
    /// </summary>
    public enum ProcessingPhase
    {
        Initializing,
        ReadingPdf,
        AnalyzingOcr,
        AutoTagging,
        CreatingPdf,
        Saving,
        Complete
    }

    /// <summary>
    /// Provides detailed progress information for PDF processing
    /// </summary>
    public class ProcessingProgress
    {
        /// <summary>
        /// Current processing phase
        /// </summary>
        public ProcessingPhase Phase { get; set; }

        /// <summary>
        /// Overall progress percentage (0-100)
        /// </summary>
        public double OverallPercent { get; set; }

        /// <summary>
        /// Progress within the current phase (0-100)
        /// </summary>
        public double PhasePercent { get; set; }

        /// <summary>
        /// Human-readable status message
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Additional detail about current operation
        /// </summary>
        public string? Detail { get; set; }

        /// <summary>
        /// Current item being processed (e.g., page number, batch number)
        /// </summary>
        public int CurrentItem { get; set; }

        /// <summary>
        /// Total items to process
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Creates a progress report for the Initializing phase
        /// </summary>
        public static ProcessingProgress Initializing(double percent = 0)
        {
            return new ProcessingProgress
            {
                Phase = ProcessingPhase.Initializing,
                OverallPercent = MapToOverallPercent(ProcessingPhase.Initializing, percent),
                PhasePercent = percent,
                StatusMessage = "Initializing..."
            };
        }

        /// <summary>
        /// Creates a progress report for the Reading PDF phase
        /// </summary>
        public static ProcessingProgress ReadingPdf(double percent = 0)
        {
            return new ProcessingProgress
            {
                Phase = ProcessingPhase.ReadingPdf,
                OverallPercent = MapToOverallPercent(ProcessingPhase.ReadingPdf, percent),
                PhasePercent = percent,
                StatusMessage = "Reading PDF..."
            };
        }

        /// <summary>
        /// Creates a progress report for the OCR Analysis phase
        /// </summary>
        public static ProcessingProgress AnalyzingOcr(int currentPage, int totalPages, string? detail = null)
        {
            var phasePercent = totalPages > 0 ? (currentPage * 100.0 / totalPages) : 0;
            return new ProcessingProgress
            {
                Phase = ProcessingPhase.AnalyzingOcr,
                OverallPercent = MapToOverallPercent(ProcessingPhase.AnalyzingOcr, phasePercent),
                PhasePercent = phasePercent,
                StatusMessage = "Analyzing OCR...",
                Detail = detail ?? $"Processing page {currentPage} of {totalPages}",
                CurrentItem = currentPage,
                TotalItems = totalPages
            };
        }

        /// <summary>
        /// Creates a progress report for OCR completion
        /// </summary>
        public static ProcessingProgress OcrComplete(int totalPages)
        {
            return new ProcessingProgress
            {
                Phase = ProcessingPhase.AnalyzingOcr,
                OverallPercent = MapToOverallPercent(ProcessingPhase.AnalyzingOcr, 100),
                PhasePercent = 100,
                StatusMessage = "OCR complete",
                Detail = $"Extracted {totalPages} pages",
                CurrentItem = totalPages,
                TotalItems = totalPages
            };
        }

        /// <summary>
        /// Creates a progress report for the Auto-Tagging phase
        /// </summary>
        public static ProcessingProgress AutoTagging(int currentBatch, int totalBatches, string? detail = null)
        {
            var phasePercent = totalBatches > 0 ? (currentBatch * 100.0 / totalBatches) : 0;
            return new ProcessingProgress
            {
                Phase = ProcessingPhase.AutoTagging,
                OverallPercent = MapToOverallPercent(ProcessingPhase.AutoTagging, phasePercent),
                PhasePercent = phasePercent,
                StatusMessage = "Auto Tagging...",
                Detail = detail ?? $"Processing batch {currentBatch} of {totalBatches}",
                CurrentItem = currentBatch,
                TotalItems = totalBatches
            };
        }

        /// <summary>
        /// Creates a progress report for the PDF Creation phase
        /// </summary>
        public static ProcessingProgress CreatingPdf(int currentPage, int totalPages, string? detail = null)
        {
            var phasePercent = totalPages > 0 ? (currentPage * 100.0 / totalPages) : 0;
            return new ProcessingProgress
            {
                Phase = ProcessingPhase.CreatingPdf,
                OverallPercent = MapToOverallPercent(ProcessingPhase.CreatingPdf, phasePercent),
                PhasePercent = phasePercent,
                StatusMessage = "Creating PDF...",
                Detail = detail ?? $"Processing page {currentPage} of {totalPages}",
                CurrentItem = currentPage,
                TotalItems = totalPages
            };
        }

        /// <summary>
        /// Creates a progress report for the Saving phase
        /// </summary>
        public static ProcessingProgress Saving(double percent = 0)
        {
            return new ProcessingProgress
            {
                Phase = ProcessingPhase.Saving,
                OverallPercent = MapToOverallPercent(ProcessingPhase.Saving, percent),
                PhasePercent = percent,
                StatusMessage = "Saving..."
            };
        }

        /// <summary>
        /// Creates a progress report for completion
        /// </summary>
        public static ProcessingProgress Complete()
        {
            return new ProcessingProgress
            {
                Phase = ProcessingPhase.Complete,
                OverallPercent = 100,
                PhasePercent = 100,
                StatusMessage = "Complete!"
            };
        }

        /// <summary>
        /// Maps phase progress to overall progress based on phase weight.
        /// Phase distribution:
        /// - Initializing:   0-5%   (5%)
        /// - Reading PDF:    5-10%  (5%)
        /// - Analyzing OCR:  10-50% (40%) - largest phase
        /// - Auto Tagging:   50-75% (25%) - significant for tagged PDFs
        /// - Creating PDF:   75-90% (15%)
        /// - Saving:         90-100% (10%)
        /// </summary>
        private static double MapToOverallPercent(ProcessingPhase phase, double phasePercent)
        {
            phasePercent = Math.Clamp(phasePercent, 0, 100);

            return phase switch
            {
                ProcessingPhase.Initializing => 0 + (phasePercent * 0.05),           // 0-5%
                ProcessingPhase.ReadingPdf => 5 + (phasePercent * 0.05),             // 5-10%
                ProcessingPhase.AnalyzingOcr => 10 + (phasePercent * 0.40),          // 10-50%
                ProcessingPhase.AutoTagging => 50 + (phasePercent * 0.25),           // 50-75%
                ProcessingPhase.CreatingPdf => 75 + (phasePercent * 0.15),           // 75-90%
                ProcessingPhase.Saving => 90 + (phasePercent * 0.10),                // 90-100%
                ProcessingPhase.Complete => 100,
                _ => 0
            };
        }

        /// <summary>
        /// Gets a formatted status string with percentage for backward compatibility
        /// </summary>
        public string ToStatusString()
        {
            var status = StatusMessage;
            if (!string.IsNullOrEmpty(Detail))
            {
                status = Detail;
            }
            return $"{status} {Math.Round(OverallPercent)}%";
        }

        /// <summary>
        /// Gets a formatted status string without percentage
        /// </summary>
        public string ToDisplayString()
        {
            if (!string.IsNullOrEmpty(Detail))
            {
                return $"{StatusMessage} ({Detail})";
            }
            return StatusMessage;
        }
    }
}




