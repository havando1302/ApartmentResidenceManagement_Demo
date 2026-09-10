using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ApartmentResidenceManagement.Wpf.Controls;

namespace ApartmentResidenceManagement.Wpf.Views.Dialogs;

public partial class NotificationDialog : Window
{
    public MessageBoxResult Result { get; private set; }

    public NotificationDialog(string message, string title, MessageBoxButton buttons,
        NotificationKind kind, string? details = null)
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        Notice.Message = message;
        Notice.Kind = kind;
        if (!string.IsNullOrWhiteSpace(details))
        {
            DetailsText.Text = details;
            DetailsExpander.Visibility = Visibility.Visible;
        }

        var workArea = SystemParameters.WorkArea;
        MaxWidth = Math.Max(1, workArea.Width - 32);
        MaxHeight = Math.Max(1, workArea.Height - 32);
        Width = Math.Min(480, MaxWidth);

        Result = buttons switch
        {
            MessageBoxButton.YesNo => MessageBoxResult.No,
            MessageBoxButton.OK => MessageBoxResult.OK,
            _ => MessageBoxResult.Cancel
        };

        switch (buttons)
        {
            case MessageBoxButton.YesNo:
                AddButton("Hủy", MessageBoxResult.No, primary: false, isCancel: true);
                AddButton("Đồng ý", MessageBoxResult.Yes, primary: true);
                break;
            case MessageBoxButton.YesNoCancel:
                AddButton("Hủy", MessageBoxResult.Cancel, primary: false, isCancel: true);
                AddButton("Không", MessageBoxResult.No, primary: false);
                AddButton("Có", MessageBoxResult.Yes, primary: true);
                break;
            case MessageBoxButton.OKCancel:
                AddButton("Hủy", MessageBoxResult.Cancel, primary: false, isCancel: true);
                AddButton("Đồng ý", MessageBoxResult.OK, primary: true);
                break;
            default:
                AddButton("Đã hiểu", MessageBoxResult.OK, primary: true, isCancel: true);
                break;
        }
    }

    private void AddButton(string label, MessageBoxResult result, bool primary, bool isCancel = false)
    {
        var button = new Button
        {
            Content = label,
            MinHeight = 44,
            Margin = new Thickness(4, 0, 4, 0),
            IsCancel = isCancel,
            IsDefault = isCancel,
            // Startup errors may occur before application theme resources load.
            Style = TryFindResource(primary ? "PrimaryButton" : "OutlineButton") as Style
        };
        button.Click += (_, _) =>
        {
            Result = result;
            Close();
        };
        Actions.Children.Add(button);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}
