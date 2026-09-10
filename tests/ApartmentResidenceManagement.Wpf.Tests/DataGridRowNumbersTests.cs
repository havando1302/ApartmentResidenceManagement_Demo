using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ApartmentResidenceManagement.Wpf.Behaviors;
using ApartmentResidenceManagement.Wpf.Views;
using Xunit;

namespace ApartmentResidenceManagement.Wpf.Tests;

[Collection(WpfTestCollection.Name)]
public sealed class DataGridRowNumbersTests(WpfDispatcherFixture fixture)
{
    [Fact]
    public void Scrolling_recycles_rows_and_keeps_displayed_numbers_beyond_ten_thousand()
    {
        fixture.Run(() =>
        {
            var grid = CreateGrid(CreateItems(12_050));
            using var host = new InvisibleHost(grid);
            var previouslySeen = new Dictionary<DataGridRow, object>();
            var reusedContainer = false;

            foreach (var index in new[] { 0, 400, 10_020, 12_049, 50, 650, 0 })
            {
                ScrollTo(host, grid, index);
                AssertDisplayedNumbers(grid);
                Assert.NotNull(grid.ItemContainerGenerator.ContainerFromIndex(index));

                foreach (var row in RealizedRows(grid))
                {
                    if (previouslySeen.TryGetValue(row, out var oldItem) &&
                        !ReferenceEquals(oldItem, row.Item))
                    {
                        reusedContainer = true;
                    }

                    previouslySeen[row] = row.Item;
                }
            }

            Assert.True(reusedContainer, "The test must exercise reused row containers.");
        });
    }

    [Fact]
    public void Sorting_and_filtering_number_the_current_view_from_one()
    {
        fixture.Run(() =>
        {
            var items = CreateItems(240);
            var grid = CreateGrid(items);
            using var host = new InvisibleHost(grid);
            ScrollTo(host, grid, 120);

            var view = CollectionViewSource.GetDefaultView(items);
            view.SortDescriptions.Add(new SortDescription(nameof(RowItem.Id), ListSortDirection.Descending));
            host.Settle();
            AssertDisplayedNumbers(grid);
            ScrollTo(host, grid, 0);
            Assert.Equal(239, ((RowItem)grid.Items[0]).Id);
            AssertDisplayedNumbers(grid);

            view.Filter = item => ((RowItem)item).Id % 3 == 0;
            host.Settle();
            Assert.Equal(80, grid.Items.Count);
            AssertDisplayedNumbers(grid);
            ScrollTo(host, grid, 60);
            AssertDisplayedNumbers(grid);

            view.Filter = null;
            view.SortDescriptions.Clear();
            ScrollTo(host, grid, 0);
            Assert.Equal(0, ((RowItem)grid.Items[0]).Id);
            AssertDisplayedNumbers(grid);
        });
    }

    [Fact]
    public void Collection_changes_and_source_replacement_refresh_existing_cells()
    {
        fixture.Run(() =>
        {
            var items = CreateItems(240);
            var grid = CreateGrid(items);
            using var host = new InvisibleHost(grid);
            ScrollTo(host, grid, 100);

            items.Insert(0, new RowItem(-1));
            host.Settle();
            AssertDisplayedNumbers(grid);

            items.RemoveAt(0);
            host.Settle();
            AssertDisplayedNumbers(grid);

            items.Move(105, 2);
            host.Settle();
            AssertDisplayedNumbers(grid);

            items.Clear();
            host.Settle();
            Assert.Empty(RealizedRows(grid));
            foreach (var item in CreateItems(180))
            {
                items.Add(item);
            }

            ScrollTo(host, grid, 0);
            AssertDisplayedNumbers(grid);

            grid.ItemsSource = CreateItems(320);
            ScrollTo(host, grid, 270);
            AssertDisplayedNumbers(grid);
            ScrollTo(host, grid, 0);
            AssertDisplayedNumbers(grid);
        });
    }

    [Fact]
    public void Enabling_after_load_and_reloading_the_grid_refreshes_numbers()
    {
        fixture.Run(() =>
        {
            var items = CreateItems(200);
            var grid = CreateGrid(items, enabled: false);
            using var host = new InvisibleHost(grid);
            Assert.True(grid.IsLoaded);
            Assert.NotEmpty(RealizedRows(grid));
            Assert.All(RealizedRows(grid), row => Assert.Null(DataGridRowNumbers.GetRowNumber(row)));

            DataGridRowNumbers.SetIsEnabled(grid, true);
            host.Settle();
            AssertDisplayedNumbers(grid);

            host.Root.Children.Remove(grid);
            host.Settle();
            Assert.False(grid.IsLoaded);
            items.Insert(0, new RowItem(-1));
            host.Root.Children.Add(grid);
            host.Settle();
            Assert.True(grid.IsLoaded);
            AssertDisplayedNumbers(grid);
            ScrollTo(host, grid, 150);
            AssertDisplayedNumbers(grid);
        });
    }

    [Theory]
    [InlineData(typeof(ApartmentView))]
    [InlineData(typeof(ResidentView))]
    [InlineData(typeof(ResidencyView))]
    [InlineData(typeof(VehicleView))]
    [InlineData(typeof(FamilyMembersView))]
    [InlineData(typeof(ResidencyHistoryView))]
    [InlineData(typeof(MyVehiclesView))]
    public void Every_table_binds_its_visible_number_cells_to_the_shared_behavior(Type viewType)
    {
        fixture.Run(() =>
        {
            var view = (UserControl)Activator.CreateInstance(viewType)!;
            view.DataContext = new { HasApartment = true };
            using var host = new InvisibleHost(view);
            var grid = Assert.Single(Descendants<DataGrid>(view));
            Assert.True(DataGridRowNumbers.GetIsEnabled(grid));
            Assert.True(grid.EnableRowVirtualization);
            grid.ItemsSource = CreateItems(240);

            foreach (var index in new[] { 0, 190, 40, 0 })
            {
                ScrollTo(host, grid, index);
                AssertDisplayedNumbers(grid);
                Assert.NotNull(grid.ItemContainerGenerator.ContainerFromIndex(index));
            }
        });
    }

    private static ObservableCollection<RowItem> CreateItems(int count) =>
        new(Enumerable.Range(0, count).Select(id => new RowItem(id)));

    private static DataGrid CreateGrid(ObservableCollection<RowItem> items, bool enabled = true)
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding
        {
            Path = new PropertyPath("(0)", DataGridRowNumbers.RowNumberProperty),
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1)
        });

        var grid = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            IsReadOnly = true,
            RowHeight = 24,
            EnableRowVirtualization = true,
            Columns =
            {
                new DataGridTemplateColumn { Header = "#", Width = 60, CellTemplate = new DataTemplate { VisualTree = text } },
                new DataGridTextColumn { Header = "Id", Width = 100, Binding = new Binding(nameof(RowItem.Id)) }
            }
        };
        VirtualizingPanel.SetIsVirtualizing(grid, true);
        VirtualizingPanel.SetVirtualizationMode(grid, VirtualizationMode.Recycling);
        ScrollViewer.SetCanContentScroll(grid, true);
        DataGridRowNumbers.SetIsEnabled(grid, enabled);
        return grid;
    }

    private static void ScrollTo(InvisibleHost host, DataGrid grid, int index)
    {
        host.Settle();
        grid.ScrollIntoView(grid.Items[index]);
        host.Settle();
    }

    private static void AssertDisplayedNumbers(DataGrid grid)
    {
        var rows = RealizedRows(grid);
        Assert.InRange(rows.Length, 1, 100);
        Assert.True(rows.Length < grid.Items.Count, "The grid must keep virtualization active.");

        foreach (var row in rows)
        {
            var expected = grid.Items.IndexOf(row.Item) + 1;
            Assert.True(expected > 0);
            Assert.Equal(expected, DataGridRowNumbers.GetRowNumber(row));
            var cell = grid.Columns[0].GetCellContent(row);
            Assert.NotNull(cell);
            var text = Assert.Single(Descendants<TextBlock>(cell));
            Assert.Equal(expected.ToString(CultureInfo.InvariantCulture), text.Text);
        }
    }

    private static DataGridRow[] RealizedRows(DataGrid grid) =>
        Descendants<DataGridRow>(grid).Where(row => row.GetIndex() >= 0).ToArray();

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

    private sealed record RowItem(int Id);

}
