using System.Text.Json;

namespace PersonalLibrary.Repositories
{
    public class StorageException : Exception
    {
        public StorageException(string message, Exception? inner = null) : base(message, inner) { }
    }

    public static class JsonFileStore
    {
        public static T Load<T>(string path, JsonSerializerOptions options) where T : class, new()
        {
            var backupPath = path + ".bak";
            if (!File.Exists(path) && !File.Exists(backupPath))
                return new T();

            Exception? mainError = null;
            if (File.Exists(path))
            {
                var (data, error) = TryRead<T>(path, options);
                if (data != null) return data;
                mainError = error;
            }

            if (File.Exists(backupPath))
            {
                var (data, error) = TryRead<T>(backupPath, options);
                if (data != null)
                {
                    if (File.Exists(path))
                        File.Move(path, path + ".corrupt", overwrite: true);
                    return data;
                }
                throw new StorageException(
                    $"Cannot read {Path.GetFileName(path)} or {Path.GetFileName(backupPath)}: {mainError?.Message ?? "file is missing"} / {error?.Message}",
                    mainError ?? error);
            }

            throw new StorageException($"Cannot read {Path.GetFileName(path)} and no backup exists: {mainError?.Message}", mainError);
        }

        public static void Save<T>(string path, T data, JsonSerializerOptions options)
        {
            var tempPath = path + ".tmp";
            var json = JsonSerializer.Serialize(data, options);

            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(path))
                File.Replace(tempPath, path, path + ".bak");
            else
                File.Move(tempPath, path);
        }

        private static (T? Data, Exception? Error) TryRead<T>(string path, JsonSerializerOptions options) where T : class
        {
            try
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<T>(json, options);
                return data == null ? (null, new InvalidDataException("file is empty")) : (data, null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        }
    }
}
