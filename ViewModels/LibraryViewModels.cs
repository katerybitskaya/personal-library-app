using PersonalLibrary.Models;
using PersonalLibrary.Services;

namespace PersonalLibrary.ViewModels
{
    public class CatalogueViewModel
    {
        public Dictionary<char, List<Author>> AuthorsByLetter { get; set; } = new();

        public List<char> ExistingLetters { get; set; } = new();

        public SearchResult? SearchResult { get; set; }

        public string SearchQuery { get; set; } = string.Empty;

        public int AuthorCount { get; set; }

        public int SeriesCount { get; set; }

        public int BookCount { get; set; }
    }

    public class AuthorViewModel
    {
        public Author Author { get; set; } = null!;
    }

    public class BookViewModel
    {
        public Book Book { get; set; } = null!;
        public Author Author { get; set; } = null!;

        public Series? ParentSeries { get; set; }
    }

    public class SeriesViewModel
    {
        public Series Series { get; set; } = null!;
        public Author Author { get; set; } = null!;
    }

    public class TrashViewModel
    {
        public List<TrashItem> Items { get; set; } = new();
    }

    public class HistoryViewModel
    {
        public List<HistoryEntry> Entries { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 30;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        public int FirstItem => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int LastItem => Math.Min(Page * PageSize, TotalCount);
    }

    public class MissingBooksViewModel
    {
        public List<MissingBookInfo> Items { get; set; } = new();
    }
}
