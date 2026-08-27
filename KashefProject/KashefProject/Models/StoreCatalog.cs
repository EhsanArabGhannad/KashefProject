namespace KashefProject.Models;

public sealed record Product(
    string Slug,
    string Name,
    string CategorySlug,
    string CategoryName,
    string Price,
    string ShortDescription,
    string Description,
    string Size,
    string Material,
    string Finish,
    string ImagePath,
    string ImageAlt,
    string Badge,
    string CardClass,
    IReadOnlyList<string> Highlights);

public sealed record ProductCategory(
    string Slug,
    string Name,
    string Kicker,
    string Description,
    string ImagePath,
    string ImageAlt,
    string CardClass);

public sealed record CategoryPageViewModel(ProductCategory Category, IReadOnlyList<Product> Products);

public static class StoreCatalog
{
    public static IReadOnlyList<ProductCategory> Categories { get; } =
    [
        new(
            "sculptures",
            "Sculptures",
            "Sculptural objects",
            "Statement forms designed to shift the mood of a shelf, table, or room.",
            "/images/products/wave-sculpture.png",
            "Modern matte black 3D-printed sculpture",
            "category-card--sculptures"),
        new(
            "vases",
            "Vases",
            "Functional forms",
            "Geometric vessels with a distinctly digital rhythm and a hand-finished feel.",
            "/images/products/poly-vase.png",
            "Pearl white geometric 3D-printed vase",
            "category-card--vases"),
        new(
            "wall-art",
            "Wall Art",
            "Art for your walls",
            "Layered reliefs that translate traditional pattern into a contemporary object.",
            "/images/products/gol-frame.png",
            "Colorful Persian-inspired 3D-printed relief panel",
            "category-card--wall")
    ];

    public static IReadOnlyList<Product> Products { get; } =
    [
        new(
            "wave-sculpture",
            "Wave Sculpture",
            "sculptures",
            "Sculptures",
            "$68",
            "Matte black · 11 in",
            "A continuous, ribbon-like form that changes character from every angle. Printed slowly for crisp edges and finished by hand for a quiet matte surface.",
            "11 × 8 × 5 in",
            "PLA+ bioplastic",
            "Soft-touch matte black",
            "/images/products/wave-sculpture.png",
            "Matte black Wave sculpture with visible 3D-print texture",
            "NEW",
            "product-card--cream",
            ["Made to order", "Lightweight and durable", "Available in 12+ colors"]),
        new(
            "poly-vase",
            "Poly Vase",
            "vases",
            "Vases",
            "$42",
            "Pearl white · 9.5 in",
            "A faceted vessel built from clean planes and subtle shadows. It works beautifully as a sculptural object or with dried stems.",
            "9.5 × 5.5 × 5.5 in",
            "PLA+ bioplastic",
            "Pearl white satin",
            "/images/products/poly-vase.png",
            "Pearl white geometric Poly Vase",
            "BESTSELLER",
            "product-card--lavender",
            ["Made to order", "For dried botanicals", "Custom colors available"]),
        new(
            "persian-relief-panel",
            "Persian Relief Panel",
            "wall-art",
            "Wall Art",
            "$96",
            "Three-color · 16 × 20 in",
            "A dimensional wall piece inspired by Persian floral geometry, reinterpreted through layered color and precision 3D printing.",
            "16 × 20 × 1.2 in",
            "PLA+ bioplastic",
            "Three-color satin relief",
            "/images/products/gol-frame.png",
            "Three-color Persian-inspired relief panel",
            "LIMITED RUN",
            "product-card--orange",
            ["Numbered small batch", "Ready to hang", "Custom palette available"])
    ];

    public static Product? FindProduct(string slug) =>
        Products.FirstOrDefault(product => string.Equals(product.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public static ProductCategory? FindCategory(string slug) =>
        Categories.FirstOrDefault(category => string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
