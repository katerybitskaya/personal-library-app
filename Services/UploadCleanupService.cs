using PersonalLibrary.Interfaces;
using PersonalLibrary.Models;
using PersonalLibrary.Repositories;

namespace PersonalLibrary.Services
{
    public class UploadCleanupService
    {
        private const string UploadsPrefix = "/uploads/";

        private readonly ILibraryRepository _libraryRepository;
        private readonly JsonTrashRepository _trashRepository;
        private readonly IWebHostEnvironment _env;

        public UploadCleanupService(ILibraryRepository libraryRepository, JsonTrashRepository trashRepository, IWebHostEnvironment env)
        {
            _libraryRepository = libraryRepository;
            _trashRepository = trashRepository;
            _env = env;
        }

        public static IEnumerable<string?> ImagePaths(Author author) =>
            new[] { author.PhotoPath }
                .Concat(author.Books.Select(b => b.CoverPath))
                .Concat(author.Series.SelectMany(ImagePaths));

        public static IEnumerable<string?> ImagePaths(Series series) =>
            new[] { series.CoverPath }.Concat(series.Books.Select(b => b.CoverPath));

        public void DeleteIfUnused(IEnumerable<string?> paths)
        {
            var candidates = paths
                .Where(p => !string.IsNullOrEmpty(p) && p!.StartsWith(UploadsPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(p => p!)
                .Distinct()
                .ToList();
            if (candidates.Count == 0) return;

            var used = _libraryRepository.GetAllAuthors().SelectMany(ImagePaths)
                .Where(p => p != null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var trash = _trashRepository.GetAll();

            foreach (var path in candidates)
            {
                if (used.Contains(path) || trash.Any(t => t.SerializedData.Contains(path, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var fileName = Path.GetFileName(path);
                var fullPath = Path.Combine(_env.WebRootPath, "uploads", fileName);
                try
                {
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
