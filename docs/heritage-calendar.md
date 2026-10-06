# Iranian heritage calendar

Public route: `/calendar/`. Persian content with an RTL main region; shared store navigation remains English/LTR. The cream, orange, lime, bold-heading and rounded-panel treatment follows the existing storefront.

## Features

- Solar month grid with **imperial (Shahanshahi) year numbering**, Saturday-first week, Persian numerals, Iran-local today.
- Year/month selector (2480–2680 imperial), previous/next month, today shortcut and selected-day details.
- Countdown to the next matching event; ongoing multi-day festivals count as today.
- Ancient festivals, royal anniversaries and modern commemorations as separate filters.
- Whole-year search, including Arabic/Persian kaf and ya normalization.
- Source, historical caveat and recurrence basis on every event. Native expandable month agenda.
- Server-rendered GET navigation: no JavaScript needed for any calendar control.

## Editorial scope and dates

All displayed years and new URL/form year parameters use the imperial era: `imperial = Solar Hijri + 1180` (1405 → 2585). This changes only the year number. Internal .NET `PersianCalendar` calculations continue to use Solar Hijri years, preserving month lengths, leap years and absolute dates. The controller redirects old preview links using 1300–1500 to their imperial equivalents. Current day, countdown, day labels, event details, agenda and search results share the same imperial formatter. This imperial era is distinct from the similarly named Zoroastrian Shahanshahi religious calendar. The editorial explanation links to [Iranica's calendar history](https://www.iranicaonline.org/articles/calendars/calendars-ii-in-the-islamic-period/).

The initial catalogue contains **38 sourced entries**, not an exhaustive record of Iranian history. Unknown ancient day/month dates are not fabricated. Modern Cyrus Day is labeled a contemporary, unofficial commemoration rather than an ancient or UNESCO holiday. Traditional Zoroaster birthday is explicitly not an established historical birth date.

Default `named` convention displays traditional month/day labels on the Solar Hijri grid. It is **not** a religious-calendar conversion. Optional `seasonal` convention counts traditional 30-day months from the selected year's Nowruz. This is a declared presentation convention, not a claim to exactly implement Fasli, Qadimi or Shahanshahi calendars. The page explains this distinction. Year-end five-day festivities use the actual final five Solar Hijri days, including leap years.

Gregorian-based anniversaries are converted afresh each year; historically attested Solar Hijri anniversaries stay fixed. Chaharshanbe Suri is the Tuesday evening before the last Wednesday (not merely the last Tuesday). No astronomical equinox time calculation is claimed.

## Maintaining content

Entries live in `Data/HeritageCalendarCatalog.cs`; each needs a stable unique ID, category, concise original description, explicit date rule, HTTPS source and, when relevant, uncertainty note and original Gregorian historical year. `Duration` supports multi-day festivals. Editing the catalogue requires a new build/release; calendar editing is not currently part of the admin panel. No database migration or customer-data change is needed.

Sources include Encyclopaedia Iranica, UNESCO's Yalda entry, Avesta's calendar reference and Modi's primary account, the Farah Pahlavi foundation/site, and the Tel Aviv University Iranian studies center. Links are visible alongside the events.

## Verification

```powershell
dotnet build KashefProject/KashefProject/KashefProject.csproj --no-restore
dotnet run --project Tests/HeritageCalendar/HeritageCalendar.Tests.csproj
pwsh -File Tests/calendar-http-smoke.ps1 -BaseUrl http://127.0.0.1:5190
```

The standalone test runner links the three calendar source files and needs no third-party test framework. It checks leap/common months, both date conventions, overlaps, historical lower bounds, normalization, filtering, year navigation, invalid dates, Iran timezone, and recurrence boundaries throughout 1300–1500. HTTP checks are read-only and cover rendered dates, encoding, stylesheet availability, navigation and invalid query responses. Run against an isolated local database; do not reseed the production store for calendar tests.
