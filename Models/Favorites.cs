namespace PersonalLibrary.Models
{
    public static class Favorites
    {
        public static void Migrate(Author author)
        {
            Move(author.FavoriteBy, () => author.LegacyIsFavorite, v => author.LegacyIsFavorite = v);
            author.Books.ForEach(Migrate);
            author.Series.ForEach(Migrate);
        }

        public static void Migrate(Series series)
        {
            Move(series.FavoriteBy, () => series.LegacyIsFavorite, v => series.LegacyIsFavorite = v);
            series.Books.ForEach(Migrate);
        }

        public static void Migrate(Book book) =>
            Move(book.FavoriteBy, () => book.LegacyIsFavorite, v => book.LegacyIsFavorite = v);

        public static void Set(List<string> favoriteBy, string userId, bool isFavorite)
        {
            favoriteBy.RemoveAll(id => id == userId);
            if (isFavorite) favoriteBy.Add(userId);
        }

        private static void Move(List<string> favoriteBy, Func<bool> legacy, Action<bool> reset)
        {
            if (!legacy()) return;
            if (!favoriteBy.Contains(AppUser.AdminId)) favoriteBy.Add(AppUser.AdminId);
            reset(false);
        }
    }
}
