using System.ComponentModel.DataAnnotations;

namespace KashefProject.Models;

public sealed class CheckoutViewModel
{
    [Required, StringLength(4096)] public string ReviewToken { get; set; } = "";
    [Required, StringLength(120), Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Phone, StringLength(30)] public string? Phone { get; set; }
    [Required, StringLength(160), Display(Name = "Street address")] public string AddressLine1 { get; set; } = "";
    [StringLength(160), Display(Name = "Apartment, suite, etc. (optional)")] public string? AddressLine2 { get; set; }
    [Required, StringLength(80)] public string City { get; set; } = "";
    [Required, StringLength(2)] public string State { get; set; } = "";
    [Required, RegularExpression("^[0-9]{5}(-[0-9]{4})?$", ErrorMessage = "Enter a valid US ZIP code."), Display(Name = "ZIP code")]
    public string PostalCode { get; set; } = "";
    [Range(typeof(bool), "true", "true", ErrorMessage = "Please confirm that you reviewed your order and delivery details.")]
    public bool AcknowledgePending { get; set; }
    public CartSummary Summary { get; set; } = new([]);
}

public static class UsStates
{
    public static IReadOnlyDictionary<string, string> All { get; } = new Dictionary<string, string>
    {
        ["AL"]="Alabama", ["AK"]="Alaska", ["AZ"]="Arizona", ["AR"]="Arkansas", ["CA"]="California",
        ["CO"]="Colorado", ["CT"]="Connecticut", ["DE"]="Delaware", ["DC"]="District of Columbia", ["FL"]="Florida",
        ["GA"]="Georgia", ["HI"]="Hawaii", ["ID"]="Idaho", ["IL"]="Illinois", ["IN"]="Indiana", ["IA"]="Iowa",
        ["KS"]="Kansas", ["KY"]="Kentucky", ["LA"]="Louisiana", ["ME"]="Maine", ["MD"]="Maryland",
        ["MA"]="Massachusetts", ["MI"]="Michigan", ["MN"]="Minnesota", ["MS"]="Mississippi", ["MO"]="Missouri",
        ["MT"]="Montana", ["NE"]="Nebraska", ["NV"]="Nevada", ["NH"]="New Hampshire", ["NJ"]="New Jersey",
        ["NM"]="New Mexico", ["NY"]="New York", ["NC"]="North Carolina", ["ND"]="North Dakota", ["OH"]="Ohio",
        ["OK"]="Oklahoma", ["OR"]="Oregon", ["PA"]="Pennsylvania", ["RI"]="Rhode Island", ["SC"]="South Carolina",
        ["SD"]="South Dakota", ["TN"]="Tennessee", ["TX"]="Texas", ["UT"]="Utah", ["VT"]="Vermont",
        ["VA"]="Virginia", ["WA"]="Washington", ["WV"]="West Virginia", ["WI"]="Wisconsin", ["WY"]="Wyoming"
    };
}
