namespace PersonalLibrary.Helpers
{
    public static class FileUploadHelper
    {
        private static readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

        public static bool IsRejected(IFormFile? file) =>
            file != null && file.Length > 0 && !_allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());

        public static async Task<string?> SaveAsync(IFormFile? file, string? manualPath, IWebHostEnvironment env)
        {
            if (file != null && file.Length > 0)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(ext))
                    return null; 

                var uploadDir = Path.Combine(env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadDir);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(uploadDir, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                return $"/uploads/{fileName}"; 
            }

            if (!string.IsNullOrWhiteSpace(manualPath))
                return manualPath.Trim();

            return null;
        }
    }
}
