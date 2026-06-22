using CDT.OCRBot.Domain;
using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;
using CDT.OCRBot.Domain.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CDT.OCRBot.UI.Services
{
    /// <summary>
    /// Manages file selection state and validation
    /// </summary>
    public class FileSelectionService
    {
        private readonly List<string> _selectedFiles = new();

        /// <summary>
        /// Gets the maximum number of files allowed
        /// </summary>
        public int MaxFiles => AppConstants.Processing.MaxFilesPerBatch;

        /// <summary>
        /// Gets the list of currently selected files
        /// </summary>
        public IReadOnlyList<string> SelectedFiles => _selectedFiles.AsReadOnly();

        /// <summary>
        /// Gets the number of currently selected files
        /// </summary>
        public int Count => _selectedFiles.Count;

        /// <summary>
        /// Gets the number of available slots for additional files
        /// </summary>
        public int AvailableSlots => AppConstants.Processing.MaxFilesPerBatch - _selectedFiles.Count;

        /// <summary>
        /// Checks if the maximum file limit has been reached
        /// </summary>
        public bool IsAtMaxFiles => _selectedFiles.Count >= AppConstants.Processing.MaxFilesPerBatch;

        /// <summary>
        /// Adds files to the selection, respecting max limit and avoiding duplicates
        /// </summary>
        /// <param name="files">Files to add</param>
        /// <returns>Number of files actually added</returns>
        public int AddFiles(IEnumerable<string> files)
        {
            int addedCount = 0;

            foreach (var file in files)
            {
                if (_selectedFiles.Count >= AppConstants.Processing.MaxFilesPerBatch)
                    break;

                if (!_selectedFiles.Contains(file))
                {
                    _selectedFiles.Add(file);
                    addedCount++;
                }
            }

            return addedCount;
        }

        /// <summary>
        /// Removes a specific file from the selection
        /// </summary>
        /// <param name="filePath">Path of file to remove</param>
        /// <returns>True if file was removed, false if not found</returns>
        public bool RemoveFile(string filePath)
        {
            return _selectedFiles.Remove(filePath);
        }

        /// <summary>
        /// Clears all selected files
        /// </summary>
        public void ClearAll()
        {
            _selectedFiles.Clear();
        }

        /// <summary>
        /// Validates and filters files to fit available slots
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <returns>Tuple of (canAddAll, filesToAdd)</returns>
        public (bool canAddAll, List<string> filesToAdd) ValidateAndFilterFiles(IEnumerable<string> files)
        {
            var filesList = files.ToList();
            int available = AvailableSlots;

            if (filesList.Count <= available)
            {
                return (true, filesList);
            }
            else
            {
                return (false, filesList.Take(available).ToList());
            }
        }

        /// <summary>
        /// Gets file selection display items
        /// </summary>
        public List<FileDisplayItem> GetDisplayItems()
        {
            return _selectedFiles.Select(f => new FileDisplayItem
            {
                Name = Path.GetFileNameWithoutExtension(f),
                FullPath = f
            }).ToList();
        }
    }





}
