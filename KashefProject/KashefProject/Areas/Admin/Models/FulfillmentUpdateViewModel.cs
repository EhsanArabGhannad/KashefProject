using System.ComponentModel.DataAnnotations;
using KashefProject.Data;

namespace KashefProject.Areas.Admin.Models;

public sealed class FulfillmentUpdateViewModel
{
    public FulfillmentStatus FulfillmentStatus { get; set; }

    [StringLength(80)]
    public string? TrackingCarrier { get; set; }

    [StringLength(120)]
    public string? TrackingNumber { get; set; }

    [StringLength(1000)]
    public string? AdminNotes { get; set; }
}
