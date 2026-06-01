# Bookstore 3.0

Bookstore 3.0 is a desktop book catalogue for managing a personal or small library. The application lets you browse, search, filter, group, and export books in a data grid; view full details including cover images, annotations, and notes; and maintain lookup tables for groups, publishers, shops, languages, and cities.

The solution is a WPF application backed by a SQLite database. Data access is built on the **KpzRepository.Sqlite** NuGet package, and the UI uses **Syncfusion WPF** controls for rich grid and input functionality.

## Solution structure

| Project | Description |
|---------|-------------|
| `Bookstore3.WPF` | Main WPF application (MainWindow, BookWindow, lookup windows) |
| `Bookstore3.Repository` | Data access layer (repositories, factory) |
| `Bookstore3.Model` | Entity classes mapped to database tables |

Database connection settings are stored in `appsettings.json`:

```json
{
  "DefaultConnectionString": "Data Source=Bookstore3SqliteDb.db;Cache=Shared"
}
```

## KpzRepository.Sqlite

This project uses [KpzRepository.Sqlite](https://www.nuget.org/packages/KpzRepository.Sqlite) for SQLite data access. The package provides a lightweight repository pattern on top of Dapper and Dapper.Contrib, with support for CRUD operations, transactions, and generic repositories for lookup tables.

### Factory and repository setup

`BookstoreRepositoryFactory` extends `KpzRepositorySqliteFactory` and exposes typed repositories:

```csharp
_repositoryFactory = new BookstoreRepositoryFactory(connectionString);
_bookRepository = _repositoryFactory.GetBookRepository();
_groupRepository = _repositoryFactory.GetBaseRepository<long, group>();
_publisherRepository = _repositoryFactory.GetBaseRepository<long, publisher>();
```

### Entity mapping

Entities inherit from `BaseEntity<long>` and use Dapper.Contrib attributes:

```csharp
[Table("books")]
public class book : BaseEntity<long>
{
    [Key]
    public long id { get; set; }
    public string title { get; set; } = null!;
    public string? author { get; set; }
    // ...
}
```

Lookup entities (groups, publishers, shops, languages, cities) share a common base:

```csharp
public abstract class lookup_entity : BaseEntity<long>
{
    [Key]
    public long id { get; set; }
    public string? name { get; set; }
}
```

### CRUD operations

Standard repository methods are used throughout the app:

```csharp
// Read
var book = _bookRepository.Get(bookId);
var groups = _groupRepository.GetAll().ToList();

// Create
var book = new book { title = "Example", crt_date_time = DateTime.Now };
_bookRepository.Add(book);

// Update
book.title = "Updated Title";
_bookRepository.Update(book);

// Delete
_bookRepository.Delete(selected.id);

// Count (used in the status bar)
var totalBooks = _bookRepository.Count();
```

### Custom repository with Dapper queries

`BookRepository` extends `KpzRepository<long, book>` and adds custom queries for the main grid, joining lookup tables in one SQL statement:

```csharp
public class BookRepository : KpzRepository<long, book>, IBookRepository
{
    public IEnumerable<book_ex> GetAllBooksLightweight()
    {
        if (OpenConnection())
        {
            string sql = BuiltSelectAllBooksLightweightQuery();
            return Connection!.Query<book_ex>(sql);
        }
        return Enumerable.Empty<book_ex>();
    }
}
```

### Book search

`SearchBooksLightweight` and `SearchBooksLightweightAsync` search **title** and **author** using a parameterized `LIKE` pattern. Results use the same lightweight `book_ex` shape as the main grid:

```csharp
var results = _bookRepository.SearchBooksLightweight("tolstoy");
var resultsAsync = await _bookRepository.SearchBooksLightweightAsync("war and peace");
```

The SQL applies `%searchText%` matching on title and author and returns rows ordered by book id.

### Lookup table maintenance

Generic repositories power the lookup windows for groups, publishers, shops, languages, and cities:

```csharp
var newRecord = new group { name = "Science Fiction" };
_groupRepository.Add(newRecord);

selected.name = dialog.Answer;
_groupRepository.Update(selected);

_groupRepository.Delete(selected.id);
```

## Syncfusion WPF components

The UI relies on **Syncfusion WPF** packages (`Syncfusion.SfGrid.WPF`, `Syncfusion.SfInput.WPF`, `Syncfusion.Shared.WPF`, `Syncfusion.DataGridExcelExport.WPF`). These components provide a powerful, feature-rich experience out of the box:

- **SfDataGrid** — sorting, multi-column tri-state sorting, filtering, grouping with drag-and-drop group area, column chooser, multi-row selection, table summaries, and **export to Excel, CSV, and PDF** (via `ExportToExcel` / `ExportToPdfGrid` extension methods)
- **SfInput controls** — `SfDatePicker`, `SfDateTimeEdit`, `IntegerTextBox`, and `DoubleTextBox` on the book edit form
- **Column chooser** — hide and show grid columns at runtime

Syncfusion grids handle large datasets efficiently and support advanced scenarios such as grouped views, filtered navigation on the Details tab, and exporting selected rows to common file formats.

## Search

The main window provides a collapsible search bar between the tab area and the status bar.

### Opening and closing

- Click **Search** on the toolbar to show or hide the search bar (hidden at startup).
- Typing printable characters on the **List of All Books** or **Details of Selected Book** tab opens the search bar and routes input to the search box.
- Press **Escape**, click the close (×) button, or click **Search** again to close the bar.
- Closing the search bar reloads the full book list via `LoadBooks()`.

### Running a search

- Press **Enter** in the search box, or click **Run The Search** (play icon).
- The app calls `IBookRepository.SearchBooksLightweight` with the entered text.

### Results

| Tab active | Matches found | No matches |
|------------|---------------|------------|
| **List** | Grid shows only matching books; first result is selected and scrolled into view | Grid is cleared; **Nothing Found** tooltip on the search bar |
| **Details** | Details panel shows the first match (grid updated in the background; tab is not switched) | Details fields are cleared; **Nothing Found** tooltip on the search bar |

Use **Refresh Data** or close the search bar to return to the complete catalogue.

## Export

The **Export** menu on the main window toolbar exports **selected rows** from the book list to Excel (`.xlsx`), CSV (`.csv`), or PDF (`.pdf`).

Before export, a confirmation dialog explains how to select rows:

- **Ctrl+A** — select all visible rows
- **Shift+click** — select a range
- **Ctrl+click** — select individual rows

After confirmation, a save dialog opens with a default file name such as `Books_list_export_yyyy-MM-dd_-_HH-mm-ss`. PDF export uses **landscape** page orientation to fit the wide grid layout.

## Main features

- **Book catalogue** — browse all books in a sortable, filterable, groupable grid; customize visible columns; view row counts and price totals in the grid footer
- **Book details** — cover image, metadata, annotation, and notes on a dedicated tab; First / Previous / Next / Last navigation moves through visible grid rows (respecting filters and groups)
- **Create, edit, delete** — full book editor with cover image, lookup fields, and validation; double-click a grid row to edit
- **Search** — quick text search by title or author (see [Search](#search) above)
- **Export** — selected grid rows to Excel, CSV, or PDF via Syncfusion grid export (see [Export](#export) above)
- **Lookup tables** — maintain groups, publishers, shops, languages, and cities from the Groups menu
- **Status bar** — live totals for books and all lookup tables

## Development

Built with **Cursor AI**, **Visual Studio Insiders 2026** (version 11819.209), **.NET 10**, and **C# 12**.
