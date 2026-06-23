using System.Windows;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace CDT.OCRBot.UI.Views
{
    public partial class CustomMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        private CustomMessageBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Show a custom message box with specified parameters
        /// </summary>
        public static MessageBoxResult Show(
            string message,
            string title = "OCRBot",
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.Information,
            Window? owner = null)
        {
            var messageBox = new CustomMessageBox();

            // Only set owner if it's provided and not the message box itself
            var targetOwner = owner ?? System.Windows.Application.Current.MainWindow;
            if (targetOwner != null && targetOwner != messageBox)
            {
                messageBox.Owner = targetOwner;
            }

            messageBox.titleTextBlock.Text = title;
            messageBox.messageTextBlock.Text = message;

            // Set icon and colors based on message type
            switch (icon)
            {
                case MessageBoxImage.Information:
                    messageBox.iconTextBlock.Text = "?";
                    messageBox.iconTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    messageBox.iconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD"));
                    break;
                case MessageBoxImage.Warning:
                    messageBox.iconTextBlock.Text = "?";
                    messageBox.iconTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"));
                    messageBox.iconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0"));
                    break;
                case MessageBoxImage.Error:
                    messageBox.iconTextBlock.Text = "?";
                    messageBox.iconTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
                    messageBox.iconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                    break;
                case MessageBoxImage.Question:
                    messageBox.iconTextBlock.Text = "?";
                    messageBox.iconTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
                    messageBox.iconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD"));
                    break;
            }

            // Add buttons based on MessageBoxButton type
            messageBox.AddButtons(buttons);

            messageBox.ShowDialog();

            return messageBox.Result;
        }

        private void AddButtons(MessageBoxButton buttons)
        {
            buttonPanel.Children.Clear();

            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton("OK", MessageBoxResult.OK, true, "#43A047");
                    break;

                case MessageBoxButton.OKCancel:
                    AddButton("Cancel", MessageBoxResult.Cancel, false, "#95A5A6");
                    AddButton("OK", MessageBoxResult.OK, true, "#43A047");
                    break;

                case MessageBoxButton.YesNo:
                    AddButton("No", MessageBoxResult.No, false, "#E74C3C");
                    AddButton("Yes", MessageBoxResult.Yes, true, "#43A047");
                    break;

                case MessageBoxButton.YesNoCancel:
                    AddButton("Cancel", MessageBoxResult.Cancel, false, "#95A5A6");
                    AddButton("No", MessageBoxResult.No, false, "#E74C3C");
                    AddButton("Yes", MessageBoxResult.Yes, true, "#43A047");
                    break;
            }
        }

        private void AddButton(string content, MessageBoxResult result, bool isDefault, string colorHex)
        {
            var button = new Button
            {
                Content = content,
                MinWidth = 70,
                Height = 28,
                Padding = new Thickness(14, 0, 14, 0),
                Margin = new Thickness(6, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                IsDefault = isDefault,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                BorderThickness = new Thickness(0),
                Style = (Style)FindResource("MessageBoxButtonStyle")
            };

            button.Click += (sender, e) =>
            {
                Result = result;
                Close();
            };

            buttonPanel.Children.Add(button);
        }

    }
}




