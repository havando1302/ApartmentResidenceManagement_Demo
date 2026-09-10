using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace ApartmentResidenceManagement.Wpf.Behaviors;

/// <summary>Numbers rows by their position in the current sorted/filtered view.</summary>
public static class DataGridRowNumbers
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(DataGridRowNumbers),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyPropertyKey RowNumberPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly("RowNumber", typeof(int?), typeof(DataGridRowNumbers),
            new PropertyMetadata(null));

    public static readonly DependencyProperty RowNumberProperty = RowNumberPropertyKey.DependencyProperty;

    private static readonly DependencyProperty TrackerProperty =
        DependencyProperty.RegisterAttached("Tracker", typeof(RowTracker), typeof(DataGridRowNumbers));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static int? GetRowNumber(DependencyObject element) => (int?)element.GetValue(RowNumberProperty);

    private static void OnIsEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not DataGrid grid)
            return;

        if (grid.GetValue(TrackerProperty) is RowTracker previous)
            previous.Dispose();

        grid.ClearValue(TrackerProperty);
        if ((bool)e.NewValue)
            grid.SetValue(TrackerProperty, new RowTracker(grid));
    }

    private sealed class RowTracker : IDisposable
    {
        private readonly DataGrid _grid;
        private readonly HashSet<DataGridRow> _rows = new();
        private DispatcherOperation? _pendingRefresh;
        private bool _isAttached;

        public RowTracker(DataGrid grid)
        {
            _grid = grid;
            _grid.Loaded += OnLoaded;
            _grid.Unloaded += OnUnloaded;
            Attach();
        }

        private void Attach()
        {
            if (_isAttached)
                return;

            _isAttached = true;
            _grid.LoadingRow += OnLoadingRow;
            _grid.UnloadingRow += OnUnloadingRow;
            _grid.ItemContainerGenerator.ItemsChanged += OnItemsChanged;
            _grid.ItemContainerGenerator.StatusChanged += OnGeneratorStatusChanged;
            // A reloaded grid can retain containers without raising LoadingRow again.
            TrackExistingRows(_grid);
            ScheduleRefresh();
        }

        private void TrackExistingRows(DependencyObject parent)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is DataGridRow row)
                {
                    if (ItemsControl.ItemsControlFromItemContainer(row) == _grid)
                        _rows.Add(row);
                }
                else if (child is not DataGrid)
                {
                    TrackExistingRows(child);
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Attach();
            ScheduleRefresh();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Detach();

        private void OnLoadingRow(object? sender, DataGridRowEventArgs e)
        {
            _rows.Add(e.Row);
            UpdateRowNumber(e.Row);
            ScheduleRefresh();
        }

        private void OnUnloadingRow(object? sender, DataGridRowEventArgs e)
        {
            _rows.Remove(e.Row);
            e.Row.ClearValue(RowNumberPropertyKey);
        }

        private void OnItemsChanged(object sender, ItemsChangedEventArgs e) => ScheduleRefresh();

        private void OnGeneratorStatusChanged(object? sender, EventArgs e)
        {
            if (_grid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                ScheduleRefresh();
        }

        private void ScheduleRefresh()
        {
            if (!_isAttached || _pendingRefresh is { Status: DispatcherOperationStatus.Pending })
                return;

            // Wait until WPF finishes remapping recycled containers and changing view indexes.
            _pendingRefresh = _grid.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _pendingRefresh = null;
                foreach (var row in _rows)
                    UpdateRowNumber(row);
            }));
        }

        private static void UpdateRowNumber(DataGridRow row)
        {
            var index = row.GetIndex();
            row.SetValue(RowNumberPropertyKey,
                index >= 0 && row.Item != CollectionView.NewItemPlaceholder ? (int?)(index + 1) : null);
        }

        private void Detach()
        {
            _isAttached = false;
            _grid.LoadingRow -= OnLoadingRow;
            _grid.UnloadingRow -= OnUnloadingRow;
            _grid.ItemContainerGenerator.ItemsChanged -= OnItemsChanged;
            _grid.ItemContainerGenerator.StatusChanged -= OnGeneratorStatusChanged;
            _pendingRefresh?.Abort();
            _pendingRefresh = null;
            foreach (var row in _rows)
                row.ClearValue(RowNumberPropertyKey);
            _rows.Clear();
        }

        public void Dispose()
        {
            _grid.Loaded -= OnLoaded;
            _grid.Unloaded -= OnUnloaded;
            Detach();
        }
    }
}
