# 📚 Personal Library

A modern web-based library management system built with ASP.NET Core. Organize and manage your personal book collection with features for authors, series, history tracking, and more.

## Overview

Personal Library is a full-stack ASP.NET MVC application that provides a user-friendly interface for managing your personal book collection. It supports multilingual localization, design patterns for scalability, and JSON-based data persistence.

## Features

- **📖 Book Management** - Add, edit, delete books with metadata (author, series, cover images)
- **✍️ Author Management** - Manage authors and their associated books
- **📚 Series Management** - Organize books into series with automatic ordering
- **🔍 Advanced Search** - Find books by title, author, or series using iterator pattern
- **📜 History Tracking** - Automatic tracking of all library modifications with timestamps
- **🗑️ Trash System** - Soft delete with recovery functionality
- **🌍 Multilingual Support** - English, Russian, and Polish localization
- **🖼️ Cover Management** - Upload and display book cover images
- **📊 Missing Books Tracking** - Identify gaps in book series

## Technologies Used

### Backend
- **ASP.NET Core 8.0** - Web framework
- **C# 12** - Programming language with nullable reference types
- **Entity Composition** - Custom service architecture without Entity Framework

### Frontend
- **Razor Views** - Server-side templating
- **HTML5 & CSS3** - Responsive UI with dark theme
- **JavaScript** - Client-side interactions (modals, toasts, dynamic updates)
- **Bootstrap** - Layout and responsive design

### Data & Patterns
- **JSON** - Data storage format (library.json, trash.json, history.json)
- **Design Patterns**:
  - **Composite Pattern** - Hierarchical structure (Author > Series > Book)
  - **Observer Pattern** - Event-driven history tracking
  - **Iterator Pattern** - Advanced search functionality

### Localization
- **IStringLocalizer** - Multi-language support
- **.resx Files** - Resource files for English, Russian, Polish

## Project Structure

```
PersonalLibrary/
├── Controllers/           # MVC Controllers
│   ├── HomeController     # Catalog & language switching
│   ├── AuthorController   # Author CRUD operations
│   ├── BookController     # Book CRUD operations
│   ├── SeriesController   # Series CRUD operations
│   ├── TrashController    # Trash bin management
│   ├── HistoryController  # Activity history
│   └── MissingBooksController  # Missing books tracking
├── Models/                # Domain models
│   ├── Book              # Book entity with IsMissing flag
│   ├── Author            # Author entity
│   ├── Series            # Book series with ordering
│   ├── HistoryEntry      # Activity log entries
│   └── TrashItem         # Deleted item recovery data
├── Views/                 # Razor view templates
├── Services/              # Business logic
│   ├── LibraryService    # Core library operations
│   ├── HistoryService    # History management & observer
│   └── TrashService      # Trash bin operations
├── Repositories/          # Data access layer
│   ├── JsonLibraryRepository
│   ├── JsonTrashRepository
│   └── JsonHistoryRepository
├── Patterns/              # Design pattern implementations
│   ├── LibraryComposite  # Composite pattern
│   ├── LibraryObserver   # Observer pattern
│   └── LibraryIterator   # Iterator pattern
├── Interfaces/            # Service contracts
├── Helpers/               # Utility classes (file upload)
├── Resources/             # Localization (.resx files)
├── Filters/               # Action filters (trash count badge)
├── wwwroot/               # Static files (CSS, JS, uploads)
└── Data/                  # JSON data storage (auto-generated)
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 / Visual Studio Code / JetBrains Rider
- Windows, Linux, or macOS

### Installation

1. **Clone or download the project**
   ```bash
   cd PersonalLibrary
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```
   The application will start at `http://localhost:5103` (default `http` profile from
   `Properties/launchSettings.json`; with `dotnet run --launch-profile https` —
   `https://localhost:7054`).

   To run on a fixed port (used by the Cloudflare Tunnel / Apache proxy):
   ```bash
   dotnet run --urls "http://localhost:5000"
   ```
   In production (VPS) the port is set by `ASPNETCORE_URLS=http://localhost:5000`.

### Initial Setup

1. Open the browser and navigate to the application
2. The `Data` folder with JSON files will be created automatically on first run
3. Start adding authors and books through the web interface

## Configuration

Edit `appsettings.json` to customize:

```json
{
  "LibrarySettings": {
    "DataDirectory": "Data"
  }
}
```

## Architecture Highlights

### Design Patterns

**Composite Pattern**
- Hierarchical structure: Authors contain Series, Series contain Books
- Unified interface through `ILibraryComponent`

**Observer Pattern**
- `LibraryEventPublisher` notifies subscribers of library changes
- `HistoryService` automatically logs all modifications

**Iterator Pattern**
- `LibraryIterator` for traversing composite structures
- `LibrarySearchIterator` for advanced search functionality

### Dependency Injection

Services are configured in `Program.cs` using ASP.NET Core DI container:
- Singleton repositories for JSON data access
- Scoped services for request-specific operations
- Transient filters for trash count tracking

## Data Storage

All data is stored in JSON format in the `Data` folder:

- **library.json** - Authors, books, and series definitions
- **trash.json** - Deleted items with recovery metadata
- **history.json** - Complete activity log with timestamps

> The `Data/` folder is excluded from the repository (`.gitignore`) since it contains personal library data. It is created automatically on first run.

## Localization

The application supports three languages with language switching on the home page:

- 🇬🇧 **English** - SharedResource.en.resx
- 🇷🇺 **Russian** - SharedResource.ru.resx
- 🇵🇱 **Polish** - SharedResource.pl.resx

Language preference is stored in session and persists during the user session.

## Key Features Details

### Book Series Management
- Add books to series with automatic ordering
- Missing book tracking (indicated by "-" title)
- Smart series positioning

### History Tracking
- Automatic logging of all operations
- Timestamps and operation details
- Observer pattern implementation

### Trash System
- Soft delete functionality
- Item recovery with metadata restoration
- Automatic cleanup of recovered items

### Search Functionality
- Search by book title
- Search by author name
- Case-insensitive matching
- Iterator-based implementation

## API Endpoints

### Home
- `GET /` - Library catalog

### Authors
- `GET /Author/Details/{id}` - Author details
- `POST /Author/Create` - Add author
- `POST /Author/Update/{id}` - Update author
- `POST /Author/Delete/{id}` - Delete author

### Books
- `GET /Book/Details/{id}` - Book details
- `POST /Book/Create` - Add book
- `POST /Book/Update/{id}` - Update book
- `POST /Book/Delete/{id}` - Delete book

### Series
- `GET /Series/Details/{id}` - Series details
- `POST /Series/Create` - Create series
- `POST /Series/Update/{id}` - Update series

### Trash
- `GET /Trash/Index` - View trash
- `POST /Trash/Restore/{id}` - Restore item
- `POST /Trash/PermanentDelete/{id}` - Permanently delete

### History
- `GET /History/Index` - View activity history

### Missing Books
- `GET /MissingBooks/Index` - View missing books

## Development

### Build for Production
```bash
dotnet publish -c Release
```

### Run Tests (if applicable)
```bash
dotnet test
```

### Code Structure
- Controllers implement MVC pattern
- Services contain business logic
- Repositories handle data persistence
- Models represent domain entities
- Views use Razor templating

## Styling

The application features:
- Dark theme with gold accents
- Responsive design for mobile and desktop
- CSS in `wwwroot/css/site.css`
- JavaScript utilities in `wwwroot/js/site.js`

## Error Handling

- Centralized exception handling in middleware
- User-friendly error pages
- Development exception details in Development environment
- HSTS enabled for production

## Future Enhancements

- Database migration from JSON to SQL Server / PostgreSQL
- RESTful API for mobile clients
- Advanced filtering and sorting
- Export/Import functionality
- User authentication and multi-user support
- Book ratings and reviews

## License

This project is developed for educational purposes.

---

**Built with ❤️ using ASP.NET Core 8.0**
