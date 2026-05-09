using System.ComponentModel.DataAnnotations;

namespace PersonalLibrary.ViewModels
{
    public class AddAuthorForm
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
        public IFormFile? PhotoFile { get; set; }
        public string? PhotoPath { get; set; }
    }

    public class AddBookForm
    {
        [Required]
        public string AuthorId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(500, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        public IFormFile? CoverFile { get; set; }
        public string? CoverPath { get; set; }
    }

    public class AddSeriesForm
    {
        [Required]
        public string AuthorId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Series name is required.")]
        [StringLength(500, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        public List<SeriesPartForm> Parts { get; set; } = new();
    }

    public class SeriesPartForm
    {
        [Required(ErrorMessage = "Part title is required.")]
        public string Title { get; set; } = string.Empty;
        public string? CoverPath { get; set; }
    }

    public class AddBookToSeriesForm
    {
        [Required]
        public string SeriesId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(500, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        public int? OrderInSeries { get; set; }

        public IFormFile? CoverFile { get; set; }
        public string? CoverPath { get; set; }
    }

    public class RenameForm
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        public string ItemType { get; set; } = string.Empty;

        [Required(ErrorMessage = "New name is required.")]
        [StringLength(500, MinimumLength = 1)]
        public string NewName { get; set; } = string.Empty;
    }

    public class UpdatePhotoForm
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        public string ItemType { get; set; } = string.Empty;

        public IFormFile? PhotoFile { get; set; }
        public string? Path { get; set; }
    }
}
