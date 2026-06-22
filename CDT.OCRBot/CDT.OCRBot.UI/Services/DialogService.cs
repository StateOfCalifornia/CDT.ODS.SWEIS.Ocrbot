using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using WinFormsDialogResult = System.Windows.Forms.DialogResult;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace CDT.OCRBot.UI.Services
{
    public class DialogService : IDialogService
    {
        public async Task<List<string>?> OpenFileDialogAsync(string title, string filter, bool multiselect, string initialDirectory)
        {
            return await Task.Run(() =>
            {
                List<string>? selectedFiles = null;
                bool? dialogResult = null;

                var thread = new Thread(() =>
                {
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = title,
                        Filter = filter,
                        Multiselect = multiselect,
                        InitialDirectory = initialDirectory,
                        RestoreDirectory = false
                    };

                    dialogResult = openFileDialog.ShowDialog();

                    if (dialogResult == true)
                    {
                        selectedFiles = openFileDialog.FileNames.ToList();
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                return selectedFiles;
            });
        }

        public async Task<string?> OpenFolderDialogAsync(string description, string initialDirectory)
        {
            return await Task.Run(() =>
            {
                string? selectedFolder = null;
                bool? dialogResult = null;

                var thread = new Thread(() =>
                {
                    using var folderDialog = new FolderBrowserDialog
                    {
                        Description = description,
                        SelectedPath = initialDirectory,
                        ShowNewFolderButton = false
                    };

                    dialogResult = folderDialog.ShowDialog() == WinFormsDialogResult.OK;

                    if (dialogResult == true)
                    {
                        selectedFolder = folderDialog.SelectedPath;
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                return selectedFolder;
            });
        }

        public void ShowMessage(string message, string title, string iconType = "Information")
        {
            MessageBoxImage icon = iconType switch
            {
                "Error" => MessageBoxImage.Error,
                "Warning" => MessageBoxImage.Warning,
                "Question" => MessageBoxImage.Question,
                _ => MessageBoxImage.Information
            };

            try 
            {
                System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            }
            catch
            {
                System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            }
        }
    }
}
