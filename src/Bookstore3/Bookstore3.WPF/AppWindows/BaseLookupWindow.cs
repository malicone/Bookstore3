using Bookstore3.Model.Abstract;
using Bookstore3.Repository;
using Bookstore3.WPF.Options;
using Bookstore3.WPF.Utils;
using KpzRepository.Repository;
using MahApps.Metro.IconPacks;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using Syncfusion.UI.Xaml.ScrollAxis;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Bookstore3.WPF.AppWindows;

public class BaseLookupWindow<TLookupEntity> : Window, IOptionsSavable where TLookupEntity : lookup_entity, new()
{
    public BaseLookupWindow(
        IBookstoreRepositoryFactory repositoryFactory,
        string title = "List of Records")
    {
        _repositoryFactory = repositoryFactory;
        _lookupRepository = repositoryFactory.GetBaseRepository<long, TLookupEntity>();
        _appOptionRepository = repositoryFactory.GetAppOptionRepository();
        _bookRepository = repositoryFactory.GetBookRepository();
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

    public long? LastCreatedRecordId { get; private set; }

    public bool SaveOptions()
    {
        if (_dataGrid is null)
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
        LastCreatedRecordId = null;
        LoadData();
        ApplyDataGridOptionsFromDatabase();
        UpdateStatusBar();
        FocusDataGrid();
    }

    private void FocusDataGrid()
    {
        if (_dataGrid is null)
            return;

        _dataGrid.Dispatcher.BeginInvoke(() =>
        {
            SelectFirstRecord();
            _dataGrid.Focus();
        }, DispatcherPriority.Loaded);
    }

    private void SelectFirstRecord()
    {
        if (_items is null || _items.Count == 0)
            return;

        SelectRecord(_items[0]);
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
        return WindowOptionsPersistence.TryApply(
            _appOptionRepository,
            this,
            GetFullOptionName,
            MinWidth,
            MinHeight);
    }

    private bool ApplyDataGridOptionsFromDatabase()
    {
        if (_dataGrid is null)
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

            _lookupRepository?.Add(newRecord);
            LastCreatedRecordId = newRecord.id > 0
                ? newRecord.id
                : _lookupRepository?.GetMaxId();
            LoadData();
            if (LastCreatedRecordId is > 0)
                SelectRecordById(LastCreatedRecordId.Value);
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
            var recordId = selected.id;
            selected.name = dialog.Answer;
            _lookupRepository?.Update(selected);
            LoadData();
            SelectRecordById(recordId);
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

        var referenceKind = LookupBookReference.TryGetKind(typeof(TLookupEntity));
        var referencingBookCount = 0;
        if (referenceKind is LookupBookReferenceKind kind)
            referencingBookCount = _bookRepository.CountBooksReferencingLookup(kind, selected.id);

        if (ConfirmDelete(selected.name, referenceKind, referencingBookCount) == false)
            return;

        if (referencingBookCount > 0 && referenceKind is LookupBookReferenceKind referenceKindToClear)
        {
            _bookRepository.ClearBookLookupReferences(
                referenceKindToClear,
                selected.id,
                AppConstants.UndefinedRecordId);
        }

        _lookupRepository?.Delete(selected.id);
        LoadData();
        UpdateStatusBar();
        AppUtils.ShowInfoMessage("Record deleted successfully.");
    }

    private static bool ConfirmDelete(
        string? recordName,
        LookupBookReferenceKind? referenceKind,
        int referencingBookCount)
    {
        var displayName = string.IsNullOrWhiteSpace(recordName) ? "this record" : recordName.Trim();
        string message;

        if (referencingBookCount > 0 && referenceKind is LookupBookReferenceKind kind)
        {
            var lookupLabel = LookupBookReference.GetDisplayName(kind);
            message =
                $"Delete record '{displayName}'?{Environment.NewLine}{Environment.NewLine}" +
                $"{referencingBookCount} book(s) reference this {lookupLabel}. " +
                $"If you continue, the record will be deleted and those books will have their {lookupLabel} set to undefined.";
        }
        else
        {
            message = $"Delete record '{displayName}'?";
        }

        return AppUtils.ShowConfirmMessage(message);
    }

    private void RefreshDataButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        LoadData();
        UpdateStatusBar();
    }

    private TLookupEntity? GetSelectedRecord() => _dataGrid?.SelectedItem as TLookupEntity;

    private void LoadData()
    {
        if (_lookupRepository is null || _dataGrid is null)
            return;

        var records = _lookupRepository.GetAll().ToList();
        _items = new ObservableCollection<TLookupEntity>(records);
        _dataGrid.ItemsSource = _items;
    }

    private void SelectRecordById(long recordId)
    {
        if (_dataGrid is null || _items is null || recordId <= 0)
            return;

        var record = _items.FirstOrDefault(item => item.id == recordId);
        if (record is not null)
            SelectRecord(record);
    }

    private void SelectRecord(TLookupEntity record)
    {
        if (_dataGrid is null)
            return;

        _dataGrid.SelectedItem = record;

        var rowIndex = _dataGrid.ResolveToRowIndex(record);
        if (rowIndex < 0)
            return;

        var columnIndex = _dataGrid.ResolveToStartColumnIndex();
        _dataGrid.ScrollInView(new RowColumnIndex(rowIndex, columnIndex));
        _dataGrid.View?.MoveCurrentTo(record);
    }

    private void UpdateStatusBar()
    {
        if (_lookupRepository is null || _countText is null)
            return;

        _countText.Text = string.Format("Count {0}", _lookupRepository.Count());
    }

    private string GetFullOptionName(string optionName) => $"{_OptionsPrefix}.{typeof(TLookupEntity).Name}.{optionName}";

    private readonly IBookstoreRepositoryFactory _repositoryFactory;
    private readonly IKpzRepository<long, TLookupEntity>? _lookupRepository;
    private readonly IAppOptionRepository _appOptionRepository;
    private readonly IBookRepository _bookRepository;
    private SfDataGrid? _dataGrid;
    private TextBlock? _countText;
    private ObservableCollection<TLookupEntity>? _items;

    private const string _OptionsPrefix = "LookupWindow";
    private const string _DataGridOptionName = "DataGrid";
}