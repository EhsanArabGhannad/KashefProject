using System.ComponentModel.DataAnnotations;
using KashefProject.Models;
using KashefProject.Services;

namespace KashefProject.Areas.Admin.Models;

public sealed class CalendarEventEditorViewModel : IValidatableObject
{
    public int Id { get; set; }
    [MaxLength(32)] public string? Revision { get; set; }
    [Required, StringLength(180)] public string TitleFa { get; set; } = "";
    [Required, StringLength(180)] public string TitleEn { get; set; } = "";
    [Required, StringLength(4000)] public string DescriptionFa { get; set; } = "";
    [Required, StringLength(4000)] public string DescriptionEn { get; set; } = "";
    [StringLength(2000)] public string? NoteFa { get; set; }
    [StringLength(2000)] public string? NoteEn { get; set; }
    [Required] public string Category { get; set; } = "ancient";
    public HeritageDateRule Rule { get; set; }
    [Range(1, 12)] public int Month { get; set; } = 1;
    [Range(1, 31)] public int Day { get; set; } = 1;
    [Range(1, 31)] public int Duration { get; set; } = 1;
    [Range(1, 9998)] public int? HistoricalYear { get; set; }
    [Range(HeritageCalendarService.MinYear, HeritageCalendarService.MaxYear)] public int? OnlyImperialYear { get; set; }
    public bool IsPublished { get; set; } = true;
    public IFormFile? Image { get; set; }
    public bool RemoveImage { get; set; }
    // Populated from the database, never accepted as an upload path.
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public string? ExistingImagePath { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Category is not ("ancient" or "royal" or "memorial"))
            yield return new("Choose a valid category.", [nameof(Category)]);
        if (!Enum.IsDefined(Rule))
            yield return new("Choose a valid date rule.", [nameof(Rule)]);
        if (Month is >= 1 and <= 12 && Day is >= 1 and <= 31)
        {
            var max = Rule switch
            {
                HeritageDateRule.Solar => Month <= 6 ? 31 : 30,
                HeritageDateRule.NameDay => 30,
                HeritageDateRule.Gregorian => DateTime.DaysInMonth(2000, Month),
                _ => 31
            };
            if (Day > max) yield return new("This day does not exist in the selected month/date system.", [nameof(Day)]);
            if (Rule == HeritageDateRule.Solar && OnlyImperialYear.HasValue &&
                OnlyImperialYear is >= HeritageCalendarService.MinYear and <= HeritageCalendarService.MaxYear &&
                Day > new System.Globalization.PersianCalendar().GetDaysInMonth(HeritageCalendarText.SolarHijriYear(OnlyImperialYear.Value), Month))
                yield return new("This Imperial year is not a leap year; choose a valid day.", [nameof(Day)]);
            if (Rule == HeritageDateRule.Gregorian && Month == 2 && Day == 29 &&
                OnlyImperialYear is >= HeritageCalendarService.MinYear and <= HeritageCalendarService.MaxYear &&
                !DateTime.IsLeapYear(HeritageCalendarService.SolarDate(HeritageCalendarText.SolarHijriYear(OnlyImperialYear.Value) + 1, 1, 1).Year))
                yield return new("29 February does not occur in the selected Imperial year.", [nameof(Day)]);
        }
    }
}
