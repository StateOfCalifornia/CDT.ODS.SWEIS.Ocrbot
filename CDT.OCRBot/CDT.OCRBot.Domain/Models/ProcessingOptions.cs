
namespace CDT.OCRBot.Domain.Models
{
    /// <summary>
    /// Options for PDF processing modes
    /// </summary>
    public class ProcessingOptions
    {
        /// <summary>
        /// Creates a text-only PDF (visible text, no background images)
        /// Default: false (creates searchable PDF with original images)
        /// </summary>
        public bool TextOnly { get; init; }

        /// <summary>
        /// Adds PDF/UA accessibility tags with semantic structure
        /// Requires Azure OpenAI service and adds processing time
        /// Default: false
        /// </summary>
        public bool AddUATags { get; init; }

        /// <summary>
        /// Enables export of extracted OCR text to .txt file
        /// Default: false
        /// </summary>
        public bool EnableTextDump { get; init; }

        /// <summary>
        /// Indicates whether the tagging service is required for these options
        /// </summary>
        public bool RequiresTaggingService => AddUATags;

        /// <summary>
        /// Indicates whether the original PDF bytes are needed
        /// </summary>
        public bool RequiresOriginalPdf => !TextOnly || AddUATags;

        /// <summary>
        /// Gets a human-readable description of the processing mode
        /// </summary>
        /// <returns>Description string</returns>
        public string GetDescription()
        {
            return (TextOnly, AddUATags) switch
            {
                (false, false) => "Searchable PDF (invisible text over original)",
                (true, false) => "Text-only PDF (no images)",
                (false, true) => "Tagged searchable PDF (PDF/UA compliant)",
                (true, true) => "Tagged text-only PDF (PDF/UA compliant)",
            };
        }

        /// <summary>
        /// Gets the estimated base processing time multiplier
        /// </summary>
        /// <returns>Multiplier for time estimation (1.0 = base time)</returns>
        public double GetTimeMultiplier()
        {
            return AddUATags ? 3.5 : 1.0;
        }
    }
}




