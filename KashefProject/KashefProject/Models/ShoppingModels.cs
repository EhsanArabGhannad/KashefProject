using System.Globalization;

namespace KashefProject.Models;

public static class StoreMoney
{
    public static string Format(long cents) => (cents / 100m).ToString("C", CultureInfo.GetCultureInfo("en-US"));
}

public sealed record CartLine(int ProductId, string Name, string Slug, string? ImagePath,
    string Finish, string Size, int Quantity, long? UnitPriceCents, bool Available)
{
    public long LineTotalCents => checked((UnitPriceCents ?? 0) * Quantity);
}

public sealed record CartSummary(IReadOnlyList<CartLine> Lines)
{
    public int Count => Lines.Sum(line => line.Quantity);
    public long SubtotalCents => Lines.Sum(line => line.Available ? line.LineTotalCents : 0);
    public bool CanCheckout => Lines.Count > 0 && Lines.All(line => line.Available);
}
