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
- **❤️ Favourites** - Heart authors, series and books and see them on one page
- **🚩 Flags & Notes** - Mark books (duplicate, for sale, lent out, signed…) with an optional note; mark ongoing series
- **📕 Not Owned** - Mark a series book you don't have yet; it can only carry the “out of print” flag and can't be a favourite
- **⚡ No Page Reloads** - Changes (flags, favourites, adding or deleting) update the page in place
- **🔒 Optional Password Protection** - Turn login on or off with a single config file, no code changes

## Technologies Used

### Backend
- **ASP.NET Core 8.0** - Web framework
- **C# 12** - Programming language with nullable reference types
- **Entity Composition** - Custom service architecture without Entity Framework

### Frontend
- **Razor Views** - Server-side templating
- **HTML5 & CSS3** - Responsive UI with dark theme
- **JavaScript** - Client-side interactions (modals, toasts, dynamic updates)

### Data & Patterns
- **JSON** - Data storage format (library.json, trash.json, history.json)
- **Design Patterns**:
  - **Composite Pattern** - Hierarchical structure (Author > Series > Book)
  - **Observer Pattern** - Event-driven history tracking
  - **Iterator Pattern** - Advanced search functionality

### Localization
- **LocalizationService** - Built-in dictionaries for English, Russian, Polish

## Project Structure

```
PersonalLibrary/
├── Controllers/           # MVC Controllers
│   ├── HomeController     # Catalog & language switching
│   ├── AccountController  # Login / logout (optional protection)
│   ├── AuthorController   # Author CRUD operations
│   ├── BookController     # Book CRUD operations
│   ├── SeriesController   # Series CRUD operations
│   ├── TrashController    # Trash bin management
│   ├── HistoryController  # Activity history
│   ├── MissingBooksController  # Missing books tracking
│   ├── FavoritesController  # Favourite authors, series and books
│   └── FlaggedController  # Flagged books and ongoing series
├── Models/                # Domain models
│   ├── Book              # Book entity with IsMissing flag
│   ├── Author            # Author entity
│   ├── Series            # Book series with ordering
│   ├── HistoryEntry      # Activity log entries
│   ├── TrashItem         # Deleted item recovery data
│   └── ProtectionSettings # Password protection options
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
├── Security/              # Optional password protection
│   ├── ProtectionMiddleware   # Blocks all pages and files for anonymous users
│   ├── LoginAttemptTracker    # Temporary lockout after failed logins
│   ├── ProtectionHelper       # Credential check, client IP detection
│   └── PasswordTool           # --hash-password console command
├── Interfaces/            # Service contracts
├── Middleware/            # Data storage guard, request lock
├── Helpers/               # Utility classes (file upload)
├── wwwroot/               # Static files (CSS, JS, uploads)
├── Data/                  # JSON data storage (auto-generated)
└── protection.example.json # Template for protection.json
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
   The application will start at `http://localhost:5000` (default Kestrel port).
   To use another port: `dotnet run --urls "http://localhost:5050"`.

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

## Password Protection (optional)

By default the library is open to everyone. To require a login, create a `protection.json` file next to the application (in the project folder when using `dotnet run`, or next to `PersonalLibrary.dll` after `dotnet publish`):

1. Copy the template:
   ```bash
   cp protection.example.json protection.json
   ```
2. Generate a password hash (you choose the password, the tool only hashes it):
   ```bash
   dotnet run -- --hash-password
   # or, for a published build:
   dotnet PersonalLibrary.dll --hash-password
   ```
3. Put the result into `PasswordHash`, set your `Username` and `"Enabled": true`:
   ```json
   {
     "Protection": {
       "Enabled": true,
       "Username": "admin",
       "PasswordHash": "AQAAAAIAAYagAAAAE...",
       "Password": "",
       "MaxFailedAttempts": 5,
       "GlobalMaxFailedAttempts": 20,
       "LockoutMinutes": 15,
       "SessionDays": 30
     }
   }
   ```

| Setting | Meaning |
|---------|---------|
| `Enabled` | `true` — every page and uploaded image requires login; `false` or no file — open access |
| `Username` | Login name |
| `PasswordHash` | Hash from `--hash-password` (recommended) |
| `Password` | Plain-text password, used only when `PasswordHash` is empty (not recommended) |
| `MaxFailedAttempts` | Failed logins from one IP before a temporary lockout |
| `GlobalMaxFailedAttempts` | Failed logins from all IPs together before the login form is locked |
| `LockoutMinutes` | Lockout duration; afterwards login is possible again |
| `SessionDays` | How long "Remember me" keeps you signed in |


## Architecture Highlights

### Design Patterns

**Composite Pattern**
- Hierarchical structure: Authors contain Series, Series contain Books
- Unified interface through `ILibraryComponent`

**Observer Pattern**
- `LibraryEventPublisher` notifies subscribers of library changes
- `HistoryService` automatically logs all modifications

**Iterator Pattern**
- `AuthorIterator`, `SeriesIterator`, `BookIterator` for traversing the library
- `LibrarySearchIterator` for advanced search functionality

### Dependency Injection

Services are configured in `Program.cs` using ASP.NET Core DI container:
- Singleton repositories and services
- Scoped localization service

## Data Storage

All data is stored in JSON format in the `Data` folder:

- **library.json** - Authors, books, and series definitions (incl. flags, notes, “not owned”, favourites, ongoing series)
- **trash.json** - Deleted items with recovery metadata
- **history.json** - Complete activity log with timestamps
- **\*.json.bak** - Previous version of each file, used automatically if the main file can't be read
- **keys/** - Data Protection keys for login and anti-forgery cookies (keeps sessions valid after a restart)

> The `Data/` folder is excluded from the repository (`.gitignore`) since it contains personal library data. It is created automatically on first run.

## Localization

The application supports three languages. The switcher is in the navigation bar on every page (and on the login page when protection is enabled):

- 🇬🇧 **English**
- 🇷🇺 **Russian**
- 🇵🇱 **Polish**

## Key Features Details

### Book Series Management
- Add books to series with automatic ordering
- Missing book tracking (“Not owned” switch)
- Smart series positioning

### History Tracking
- Automatic logging of all operations
- Timestamps and operation details
- Observer pattern implementation
- Pagination: 30 entries per page, newest first

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
- `POST /Home/SetLanguage` - Change interface language (available without login)

### Authors
- `GET /Author/Details/{id}` - Author details
- `POST /Author/Add` - Add author
- `POST /Author/UpdatePhoto` - Update photo
- `POST /Author/Delete` - Delete author

### Books
- `GET /Book/Details/{id}` - Book details
- `POST /Book/Add` - Add book
- `POST /Book/Rename` - Rename book
- `POST /Book/UpdateCover` - Update cover
- `POST /Book/Delete` - Delete book
- `POST /Book/UpdateFlags` - Set book flags, note and “not owned”

### Series
- `GET /Series/Details/{id}` - Series details
- `POST /Series/Add` - Create series
- `POST /Series/Rename` - Rename series
- `POST /Series/UpdateCover` - Update cover
- `POST /Series/AddBook` - Add book to series
- `POST /Series/DeleteBook` - Delete book from series
- `POST /Series/Delete` - Delete series
- `POST /Series/SetOngoing` - Mark series as ongoing

### Trash
- `GET /Trash/Index` - View trash
- `POST /Trash/Restore` - Restore item
- `POST /Trash/DeleteForever` - Permanently delete
- `POST /Trash/Clear` - Clear trash

### Account (only when protection is enabled)
- `GET /Account/Login` - Login page
- `POST /Account/Login` - Sign in
- `POST /Account/Logout` - Sign out

### History
- `GET /History/Index?page={n}` - View activity history (30 entries per page)
- `POST /History/Clear` - Clear history

### Missing Books
- `GET /MissingBooks/Index` - View missing books

### Favourites
- `GET /Favorites/Index` - View favourites
- `POST /Favorites/Toggle` - Add or remove a favourite

### Flagged
- `GET /Flagged/Index` - View flagged books and ongoing series

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
- CSS in `wwwroot/css/site.css` (including the login page styles)
- JavaScript utilities in `wwwroot/js/site.js`

## Error Handling

- Centralized exception handling in middleware
- User-friendly error pages
- If the data files can't be read, a maintenance page is shown and saving is disabled
- Development exception details in Development environment
- HSTS enabled for production

## Future Enhancements

- Database migration from JSON to SQL Server / PostgreSQL
- RESTful API for mobile clients
- Advanced filtering and sorting
- Export/Import functionality
- Multi-user accounts with roles
- Book ratings and reviews

## License

This project is developed for educational purposes.

---

**Built with ❤️ using ASP.NET Core 8.0**
