using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDT.OCRBot.UI.Services
{
    public interface IDialogService
    {
        Task<List<string>?> OpenFileDialogAsync(string title, string filter, bool multiselect, string initialDirectory);
        Task<string?> OpenFolderDialogAsync(string description, string initialDirectory);
        void ShowMessage(string message, string title, string iconType = "Information");
    }
}
