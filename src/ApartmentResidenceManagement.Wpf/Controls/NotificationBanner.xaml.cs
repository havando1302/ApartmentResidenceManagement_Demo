using System.Windows;
using System.Windows.Controls;

namespace ApartmentResidenceManagement.Wpf.Controls;

public enum NotificationKind
{
    Error,
    Success,
    Warning,
    Information
}

public partial class NotificationBanner : UserControl
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(NotificationBanner), new PropertyMetadata(string.Empty, OnMessageChanged));

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(NotificationKind), typeof(NotificationBanner), new PropertyMetadata(NotificationKind.Error));

    static NotificationBanner()
    {
        // Coercion preserves page-level visibility bindings/triggers while an empty
        // message takes up no space, including the control's margin.
        VisibilityProperty.OverrideMetadata(typeof(NotificationBanner), new FrameworkPropertyMetadata(
            Visibility.Visible, null, (element, value) =>
                string.IsNullOrWhiteSpace(((NotificationBanner)element).Message) ? Visibility.Collapsed : value));
    }

    public NotificationBanner()
    {
        InitializeComponent();
        CoerceValue(VisibilityProperty);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public NotificationKind Kind
    {
        get => (NotificationKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    private static void OnMessageChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        var banner = (NotificationBanner)element;
        banner.CoerceValue(VisibilityProperty);
        banner.MessageScroller?.ScrollToTop();
    }
}
