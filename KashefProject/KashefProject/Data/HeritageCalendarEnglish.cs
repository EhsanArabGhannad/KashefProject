using KashefProject.Models;

namespace KashefProject.Data;

// Translation only: recurrence rules and source URLs remain in the source-backed catalogue.
public static class HeritageCalendarEnglish
{
    private sealed record Entry(string Title, string Description, string Note = "");
    private const string NameDayNote = "This is a traditional name-day festival. Dates differ between Zoroastrian calendars and communities. Choose the display convention above; this page is not a religious-calendar conversion.";
    private static readonly IReadOnlyDictionary<string, Entry> Entries = new Dictionary<string, Entry>
    {
        ["nowruz"] = new("Nowruz", "The start of the solar year and a celebration of nature’s renewal; an enduring Iranian tradition.", "The calendar shows Nowruz day, not the exact astronomical time of the equinox."),
        ["great-nowruz"] = new("Great Nowruz — Day of Hope", "Historical sources describe the sixth day of Farvardin as Great Nowruz and the Day of Hope."),
        ["zoroaster-birthday"] = new("Traditional birthday of Zoroaster", "A traditional Zoroastrian commemoration. Zoroaster’s actual historical birth date is unknown.", "6 Farvardin is a ritual date, not a proven historical date of birth."),
        ["sizdah-bedar"] = new("Sizdah Bedar", "Gathering outdoors at the end of the Nowruz festivities. The precise origins of this custom remain debated."),
        ["farvardigan"] = new("Farvardigan", "A commemoration of the fravashis and the departed.", "Distinct from the year-end Fravardigan observance."),
        ["ordibeheshtgan"] = new("Ordibeheshtgan", "The Ordibehesht name-day festival, honoring truth and fire."),
        ["khordadgan"] = new("Khordadgan", "The Khordad name-day festival, associated with water and wholeness."),
        ["tirgan"] = new("Tirgan", "A festival of Tir and water, linked to the story of Arash the Archer.", "Arash’s story is mythological, not a historical event with a proven anniversary date."),
        ["amordadgan"] = new("Amordadgan", "The Amordad name-day festival, honoring plants and life."),
        ["shahrivargan"] = new("Shahrivargan", "The Shahrivar name-day festival, associated with ideal kingship."),
        ["mehrgan"] = new("Mehrgan", "A major Iranian festival celebrating Mehr, friendship and the keeping of promises."),
        ["abangan"] = new("Abangan", "The Aban name-day festival, honoring the waters."),
        ["azargan"] = new("Azargan", "The Azar name-day festival, honoring fire."),
        ["yalda"] = new("Yalda Night — Chella", "The last night of autumn: gatherings, stories and poetry, with symbols of light and warmth.", "The observance begins on the evening of 30 Azar. This page does not calculate the astronomical winter solstice."),
        ["deygan-1"] = new("Deygan — Ohrmazd Day", "The first Dey festival, honoring the Creator."),
        ["deygan-8"] = new("Deygan — Dey-be-Azar", "The Dey-be-Azar festival in the month of Dey."),
        ["deygan-15"] = new("Deygan — Dey-be-Mehr", "The Dey-be-Mehr festival in the month of Dey."),
        ["deygan-23"] = new("Deygan — Dey-be-Din", "The Dey-be-Din festival in the month of Dey."),
        ["bahmangan"] = new("Bahmangan", "The Bahman name-day festival, honoring good thought."),
        ["sadeh"] = new("Sadeh", "A winter celebration of fire and community, part of the shared heritage of Iran and Tajikistan.", "This page uses 10 Bahman in the solar calendar. Its Gregorian equivalent varies by year."),
        ["sepandarmazgan"] = new("Sepandarmazgan", "A festival of Spandarmad, honoring the earth and women."),
        ["chaharshanbe-suri"] = new("Chaharshanbe Suri", "A fire celebration on the evening before the last Wednesday of the year, with diverse local customs.", "There is no fixed month-day date; this observance is calculated separately for each year."),
        ["maidyozarem"] = new("Maidyozarem Gahambar", "A seasonal mid-spring festival."),
        ["maidyoshahem"] = new("Maidyoshahem Gahambar", "A seasonal midsummer festival."),
        ["paitishahem"] = new("Paitishahem Gahambar", "A seasonal harvest festival."),
        ["ayathrem"] = new("Ayathrem Gahambar", "A seasonal festival of the returning herds."),
        ["maidyarem"] = new("Maidyarem Gahambar", "A seasonal midwinter festival."),
        ["hamaspathmaidyem"] = new("Hamaspathmaidyem — Year-end Gahambar", "A year-end festival and commemoration of the fravashis.", "Displayed on the last five days of the solar year. The placement of additional days differs between religious calendars."),
        ["reza-shah-birthday"] = new("Birthday of Reza Shah Pahlavi", "The birthday anniversary of the founder of the Pahlavi dynasty, observed by the court on 24 Esfand."),
        ["reza-shah-coronation"] = new("Coronation of Reza Shah", "The anniversary of Reza Shah’s coronation at Golestan Palace on 25 April 1926."),
        ["mohammad-reza-accession"] = new("Accession of Mohammad Reza Shah", "The transfer of the throne following Reza Shah’s abdication on 16 September 1941, during the occupation of Iran."),
        ["farah-birthday"] = new("Birthday of Empress Farah Pahlavi", "The anniversary of Farah Diba’s birth in Tehran on 14 October 1938."),
        ["mohammad-reza-birthday"] = new("Birthday of Mohammad Reza Shah", "Mohammad Reza Shah’s birthday anniversary, observed by the court on 4 Aban."),
        ["imperial-coronation"] = new("Coronation of the Shah and Empress", "The anniversary of Mohammad Reza Shah and Empress Farah’s coronation at Golestan Palace on 4 Aban 2526 Imperial (26 October 1967)."),
        ["reza-pahlavi-birthday"] = new("Birthday of Reza Pahlavi", "The anniversary of the former Crown Prince of Iran’s birth on 31 October 1960."),
        ["royal-wedding"] = new("Wedding anniversary of the Shah and Farah Diba", "The anniversary of Mohammad Reza Shah and Farah Diba’s wedding in Tehran on 21 December 1959."),
        ["mohammad-reza-memorial"] = new("Memorial anniversary of Mohammad Reza Shah", "The anniversary of Mohammad Reza Shah’s death in Egypt on 27 July 1980."),
        ["cyrus-day"] = new("Cyrus the Great Day", "An unofficial, contemporary commemoration of Cyrus, symbolically linked to accounts of his entry into Babylon.", "7 Aban is a modern commemoration, not an ancient festival or an official UNESCO day. Conventions for converting ancient dates differ.")
    };

    public static HeritageEvent Translate(HeritageEvent source)
    {
        // A missing translation is a catalogue error, not a silent mixed-language fallback.
        var entry = Entries[source.Id];
        var note = source.Rule == HeritageDateRule.NameDay
            ? NameDayNote + (entry.Note.Length > 0 ? " " + entry.Note : "") : entry.Note;
        var title = source.SourceUrl switch
        {
            var url when url.Contains("iranicaonline.org", StringComparison.Ordinal) => "Encyclopaedia Iranica",
            var url when url.Contains("rcc1937.pdf", StringComparison.Ordinal) => "J. J. Modi — Religious Customs of the Parsees",
            var url when url.Contains("avesta.org", StringComparison.Ordinal) => "Avesta — Zoroastrian calendar",
            var url when url.Contains("unesco.org", StringComparison.Ordinal) => "UNESCO — Yalda / Chella",
            var url when url.Contains("farahpahlavifoundation.org", StringComparison.Ordinal) => "Farah Pahlavi Foundation — Biography",
            var url when url.Contains("farahpahlavi.org", StringComparison.Ordinal) => "Farah Pahlavi — Official website",
            _ => "Iranian Studies Center, Tel Aviv University"
        };
        return source with { Title = entry.Title, Description = entry.Description, Note = note, SourceTitle = title };
    }
}
