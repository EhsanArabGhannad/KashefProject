using System.ComponentModel.DataAnnotations;

namespace KashefProject.Data;

public sealed class CatalogCategory
{
    public int Id { get; set; }

    [MaxLength(64)]
    public required string Slug { get; set; }

    [MaxLength(120)]
    public required string Name { get; set; }

    [MaxLength(120)]
    public required string Kicker { get; set; }

    [MaxLength(800)]
    public required string Description { get; set; }

    [MaxLength(500)]
    public required string ImagePath { get; set; }

    [MaxLength(300)]
    public required string ImageAlt { get; set; }

    [MaxLength(80)]
    public required string CardClass { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public ICollection<CatalogProduct> Products { get; set; } = [];
}

public sealed class CatalogProduct
{
    public int Id { get; set; }

    [MaxLength(64)]
    public required string Slug { get; set; }

    [MaxLength(160)]
    public required string Name { get; set; }

    public int CategoryId { get; set; }
    public CatalogCategory Category { get; set; } = null!;

    public long? PriceCents { get; set; }

    [MaxLength(240)]
    public required string ShortDescription { get; set; }

    [MaxLength(2000)]
    public required string Description { get; set; }

    [MaxLength(160)]
    public required string Size { get; set; }

    [MaxLength(160)]
    public required string Material { get; set; }

    [MaxLength(160)]
    public required string Finish { get; set; }

    [MaxLength(80)]
    public required string Badge { get; set; }

    [MaxLength(80)]
    public required string CardClass { get; set; }

    [MaxLength(1200)]
    public required string HighlightsText { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public ICollection<CatalogProductImage> Images { get; set; } = [];
}

public sealed class CatalogProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public CatalogProduct Product { get; set; } = null!;

    [MaxLength(500)]
    public required string ImagePath { get; set; }

    [MaxLength(300)]
    public required string AltText { get; set; }

    public int SortOrder { get; set; }
}
