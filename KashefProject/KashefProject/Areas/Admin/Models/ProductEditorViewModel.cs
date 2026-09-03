using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KashefProject.Areas.Admin.Models;

public sealed class ProductEditorViewModel
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    [Display(Name = "Product name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(64)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers, and hyphens only.")]
    public string? Slug { get; set; }

    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Range(0.01, 1000000)]
    [Display(Name = "Price (USD)")]
    public decimal? PriceDollars { get; set; }

    [Required, StringLength(240)]
    [Display(Name = "Short description")]
    public string ShortDescription { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Size { get; set; } = "Custom sizing available";

    [Required, StringLength(160)]
    public string Material { get; set; } = "Dimensional 3D-printed relief";

    [Required, StringLength(160)]
    public string Finish { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Badge { get; set; } = "NEW";

    [Required, StringLength(80)]
    [Display(Name = "Card color")]
    public string CardClass { get; set; } = "product-card--cream";

    [Required, StringLength(1200)]
    [Display(Name = "Highlights (one per line)")]
    public string HighlightsText { get; set; } = "Made to order";

    [Display(Name = "Featured on home page")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Visible in the store")]
    public bool IsPublished { get; set; } = true;

    [Range(0, 10000)]
    [Display(Name = "Display order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Add product images")]
    public List<IFormFile> NewImages { get; set; } = [];

    public List<int> RemoveImageIds { get; set; } = [];
    public List<ExistingImageViewModel> ExistingImages { get; set; } = [];
    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
}

public sealed record ExistingImageViewModel(int Id, string ImagePath, string AltText);
