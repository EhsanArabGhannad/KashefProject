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
    IReadOnlyList<string> Images,
    string ImageAlt,
    string Badge,
    string CardClass,
    IReadOnlyList<string> Highlights)
{
    public string ImagePath => Images[0];
}

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
            "calligraphy",
            "Calligraphy",
            "Dimensional script",
            "Persian lettering and royal signatures interpreted as warm, tactile reliefs for contemporary interiors.",
            "/images/products/collection/golden-calligraphy-03.jpg",
            "Gold Persian calligraphy wall panel in a bright modern room",
            "category-card--calligraphy"),
        new(
            "portraits",
            "Portraits",
            "Icons & figures",
            "Minimal portrait panels celebrating cultural figures through clean line, restrained color, and dimensional texture.",
            "/images/products/collection/shahbanu-portrait-01.jpg",
            "Gold Shahbanu portrait wall panel in a modern living room",
            "category-card--portraits"),
        new(
            "heritage",
            "Heritage Symbols",
            "Symbols with a story",
            "Architectural and historic emblems reworked as bold statement pieces with a modern graphic presence.",
            "/images/products/collection/lion-sun-01.jpg",
            "Gold Lion and Sun heritage wall panel",
            "category-card--heritage")
    ];

    public static IReadOnlyList<Product> Products { get; } =
    [
        new(
            "golden-calligraphy-panel",
            "Golden Calligraphy Panel",
            "calligraphy",
            "Calligraphy",
            "Price on request",
            "Gold & ivory · framed relief",
            "A dimensional Persian calligraphy composition in warm gold, presented against a softly textured ivory field and styled in a slim metallic frame.",
            "Custom sizing available",
            "Dimensional 3D-printed relief",
            "Textured ivory with metallic gold",
            [
                "/images/products/collection/golden-calligraphy-01.jpg",
                "/images/products/collection/golden-calligraphy-02.jpg",
                "/images/products/collection/golden-calligraphy-03.jpg",
                "/images/products/collection/golden-calligraphy-04.jpg",
                "/images/products/collection/golden-calligraphy-05.jpg",
                "/images/products/collection/golden-calligraphy-06.jpg"
            ],
            "Gold Persian calligraphy wall panel in a modern interior",
            "FEATURED",
            "product-card--cream",
            ["Six gallery views", "Framed and ready to hang", "Custom sizing available"]),
        new(
            "royal-signature-panel",
            "Royal Signature Panel",
            "calligraphy",
            "Calligraphy",
            "Price on request",
            "Gold on ivory · portrait format",
            "A refined signature study with a dimensional gold inscription and a subtle handwritten detail, designed as an elegant vertical focal point.",
            "Custom sizing available",
            "Dimensional 3D-printed relief",
            "Metallic gold on warm ivory",
            ["/images/products/collection/royal-signature-01.jpg"],
            "Gold royal signature wall panel in a quiet seating area",
            "SIGNATURE SERIES",
            "product-card--lavender",
            ["Portrait orientation", "Framed and ready to hang", "Made to order"]),
        new(
            "royal-calligraphy-panel",
            "Royal Calligraphy Panel",
            "calligraphy",
            "Calligraphy",
            "Price on request",
            "Gold & ivory · vertical relief",
            "A tall, expressive calligraphic composition that balances fine linear detail with a warm metallic finish.",
            "Custom sizing available",
            "Dimensional 3D-printed relief",
            "Metallic gold on textured ivory",
            [
                "/images/products/collection/royal-calligraphy-01.jpg",
                "/images/products/collection/royal-calligraphy-02.jpg",
                "/images/products/collection/royal-calligraphy-03.jpg",
                "/images/products/collection/royal-calligraphy-04.jpg"
            ],
            "Vertical gold Persian calligraphy panel in a modern interior",
            "COLLECTOR EDIT",
            "product-card--orange",
            ["Four gallery views", "Vertical format", "Custom frame options"]),
        new(
            "shahbanu-portrait",
            "Shahbanu Portrait",
            "portraits",
            "Portraits",
            "Price on request",
            "Gold line portrait · framed panel",
            "A minimalist line portrait of the Shahbanu, paired with Persian lettering and a warm gold-on-ivory palette.",
            "Custom sizing available",
            "Dimensional 3D-printed relief",
            "Satin gold on warm ivory",
            ["/images/products/collection/shahbanu-portrait-01.jpg"],
            "Gold Shahbanu portrait wall panel",
            "FEATURED",
            "product-card--cream",
            ["Portrait orientation", "Lightweight framed panel", "Made to order"]),
        new(
            "shahyad-tower-panel",
            "Shahyad Tower Panel",
            "heritage",
            "Heritage Symbols",
            "Price on request",
            "Architectural gold relief · portrait format",
            "A graphic tribute to the Shahyad Tower, combining Persian lettering with a simplified architectural silhouette in dimensional gold.",
            "Custom sizing available",
            "Dimensional 3D-printed relief",
            "Antique gold on warm ivory",
            [
                "/images/products/collection/shahyad-tower-01.jpg",
                "/images/products/collection/shahyad-tower-02.jpg"
            ],
            "Gold Shahyad Tower wall panel in a modern interior",
            "HERITAGE EDIT",
            "product-card--lavender",
            ["Two gallery views", "Architectural motif", "Framed and ready to hang"]),
        new(
            "lion-and-sun-panel",
            "Lion & Sun Panel",
            "heritage",
            "Heritage Symbols",
            "Price on request",
            "Gold on charcoal · landscape format",
            "A bold Lion and Sun emblem rendered as a crisp gold relief against a deep charcoal field for maximum graphic contrast.",
            "Custom sizing available",
            "Dimensional 3D-printed relief",
            "Warm gold on charcoal black",
            [
                "/images/products/collection/lion-sun-01.jpg",
                "/images/products/collection/lion-sun-02.jpg"
            ],
            "Gold Lion and Sun wall panel on a charcoal background",
            "FEATURED",
            "product-card--orange",
            ["Two gallery views", "Landscape format", "Statement-scale option"]),
        new(
            "reza-shah-portrait",
            "Reza Shah Portrait",
            "portraits",
            "Portraits",
            "Price on request",
            "Gold line portrait · framed panel",
            "A strong profile portrait of Reza Shah, reduced to clean gold shapes and paired with Persian lettering on a warm ivory ground.",
            "Custom sizing available",
            "Dimensional 3D-printed relief",
            "Satin gold on warm ivory",
            ["/images/products/collection/reza-shah-portrait-01.jpg"],
            "Gold Reza Shah portrait wall panel",
            "PORTRAIT SERIES",
            "product-card--cream",
            ["Portrait orientation", "Framed and ready to hang", "Made to order"]),
        new(
            "ataturk-portrait",
            "Atatürk Portrait",
            "portraits",
            "Portraits",
            "Price on request",
            "Gold line portrait · framed panel",
            "A restrained profile of Mustafa Kemal Atatürk, interpreted through a warm gold line and minimal typography.",
            "Custom sizing available",
            "Dimensional 3D-printed relief",
            "Muted gold on warm ivory",
            ["/images/products/collection/ataturk-portrait-01.jpg"],
            "Gold Atatürk portrait wall panel in a calm interior",
            "PORTRAIT SERIES",
            "product-card--lavender",
            ["Portrait orientation", "Lightweight framed panel", "Made to order"])
    ];

    public static IReadOnlyList<Product> FeaturedProducts =>
        Products.Where(product => product.Badge == "FEATURED").ToArray();

    public static Product? FindProduct(string slug) =>
        Products.FirstOrDefault(product => string.Equals(product.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public static ProductCategory? FindCategory(string slug) =>
        Categories.FirstOrDefault(category => string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
