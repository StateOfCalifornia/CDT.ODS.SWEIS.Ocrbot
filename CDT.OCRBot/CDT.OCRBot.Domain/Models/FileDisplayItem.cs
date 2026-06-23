using System;
using System.ComponentModel;
using System.IO;

namespace CDT.OCRBot.Domain.Models
{
    /// <summary>
    /// Represents a file item for display
    /// </summary>
    public class FileDisplayItem : INotifyPropertyChanged
    {
        private string _status = "Queued";
        private string _statusColor = "#546E7A";
        private string _icon = "⭕";
        private double _progressValue = 0;
        private string _outputPath = string.Empty;
        private string _outputPathVisibility = "Collapsed";
        private bool _isRemoveEnabled = true;

        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;

        public bool IsRemoveEnabled
        {
            get => _isRemoveEnabled;
            set
            {
                if (_isRemoveEnabled != value)
                {
                    _isRemoveEnabled = value;
                    OnPropertyChanged(nameof(IsRemoveEnabled));
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string StatusColor
        {
            get => _statusColor;
            set
            {
                if (_statusColor != value)
                {
                    _statusColor = value;
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        public string Icon
        {
            get => _icon;
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    OnPropertyChanged(nameof(Icon));
                }
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                if (Math.Abs(_progressValue - value) > 0.001)
                {
                    _progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                }
            }
        }

        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (_outputPath != value)
                {
                    _outputPath = value;
                    OnPropertyChanged(nameof(OutputPath));
                    // Show folder button when output path is set
                    OutputPathVisibility = string.IsNullOrEmpty(value) ? "Collapsed" : "Visible";
                }
            }
        }

        public string OutputPathVisibility
        {
            get => _outputPathVisibility;
            set
            {
                if (_outputPathVisibility != value)
                {
                    _outputPathVisibility = value;
                    OnPropertyChanged(nameof(OutputPathVisibility));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
