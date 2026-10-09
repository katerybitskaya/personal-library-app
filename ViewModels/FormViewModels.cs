using System.ComponentModel.DataAnnotations;
using PersonalLibrary.Models;

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

        [StringLength(500)]
        public string? Title { get; set; }

        public int? OrderInSeries { get; set; }

        public bool IsMissing { get; set; }

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

    public class UpdateFlagsForm
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        public List<BookFlag> Flags { get; set; } = new();

        [StringLength(1000)]
        public string? Note { get; set; }

        public bool? IsMissing { get; set; }
    }

    public class SetFavoriteForm
    {
        [Required]
        public string Kind { get; set; } = string.Empty;

        [Required]
        public string Id { get; set; } = string.Empty;

        public bool IsFavorite { get; set; }
    }

    public class SetOngoingForm
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        public bool IsOngoing { get; set; }
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
