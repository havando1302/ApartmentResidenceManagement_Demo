using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Wpf.Controls;
using ApartmentResidenceManagement.Wpf.ViewModels;
using ApartmentResidenceManagement.Wpf.Views;
using ApartmentResidenceManagement.Wpf.Views.Dialogs;
using Xunit;

namespace ApartmentResidenceManagement.Wpf.Tests;

[Collection(WpfTestCollection.Name)]
public sealed class NotificationLayoutTests(WpfDispatcherFixture fixture)
{
    private const string OwnerError = "Căn hộ mới (1002) đã có sẵn Chủ hộ. Không thể đặt làm Chủ hộ.";
    private static readonly string LongMessage = string.Join("\n", Enumerable.Range(1, 30)
        .Select(index => $"Dòng {index}: Không thể lưu thông tin cư dân. Vui lòng kiểm tra căn hộ và ngày bắt đầu cư trú."));

    [Fact]
    public void Empty_messages_release_layout_space_and_binding_updates_restore_the_banner()
    {
        fixture.Run(() =>
        {
            var state = new NotificationState();
            var banner = new NotificationBanner { DataContext = state };
            banner.SetBinding(NotificationBanner.MessageProperty, nameof(NotificationState.ErrorMessage));
            using var host = new InvisibleHost(banner, 340, 200);

            Assert.Equal(Visibility.Collapsed, banner.Visibility);
            Assert.Equal(new Size(), banner.DesiredSize);

            state.ErrorMessage = OwnerError;
            host.Settle();
            Assert.True(banner.IsVisible);
            AssertMessageFullyReachable(host, banner, OwnerError);

            state.ErrorMessage = " \r\n\t ";
            host.Settle();
            Assert.Equal(Visibility.Collapsed, banner.Visibility);
            Assert.Equal(new Size(), banner.DesiredSize);
        });
    }

    [Theory]
    [InlineData("vietnamese", 340)]
    [InlineData("multiline", 280)]
    [InlineData("unbroken", 240)]
    public void Narrow_banner_wraps_and_scrolls_to_the_last_character_without_horizontal_clipping(string scenario, double width)
    {
        fixture.Run(() =>
        {
            var message = scenario switch
            {
                "vietnamese" => string.Join(" ", Enumerable.Repeat(OwnerError, 20)),
                "multiline" => LongMessage,
                _ => new string('W', 1_200) + "KẾT_THÚC"
            };
            var banner = new NotificationBanner { Message = message, VerticalAlignment = VerticalAlignment.Top };
            using var host = new InvisibleHost(banner, width, 240);

            Assert.InRange(banner.ActualHeight, 1, 160.5);
            var scroller = Assert.Single(Descendants<ScrollViewer>(banner));
            Assert.True(scroller.ScrollableHeight > 0, "Long notifications must be scrollable instead of truncated.");
            AssertMessageFullyReachable(host, banner, message);
            AssertUnobscured(host, banner);
        });
    }

    [Theory]
    [InlineData(typeof(ApartmentView), "IsFormOpen", "SaveApartmentCommand", "Register")]
    [InlineData(typeof(ResidentView), "IsFormOpen", "SaveResidentCommand", "Register")]
    [InlineData(typeof(ResidentView), "IsAccountPanelOpen", "CloseAccountPanelCommand", "Register")]
    [InlineData(typeof(ResidencyView), "IsFormOpen", "SaveResidencyCommand", "Register")]
    [InlineData(typeof(ResidencyView), "IsFormOpen", "SaveResidencyCommand", "Transfer")]
    [InlineData(typeof(ResidencyView), "IsFormOpen", "SaveResidencyCommand", "Terminate")]
    [InlineData(typeof(VehicleView), "IsFormOpen", "SaveVehicleCommand", "Register")]
    [InlineData(typeof(MyVehiclesView), "IsFormOpen", "SaveVehicleCommand", "Register")]
    public void Modal_error_is_in_front_of_the_overlay_and_above_visible_actions_at_small_height(
        Type viewType, string openProperty, string actionCommand, string actionType)
    {
        fixture.Run(() =>
        {
            var state = new NotificationState
            {
                ErrorMessage = LongMessage,
                ActionType = actionType,
                FormTitle = "CHUYỂN CĂN HỘ - Lâm Văn S2"
            };
            typeof(NotificationState).GetProperty(openProperty)!.SetValue(state, true);
            var view = (UserControl)Activator.CreateInstance(viewType)!;
            view.DataContext = state;
            using var host = new InvisibleHost(view, 960, 540);

            var captureDirectory = Environment.GetEnvironmentVariable("RMS_NOTIFICATION_CAPTURE_DIR");
            if (viewType == typeof(ResidencyView) && actionType == "Transfer" && !string.IsNullOrWhiteSpace(captureDirectory))
            {
                System.IO.Directory.CreateDirectory(captureDirectory);
                state.ErrorMessage = OwnerError;
                host.Settle();
                host.SaveScreenshot(System.IO.Path.Combine(captureDirectory, "residency-transfer-notification.png"));
                state.ErrorMessage = LongMessage;
                host.Settle();
                host.SaveScreenshot(System.IO.Path.Combine(captureDirectory, "residency-transfer-long-notification.png"));
            }

            var banner = Assert.Single(Descendants<NotificationBanner>(view),
                item => item.IsVisible && item.Message == LongMessage);
            Assert.Contains(Ancestors(banner).OfType<Panel>(), panel => Panel.GetZIndex(panel) >= 100);
            var action = Assert.Single(Descendants<Button>(view), button => button.IsVisible &&
                BindingOperations.GetBinding(button, Button.CommandProperty)?.Path.Path == actionCommand);
            AssertAlertAboveAction(host, banner, action);
            AssertMessageFullyReachable(host, banner, LongMessage);
            AssertUnobscured(host, banner);
            AssertUnobscured(host, action);
        });
    }

    [Theory]
    [InlineData(typeof(ApartmentView))]
    [InlineData(typeof(ResidentView))]
    [InlineData(typeof(ResidencyView))]
    [InlineData(typeof(VehicleView))]
    [InlineData(typeof(MyVehiclesView))]
    [InlineData(typeof(ResidencyHistoryView))]
    [InlineData(typeof(StatisticsView))]
    public void Page_errors_remain_readable_when_no_form_is_open(Type viewType)
    {
        fixture.Run(() =>
        {
            var view = (UserControl)Activator.CreateInstance(viewType)!;
            view.DataContext = new NotificationState { ErrorMessage = LongMessage };
            using var host = new InvisibleHost(view, 960, 540);

            var banner = Assert.Single(Descendants<NotificationBanner>(view),
                item => item.IsVisible && item.Message == LongMessage);
            AssertMessageFullyReachable(host, banner, LongMessage);
            AssertUnobscured(host, banner);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Profile_errors_and_success_messages_leave_save_action_accessible(bool success)
    {
        fixture.Run(() =>
        {
            var view = new MyProfileView
            {
                DataContext = new NotificationState
                {
                    ErrorMessage = success ? string.Empty : LongMessage,
                    SuccessMessage = success ? LongMessage : string.Empty
                }
            };
            using var host = new InvisibleHost(view, 960, 540);
            var banner = Assert.Single(Descendants<NotificationBanner>(view), item => item.IsVisible);
            Assert.Equal(success ? NotificationKind.Success : NotificationKind.Error, banner.Kind);
            var action = Assert.Single(Descendants<Button>(view), button =>
                BindingOperations.GetBinding(button, Button.CommandProperty)?.Path.Path == "SaveProfileCommand");
            AssertAlertAboveAction(host, banner, action);
            AssertMessageFullyReachable(host, banner, LongMessage);
            AssertUnobscured(host, action);
        });
    }

    [Fact]
    public void Login_error_can_be_read_in_full_without_covering_the_login_button()
    {
        fixture.Run(() =>
        {
            // No command is invoked, so constructing this view needs no account
            // service, application startup, database, or visible native window.
            var window = new LoginWindow(new LoginViewModel(null!));
            try
            {
                var content = (FrameworkElement)window.Content;
                window.Content = null;
                content.DataContext = new NotificationState { ErrorMessage = LongMessage };
                using var host = new InvisibleHost(content, 900, 550);
                var banner = Assert.Single(Descendants<NotificationBanner>(content), item => item.IsVisible);
                var action = Assert.Single(Descendants<Button>(content), button =>
                    BindingOperations.GetBinding(button, Button.CommandProperty)?.Path.Path == "LoginCommand");
                AssertAlertAboveAction(host, banner, action);
                AssertMessageFullyReachable(host, banner, LongMessage);
                AssertUnobscured(host, action);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Theory]
    [InlineData(MessageBoxButton.OK, MessageBoxResult.OK, 1)]
    [InlineData(MessageBoxButton.OKCancel, MessageBoxResult.Cancel, 2)]
    [InlineData(MessageBoxButton.YesNo, MessageBoxResult.No, 2)]
    [InlineData(MessageBoxButton.YesNoCancel, MessageBoxResult.Cancel, 3)]
    public void Standalone_notifications_show_full_text_and_keep_confirmation_actions_accessible(
        MessageBoxButton buttons, MessageBoxResult dismissedResult, int actionCount)
    {
        fixture.Run(() =>
        {
            var dialog = new NotificationDialog(LongMessage, "Xác nhận thay đổi thông tin cư dân", buttons,
                NotificationKind.Warning, "Chi tiết: " + new string('X', 500));
            try
            {
                var content = (FrameworkElement)dialog.Content;
                dialog.Content = null;
                using var host = new InvisibleHost(content, 480, 540);
                Assert.Equal(dismissedResult, dialog.Result);
                var actions = Assert.Single(Descendants<UniformGrid>(content));
                var actionButtons = actions.Children.OfType<Button>().ToArray();
                Assert.Equal(actionCount, actionButtons.Length);
                var defaultAction = Assert.Single(actionButtons, button => button.IsDefault);
                Assert.True(defaultAction.IsCancel, "Closing or pressing Enter must preserve the cancellation default.");
                var banner = Assert.Single(Descendants<NotificationBanner>(content));
                AssertMessageFullyReachable(host, banner, LongMessage);
                Assert.All(actionButtons, action =>
                {
                    AssertAlertAboveAction(host, banner, action);
                    AssertUnobscured(host, action);
                });

                var details = Assert.Single(Descendants<Expander>(content));
                Assert.True(details.IsVisible);
                details.IsExpanded = true;
                host.Settle();
                // Expanding long diagnostic information must not push the
                // confirmation/cancel actions outside the available viewport.
                Assert.All(actionButtons, action => AssertUnobscured(host, action));
                var detailText = Assert.Single(Descendants<TextBox>(details));
                Assert.Contains(new string('X', 500), detailText.Text);
                Assert.Equal(TextWrapping.Wrap, detailText.TextWrapping);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Theory]
    [InlineData("Hủy", MessageBoxResult.No)]
    [InlineData("Đồng ý", MessageBoxResult.Yes)]
    public void Confirmation_buttons_preserve_yes_no_results_for_existing_callers(string label, MessageBoxResult expected)
    {
        fixture.Run(() =>
        {
            var dialog = new NotificationDialog("Bạn có muốn xóa bản ghi đã chọn?", "Xác nhận", MessageBoxButton.YesNo,
                NotificationKind.Warning);
            try
            {
                var content = (FrameworkElement)dialog.Content;
                dialog.Content = null;
                using var host = new InvisibleHost(content, 480, 300);
                var action = Assert.Single(Descendants<Button>(content), button => Equals(button.Content, label));
                action.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(expected, dialog.Result);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    private static void AssertAlertAboveAction(InvisibleHost host, NotificationBanner banner, Button action)
    {
        var alertBounds = BoundsIn(banner, host.Root);
        var actionBounds = BoundsIn(action, host.Root);
        Assert.True(alertBounds.Bottom <= actionBounds.Top + 0.5,
            $"The notification ({alertBounds}) overlaps the action ({actionBounds}).");
        AssertContained(alertBounds, new Rect(host.Root.RenderSize));
        AssertContained(actionBounds, new Rect(host.Root.RenderSize));
    }

    private static void AssertMessageFullyReachable(InvisibleHost host, NotificationBanner banner, string message)
    {
        var text = Assert.Single(Descendants<TextBlock>(banner), block => block.Text == message);
        var scroller = Assert.Single(Descendants<ScrollViewer>(banner));
        Assert.Equal(TextWrapping.Wrap, text.TextWrapping);
        Assert.Equal(TextTrimming.None, text.TextTrimming);
        Assert.Equal(0, scroller.ScrollableWidth);
        Assert.True(text.ActualWidth > 0);
        Assert.True(scroller.ViewportHeight > 0);

        scroller.ScrollToEnd();
        host.Settle();
        Assert.InRange(Math.Abs(scroller.VerticalOffset - scroller.ScrollableHeight), 0, 0.5);
        var viewport = Assert.Single(Descendants<ScrollContentPresenter>(scroller),
            presenter => ReferenceEquals(presenter.TemplatedParent, scroller));
        var finalCharacter = text.ContentEnd.GetCharacterRect(LogicalDirection.Backward);
        Assert.False(finalCharacter.IsEmpty);
        var lastBounds = text.TransformToAncestor(viewport).TransformBounds(finalCharacter);
        AssertContained(lastBounds, new Rect(viewport.RenderSize));
    }

    private static void AssertUnobscured(InvisibleHost host, FrameworkElement element)
    {
        var bounds = BoundsIn(element, host.Root);
        AssertContained(bounds, new Rect(host.Root.RenderSize));
        var hit = host.Root.InputHitTest(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2))
            as DependencyObject;
        Assert.NotNull(hit);
        Assert.True(ReferenceEquals(hit, element) || Ancestors(hit).Contains(element),
            $"{element.GetType().Name} is covered by another element ({hit.GetType().Name}).");
    }

    private static Rect BoundsIn(FrameworkElement element, Visual ancestor) =>
        element.TransformToAncestor(ancestor).TransformBounds(new Rect(element.RenderSize));

    private static void AssertContained(Rect content, Rect viewport)
    {
        const double roundingTolerance = 1.5;
        // TextPointer.GetCharacterRect can return a zero-width caret rectangle.
        Assert.True(content.Width >= 0 && content.Height > 0, $"Content has no visible area: {content}.");
        Assert.True(content.Left >= viewport.Left - roundingTolerance &&
                    content.Top >= viewport.Top - roundingTolerance &&
                    content.Right <= viewport.Right + roundingTolerance &&
                    content.Bottom <= viewport.Bottom + roundingTolerance,
            $"Content bounds {content} are clipped by viewport {viewport}.");
    }

    private static IEnumerable<DependencyObject> Ancestors(DependencyObject element)
    {
        for (var parent = GetParent(element); parent != null; parent = GetParent(parent))
        {
            yield return parent;
        }
    }

    private static DependencyObject? GetParent(DependencyObject element) => element is FrameworkContentElement content
        ? content.Parent
        : VisualTreeHelper.GetParent(element);

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T match)
        {
            yield return match;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(parent, index)))
            {
                yield return child;
            }
        }
    }

    public sealed class NotificationState : INotifyPropertyChanged
    {
        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasError)));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public string SuccessMessage { get; set; } = string.Empty;
        public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
        public bool IsFormOpen { get; set; }
        public bool IsAccountPanelOpen { get; set; }
        public bool HasAccount { get; set; }
        public bool HasApartment { get; set; } = true;
        public bool IsLoading { get; set; }
        public string ActionType { get; set; } = "Register";
        public string FormTitle { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public object[] AvailableApartments { get; } = [new { ApartmentNumber = "1002" }];
        private object? _selectedApartment;
        public object? SelectedApartmentInput
        {
            get => _selectedApartment ?? AvailableApartments[0];
            set => _selectedApartment = value;
        }
        public RelationshipType[] RelationshipTypes { get; } = Enum.GetValues<RelationshipType>();
        public RelationshipType SelectedRelationshipType { get; set; } = RelationshipType.Owner;
        public DateTime StartDateInput { get; set; } = new(2026, 9, 10);
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
