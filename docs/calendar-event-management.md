# Managed calendar events

Published on 2026-10-15 at `https://craftisma.net/admin/calendar-events` (existing Admin sign-in required). Release `/opt/craftisma/releases/20261015191133`; all 38 original bilingual events imported. See `heritage-calendar-launch.md` for backup and verification details.

## Administration

The Admin navigation now includes **Calendar**, at `/admin/calendar-events`. All routes require the existing Admin role. Add/edit forms contain Persian and English title, description and optional date note; category; date rule; month/day; duration (1–31 days); optional Imperial year restriction; optional earliest Gregorian year; visibility; and an optional image. A blank Imperial year restriction repeats the event annually. Hidden events remain editable but disappear from public results, counts and the countdown. Delete uses a separate confirmation page and a CSRF-protected POST; GET never deletes. Revision tokens reject stale edits/deletions.

Date rules preserve existing behavior: Imperial solar dates, Gregorian anniversaries, traditional name-day festivals (following the visitor's convention), Tuesday evening before the final Wednesday and the final-five-days start date. Month/day are ignored for the last two rules. Duration controls how many days are marked, including across Nowruz. Solar Esfand 30 and Gregorian February 29 recur only in valid leap years. An impossible leap date restricted to one particular year is rejected. The original historical lower bounds and caveats are retained.

## Images

- JPG, PNG or WebP only, up to 8 MB; extension and file signature must match. SVG/HTML and oversized files are rejected.
- Existing persistent `IImageStorage` is reused; generated filenames are not user-controlled. Public image paths cannot be posted through the editor.
- Optional live editor preview; uploading replaces the prior image. Removal can be done without deleting the event. Old files are cleaned after a successful database save, and failed saves clean newly uploaded files. An invalid replacement retains the old image.
- The earliest ordered occurrence with an image provides each day's background when events overlap (occurrences sort by date then stable event key). Other events remain in day details. Multi-day events carry their image on every covered day.
- The decorative absolute-position image fills the card with `object-fit: cover`, 32% opacity and a fading mask; 16% opacity on lime-selected days. Images cannot change grid height or scroll position. No-image days retain their existing appearance. English, Persian/RTL and mobile share the same image rendering.

## Persistence / release

Migration `20261015181921_AddManagedCalendarEvents` adds only `CalendarEvents` and `CalendarCatalogStates`. The one-time import and marker are committed in one transaction. Subsequent launches do not overwrite edits or restore deletions. Back up persistent data before deployment and retain the prior application release. Do not automatically restore an older whole-store database if rollback is needed: newer orders/accounts must be preserved. Reverting to an old application can show its old static catalogue, but the managed records remain in the database.

Do not run mutating admin smoke tests against production. No production event, customer, order, payment or email was changed during implementation.

## Verification

```powershell
dotnet run --project Tests/HeritageCalendar/HeritageCalendar.Tests.csproj --no-restore
dotnet run --project Tests/CalendarManagement/CalendarManagement.Tests.csproj -c CalendarAdmin --no-restore -p:UseAppHost=false
pwsh -File Tests/calendar-admin-http-smoke.ps1 -BaseUrl http://127.0.0.1:5196
pwsh -File Tests/calendar-http-smoke.ps1 -BaseUrl http://127.0.0.1:5196
```

Results: 8,353 calendar assertions, 30 management assertions (actual in-memory SQLite migrations/import, persistence, translation parity, recurrence, validation, signature rejection, concurrent updates, delete-all/restart), 32 local admin HTTP checks, and 30 public calendar HTTP checks passed. Admin HTTP tests use only a dedicated local database, a disposable `.invalid` admin account and generated PNG fixtures. They check authentication, CSRF, invalid dates, bilingual CRUD/visibility, stale edit/delete protection, upload/replacement/removal/deletion and public image delivery. JavaScript syntax check passed. NuGet vulnerability-feed lookup emitted NU1900 due to unavailable network access; compilation and tests succeeded.

Visual browser verification is separate from these automated checks; do not claim it from HTTP results alone.

Production publication verification: 30 public calendar HTTP checks, 11 read-only management endpoint/asset checks and 31 HTTPS PWA checks passed. Read-only database audit confirmed the migration, import marker, 38 published events and no incomplete bilingual entries. The service account has write permission on persistent uploads. No production CRUD/upload test, payment or email send was performed.
