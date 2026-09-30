using System;
using System.IO;
using System.Windows;
using ApartmentResidenceManagement.Domain.Exceptions;

namespace ApartmentResidenceManagement.Wpf.Services;

/// <summary>
/// Boundary for exceptions that reach the presentation layer. Technical details
/// are logged locally; users only see a safe, actionable message.
/// </summary>
public static class ExceptionHandlingService
{
    private const string DefaultMessage =
        "Đã xảy ra lỗi khi xử lý thao tác. Vui lòng thử lại. Nếu lỗi tiếp tục xảy ra, hãy khởi động lại ứng dụng.";

    public static void Handle(Exception exception)
    {
        Handle(exception, null);
    }

    public static void Handle(Exception exception, string? userMessage)
    {
        ArgumentNullException.ThrowIfNull(exception);
        TryWriteLog(exception);

        var displayMessage = !string.IsNullOrWhiteSpace(userMessage)
            ? userMessage
            : FindBusinessRuleException(exception)?.Message ?? DefaultMessage;

        try
        {
            NotificationService.Show(
                displayMessage,
                "Không thể hoàn tất thao tác",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // Error presentation must never let the original exception escape
            // back to ICommand/Dispatcher and interrupt the UI thread.
            try
            {
                MessageBox.Show(
                    displayMessage,
                    "Không thể hoàn tất thao tác",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // There is no usable UI surface (typically during shutdown).
            }
        }
    }

    private static BusinessRuleException? FindBusinessRuleException(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is BusinessRuleException businessRuleException)
            {
                return businessRuleException;
            }
        }

        return null;
    }

    private static void TryWriteLog(Exception exception)
    {
        try
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ApartmentResidenceManagement",
                "Logs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "errors.log");
            File.AppendAllText(
                logPath,
                $"[{DateTimeOffset.Now:O}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Logging must not replace or hide the original user notification.
        }
    }
}
