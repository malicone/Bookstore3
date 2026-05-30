using Bookstore3.Model.Abstract;
using KpzRepository.Repository;
using MahApps.Metro.IconPacks;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Bookstore3.WPF;

public class BaseLookupWindow<TLookupEntity> : Window where TLookupEntity : lookup_entity, new()
{
    public BaseLookupWindow(IKpzRepository<long, TLookupEntity>? repository, string title = "List of Records")
    {
        Repository = repository;
        Title = title;
        Width = 600;
        Height = 800;
        ResizeMode = ResizeMode.CanResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        BuildUi();

        Loaded += BaseLookupWindow_LoadedHandler;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeWindowStyles.DisableMinimizeAndMaximizeButtons(this);
    }

    private static readonly SolidColorBrush GridLineBrush = AppConstants.GridCellBorderBrush;

    private void BuildUi()
    {
        var gridCellStyle = new Style(typeof(GridCell));
        gridCellStyle.Setters.Add(new Setter(Border.BorderBrushProperty, GridLineBrush));
        Resources.Add(typeof(GridCell), gridCellStyle);

        var root = new DockPanel { LastChildFill = true };

        var toolbarBorder = new Border
        {
            Background = SystemColors.ControlBrush,
            BorderBrush = SystemColors.ControlDarkBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(4, 6, 4, 6)
        };
        DockPanel.SetDock(toolbarBorder, Dock.Top);

        var toolbar = new ToolBar { Background = Brushes.Transparent };
        toolbar.Items.Add(CreateToolbarButton("New Record", PackIconMaterialKind.Plus, NewRecordButton_ClickHandler));
        toolbar.Items.Add(CreateToolbarButton("Edit Record", PackIconMaterialKind.Pencil, EditRecordButton_ClickHandler));
        toolbar.Items.Add(CreateToolbarButton("Delete Record", PackIconMaterialKind.Delete, DeleteRecordButton_ClickHandler));
        toolbar.Items.Add(new Separator());
        toolbar.Items.Add(CreateToolbarButton("Refresh Data", PackIconMaterialKind.Refresh, RefreshDataButton_ClickHandler));
        toolbarBorder.Child = toolbar;

        var statusBar = new Border
        {
            Background = SystemColors.ControlBrush,
            BorderBrush = SystemColors.ControlDarkBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(4, 2, 4, 2)
        };
        DockPanel.SetDock(statusBar, Dock.Bottom);

        _countText = new TextBlock
        {
            Text = "Count 0",
            VerticalAlignment = VerticalAlignment.Center
        };
        statusBar.Child = _countText;

        _dataGrid = new SfDataGrid
        {
            AutoGenerateColumns = false,
            AllowSorting = true,
            AllowTriStateSorting = true,
            AllowFiltering = true,
            LiveDataUpdateMode = LiveDataUpdateMode.AllowDataShaping,
            AllowDraggingColumns = true,
            AllowResizingColumns = true,
            ColumnSizer = GridLengthUnitType.Star,
            GridLinesVisibility = GridLinesVisibility.Both,
            HeaderLinesVisibility = GridLinesVisibility.Both,
            HeaderRowHeight = 26,
            RowHeight = 24,
            SelectionMode = GridSelectionMode.Single,
            SelectionUnit = GridSelectionUnit.Row,
            Margin = new Thickness(8)
        };

        _dataGrid.Columns.Add(new GridTextColumn
        {
            MappingName = "id",
            HeaderText = "Id",
            TextAlignment = TextAlignment.Right,
            Width = 80
        });
        _dataGrid.Columns.Add(new GridTextColumn
        {
            MappingName = "name",
            HeaderText = "Name",
            Width = 400
        });

        root.Children.Add(toolbarBorder);
        root.Children.Add(statusBar);
        root.Children.Add(_dataGrid);

        Content = root;
    }

    private static Button CreateToolbarButton(string text, PackIconMaterialKind iconKind, RoutedEventHandler? clickHandler = null)
    {
        var button = new Button
        {
            Height = 36,
            MinWidth = 110,
            Margin = new Thickness(2, 0, 2, 0),
            Padding = new Thickness(12, 6, 12, 6),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(new PackIconMaterial
        {
            Kind = iconKind,
            Width = 18,
            Height = 18,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        content.Children.Add(new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center
        });
        button.Content = content;

        if (clickHandler is not null)
            button.Click += clickHandler;

        return button;
    }

    private void BaseLookupWindow_LoadedHandler(object sender, RoutedEventArgs e)
    {
        LoadData();
        UpdateStatusBar();
    }

    private void NewRecordButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        InputTextDialog dialog = new InputTextDialog("Enter name:", string.Empty);

        dialog.Owner = this;

        if (dialog.ShowDialog() == true)
        {
            var newRecord = new TLookupEntity
            {
                name = dialog.Answer
            };

            Repository?.Add(newRecord);
            LoadData();
            UpdateStatusBar();
            AppUtils.ShowInfoMessage("Record added successfully.");
        }
    }

    private void EditRecordButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedRecord();
        if (selected is null)
        {
            AppUtils.ShowInfoMessage("Please select a record to edit.");
            return;
        }

        var dialog = new InputTextDialog("Enter name:", selected.name ?? string.Empty)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            selected.name = dialog.Answer;
            Repository?.Update(selected);
            LoadData();
            UpdateStatusBar();
            AppUtils.ShowInfoMessage("Record updated successfully.");
        }
    }

    private void DeleteRecordButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedRecord();
        if (selected is null)
        {
            AppUtils.ShowInfoMessage("Please select a record to delete.");
            return;
        }

        var confirmResult = MessageBox.Show(
            $"Delete record '{selected.name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmResult != MessageBoxResult.Yes)
            return;

        Repository?.Delete(selected.id);
        LoadData();
        UpdateStatusBar();
        AppUtils.ShowInfoMessage("Record deleted successfully.");
    }

    private void RefreshDataButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        LoadData();
        UpdateStatusBar();
    }

    private TLookupEntity? GetSelectedRecord() => _dataGrid?.SelectedItem as TLookupEntity;

    private void LoadData()
    {
        if (Repository is null || _dataGrid is null)
            return;

        var records = Repository.GetAll().ToList();
        _items = new ObservableCollection<TLookupEntity>(records);
        _dataGrid.ItemsSource = _items;
    }

    private void UpdateStatusBar()
    {
        if (Repository is null || _countText is null)
            return;

        _countText.Text = string.Format("Count {0}", Repository.Count());
    }

    protected readonly IKpzRepository<long, TLookupEntity>? Repository;

    private SfDataGrid? _dataGrid;
    private TextBlock? _countText;
    private ObservableCollection<TLookupEntity>? _items;
}