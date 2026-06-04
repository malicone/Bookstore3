using Bookstore3.Model;

namespace Bookstore3.Repository;

public enum LookupBookReferenceKind
{
    Group,
    Publisher,
    Language,
    City,
    Shop
}

public static class LookupBookReference
{
    public static LookupBookReferenceKind? TryGetKind(Type lookupEntityType) =>
        lookupEntityType switch
        {
            Type t when t == typeof(group) => LookupBookReferenceKind.Group,
            Type t when t == typeof(publisher) => LookupBookReferenceKind.Publisher,
            Type t when t == typeof(language) => LookupBookReferenceKind.Language,
            Type t when t == typeof(city) => LookupBookReferenceKind.City,
            Type t when t == typeof(shop) => LookupBookReferenceKind.Shop,
            _ => null
        };

    public static string GetColumnName(LookupBookReferenceKind kind) =>
        kind switch
        {
            LookupBookReferenceKind.Group => "group_id",
            LookupBookReferenceKind.Publisher => "publisher_id",
            LookupBookReferenceKind.Language => "language_id",
            LookupBookReferenceKind.City => "city_id",
            LookupBookReferenceKind.Shop => "shop_id",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    public static string GetDisplayName(LookupBookReferenceKind kind) =>
        kind switch
        {
            LookupBookReferenceKind.Group => "group",
            LookupBookReferenceKind.Publisher => "publisher",
            LookupBookReferenceKind.Language => "language",
            LookupBookReferenceKind.City => "city",
            LookupBookReferenceKind.Shop => "shop",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
}
