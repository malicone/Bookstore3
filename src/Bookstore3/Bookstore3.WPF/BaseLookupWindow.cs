using Bookstore3.Model.Abstract;
using Bookstore3.Repository;
using KpzRepository.Repository;
using MahApps.Metro.IconPacks;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Bookstore3.WPF;

public class BaseLookupWindow<TLookupEntity> : Window, IOptionsSavable where TLookupEntity : lookup_entity, new()
{
    public BaseLookupWindow(
        IKpzRepository<long, TLookupEntity>? repository,
        IAppOptionRepository? appOptionRepository,
        string title = "List of Records")
    {
        Repository = repository;
        _appOptionRepository = appOptionRepository;
        Title = title;
        Width = 600;
        Height = 800;
        MinWidth = 400;
        MinHeight = 300;
        WindowStyle = WindowStyle.ToolWindow;
        ResizeMode = ResizeMode.CanResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        BuildUi();
        RegisterToolbarShortcuts();

        try
        {
            ApplyWindowOptionsFromDatabase();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"An error occurred while loading window options: {ex.Message}");
        }

        Loaded += BaseLookupWindow_LoadedHandler;
        Closing += BaseLookupWindow_ClosingHandler;
    }

    public bool SaveOptions()
    {
        if (_appOptionRepository is null || _dataGrid is null)
            return false;

        var result = WindowOptionsPersistence.Save(_appOptionRepository, this, GetFullOptionName);
        if (SfDataGridOptionsPersistence.Save(
                _appOptionRepository,
                GetFullOptionName(_DataGridOptionName),
                _dataGrid) == false)
            result = false;

        return result;
    }

    public bool LoadOptions()
    {
        var windowLoaded = ApplyWindowOptionsFromDatabase();
        var gridLoaded = ApplyDataGridOptionsFromDatabase();
        return windowLoaded || gridLoaded;
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
        toolbar.Items.Add(CreateToolbarButton("New Record", PackIconMaterialKind.Plus, Key.N, NewRecordButton_ClickHandler));
        toolbar.Items.Add(CreateToolbarButton("Edit Record", PackIconMaterialKind.Pencil, Key.E, EditRecordButton_ClickHandler));
        toolbar.Items.Add(CreateToolbarButton("Delete Record", PackIconMaterialKind.Delete, Key.D, DeleteRecordButton_ClickHandler));
        toolbar.Items.Add(new Separator());
        toolbar.Items.Add(CreateToolbarButton("Refresh Data", PackIconMaterialKind.Refresh, Key.R, RefreshDataButton_ClickHandler));
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

    private void RegisterToolbarShortcuts()
    {
        ToolbarShortcutHelper.Register(this, Key.N, NewRecordButton_ClickHandler);
        ToolbarShortcutHelper.Register(this, Key.E, EditRecordButton_ClickHandler);
        ToolbarShortcutHelper.Register(this, Key.D, DeleteRecordButton_ClickHandler);
        ToolbarShortcutHelper.Register(this, Key.R, RefreshDataButton_ClickHandler);
    }

    private static Button CreateToolbarButton(
        string text,
        PackIconMaterialKind iconKind,
        Key shortcutKey,
        RoutedEventHandler? clickHandler = null)
    {
        var button = new Button
        {
            Height = 36,
            MinWidth = 110,
            Margin = new Thickness(2, 0, 2, 0),
            Padding = new Thickness(12, 6, 12, 6),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            ToolTip = ToolbarShortcutHelper.FormatToolTip(text, shortcutKey)
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
        ApplyDataGridOptionsFromDatabase();
        UpdateStatusBar();
    }

    private void BaseLookupWindow_ClosingHandler(object? sender, CancelEventArgs e)
    {
        try
        {
            SaveOptions();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Failed to save options: {ex.Message}");
        }
    }

    private bool ApplyWindowOptionsFromDatabase()
    {
        if (_appOptionRepository is null)
            return false;

        return WindowOptionsPersistence.TryApply(
            _appOptionRepository,
            this,
            GetFullOptionName,
            MinWidth,
            MinHeight);
    }

    private bool ApplyDataGridOptionsFromDatabase()
    {
        if (_appOptionRepository is null || _dataGrid is null)
            return false;

        return SfDataGridOptionsPersistence.TryLoad(
            _appOptionRepository,
            GetFullOptionName(_DataGridOptionName),
            _dataGrid);
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

    private string GetFullOptionName(string optionName) => $"{_OptionsPrefix}.{typeof(TLookupEntity).Name}.{optionName}";

    protected readonly IKpzRepository<long, TLookupEntity>? Repository;

    private readonly IAppOptionRepository? _appOptionRepository;
    private SfDataGrid? _dataGrid;
    private TextBlock? _countText;
    private ObservableCollection<TLookupEntity>? _items;

    private const string _OptionsPrefix = "LookupWindow";
    private const string _DataGridOptionName = "DataGrid";
}