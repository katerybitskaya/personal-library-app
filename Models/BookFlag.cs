using System.Text.Json.Serialization;

namespace PersonalLibrary.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BookFlag
    {
        OneOfSeries,
        Duplicate,
        ForSale,
        OutOfPrint,
        Lent,
        Signed,
        PoorCondition,
        OtherEdition
    }

    public static class BookFlags
    {
        public static readonly BookFlag[] All = Enum.GetValues<BookFlag>();

        public static readonly BookFlag[] ForMissing = { BookFlag.OutOfPrint };

        public static readonly BookFlag[] ForRegular = All.Where(f => !ForMissing.Contains(f)).ToArray();

        public static BookFlag[] AllowedFor(Book book) => book.IsMissing ? ForMissing : ForRegular;

        public static string Key(BookFlag flag) => $"Flag_{flag}";

        public static string CssClass(BookFlag flag) => flag switch
        {
            BookFlag.OneOfSeries   => "flag-one-of-series",
            BookFlag.Duplicate     => "flag-duplicate",
            BookFlag.ForSale       => "flag-for-sale",
            BookFlag.OutOfPrint    => "flag-out-of-print",
            BookFlag.Lent          => "flag-lent",
            BookFlag.Signed        => "flag-signed",
            BookFlag.PoorCondition => "flag-poor-condition",
            BookFlag.OtherEdition  => "flag-other-edition",
            _                      => "flag-one-of-series"
        };
    }
}
