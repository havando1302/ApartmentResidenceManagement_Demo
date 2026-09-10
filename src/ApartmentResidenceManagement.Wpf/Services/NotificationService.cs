using System.Windows;
using ApartmentResidenceManagement.Wpf.Controls;
using ApartmentResidenceManagement.Wpf.Views.Dialogs;

namespace ApartmentResidenceManagement.Wpf.Services;

public static class NotificationService
{
    public static MessageBoxResult Show(string message, string title,
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.Information,
        string? details = null)
    {
        var application = System.Windows.Application.Current;
        var dispatcher = application?.Dispatcher;
        // A native emergency fallback still works when the WPF dispatcher has
        // already stopped (for example, during a fatal shutdown exception).
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return MessageBox.Show(message, title, buttons, image);

        if (!dispatcher.CheckAccess())
            return dispatcher.Invoke(() => Show(message, title, buttons, image, details));

        var kind = image switch
        {
            MessageBoxImage.Error => NotificationKind.Error,
            MessageBoxImage.Warning => NotificationKind.Warning,
            _ => NotificationKind.Information
        };
        var dialog = new NotificationDialog(message, title, buttons, kind, details);
        var owner = application!.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
            ?? application.Windows.OfType<Window>().FirstOrDefault(window => window.IsVisible);
        if (owner is not null)
        {
            dialog.Owner = owner;
            // A visible owner may be on a smaller screen than the primary one.
            // Keep the notification within the owner's available dimensions.
            if (owner.ActualWidth > 32)
                dialog.MaxWidth = Math.Min(dialog.MaxWidth, owner.ActualWidth - 32);
            if (owner.ActualHeight > 32)
                dialog.MaxHeight = Math.Min(dialog.MaxHeight, owner.ActualHeight - 32);
            dialog.Width = Math.Min(dialog.Width, dialog.MaxWidth);
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        dialog.ShowDialog();
        return dialog.Result;
    }
}
