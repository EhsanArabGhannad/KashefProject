using System.ComponentModel.DataAnnotations;
using KashefProject.Models;

namespace KashefProject.Data;

public sealed class CalendarEvent
{
    public int Id { get; set; }
    [MaxLength(80)] public string Key { get; set; } = "";
    [MaxLength(180)] public string TitleFa { get; set; } = "";
    [MaxLength(180)] public string TitleEn { get; set; } = "";
    [MaxLength(4000)] public string DescriptionFa { get; set; } = "";
    [MaxLength(4000)] public string DescriptionEn { get; set; } = "";
    [MaxLength(2000)] public string NoteFa { get; set; } = "";
    [MaxLength(2000)] public string NoteEn { get; set; } = "";
    [MaxLength(24)] public string Category { get; set; } = "ancient";
    public HeritageDateRule Rule { get; set; }
    public int Month { get; set; } = 1;
    public int Day { get; set; } = 1;
    public int Duration { get; set; } = 1;
    public int? HistoricalYear { get; set; }
    public int? OnlyImperialYear { get; set; }
    [MaxLength(500)] public string SourceTitle { get; set; } = "";
    [MaxLength(2000)] public string SourceUrl { get; set; } = "";
    [MaxLength(200)] public string? ImagePath { get; set; }
    public bool IsPublished { get; set; } = true;
    [ConcurrencyCheck, MaxLength(32)] public string Revision { get; set; } = Guid.NewGuid().ToString("N");

    public HeritageEvent ToDefinition() => new(Key, TitleFa, Category, DescriptionFa, Rule, Month, Day,
        SourceTitle, SourceUrl, NoteFa, Duration, HistoricalYear, TitleEn, DescriptionEn, NoteEn, ImagePath, OnlyImperialYear);
}

// A durable import marker prevents deleted built-in events being recreated on restart.
public sealed class CalendarCatalogState
{
    public int Id { get; set; }
}
