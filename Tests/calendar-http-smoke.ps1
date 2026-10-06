param([string]$BaseUrl = 'http://127.0.0.1:5190')
$ErrorActionPreference = 'Stop'
$origin = [uri]$BaseUrl
if (!$origin.IsAbsoluteUri -or $origin.UserInfo -or $origin.AbsolutePath -ne '/' -or $origin.Query -or $origin.Fragment) { throw 'Use a root origin without credentials.' }
if ($origin.Scheme -ne 'https' -and $origin.Host -notin @('localhost', '127.0.0.1')) { throw 'Remote checks require HTTPS.' }
function Assert($condition, $label) {
    if (!$condition) { throw "FAIL: $label" }
    Write-Output "PASS: $label"
}
function Fetch($path) {
    $response = Invoke-WebRequest ([uri]::new($origin, $path)) -TimeoutSec 15 -UseBasicParsing
    if ($response.StatusCode -ne 200) { throw "FAIL: $path unavailable" }
    return [System.Net.WebUtility]::HtmlDecode($response.Content)
}
$page = Fetch '/calendar/?year=2585&month=8&day=4'
Assert ($page.Contains('lang="fa"') -and $page.Contains('dir="rtl"')) 'Persian page and RTL main'
Assert (([regex]::Matches($page, 'data-calendar-day=').Count) -eq 30) 'Thirty-day Aban grid'
Assert ($page.Contains('زادروز محمدرضاشاه پهلوی') -and $page.Contains('تاج‌گذاری شاه و شهبانو')) 'Both events on fourth Aban'
Assert ($page.Contains('target="_blank" rel="noopener noreferrer"')) 'Safe source links'
Assert ($page.Contains('href="/calendar') -and $page.Contains('name="convention"')) 'Calendar navigation and date-convention selector'
Assert ($page.Contains('سال شاهنشاهی') -and $page.Contains('value="2585"') -and $page.Contains('۴ آبان ۲۵۸۵')) 'Imperial year in selector and event dates'
Assert (!$page.Contains('year=1405') -and $page.Contains('year=2585')) 'All generated navigation uses imperial years'
$legacy = Fetch '/calendar/?year=1405&month=8&day=4'
Assert ($legacy.Contains('value="2585"')) 'Legacy preview links redirect to imperial years'
$boundary = Fetch '/calendar/?year=2585&month=12'
Assert ($boundary.Contains('year=2586&month=1')) 'Next imperial year navigation'
$leap = Fetch '/calendar/?year=2579&month=12'
Assert (([regex]::Matches($leap, 'data-calendar-day=').Count) -eq 30) 'Leap year rendered'
$common = Fetch '/calendar/?year=2580&month=12'
Assert (([regex]::Matches($common, 'data-calendar-day=').Count) -eq 29) 'Common year rendered'
$search = Fetch ('/calendar/?year=2585&month=1&q=' + [uri]::EscapeDataString('كوروش'))
Assert ($search.Contains('روز کوروش بزرگ') -and $search.Contains('نتایج «كوروش»')) 'Normalized whole-year search'
# Inspect raw HTML: Razor must encode even when user query is echoed in results.
$raw = (Invoke-WebRequest ([uri]::new($origin, '/calendar/?q=%3Cscript%3Ealert(1)%3C%2Fscript%3E')) -UseBasicParsing).Content
Assert (!$raw.Contains('<script>alert(1)</script>')) 'User search is HTML-encoded'
foreach ($path in @('/calendar/?year=2681', '/calendar/?year=invalid', '/calendar/?year=2580&month=12&day=30')) {
    $status = 0
    try { $status = (Invoke-WebRequest ([uri]::new($origin, $path)) -TimeoutSec 15 -UseBasicParsing).StatusCode }
    catch { if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode } else { throw } }
    Assert ($status -eq 400) "Invalid date returns 400: $path"
}
$css = Fetch '/css/calendar.css'
Assert ($css.Contains('@media') -and $css.Contains('.heritage-calendar')) 'Responsive scoped stylesheet served'
foreach ($path in @('/', '/shop/', '/gallery/')) {
    $store = Fetch $path
    Assert ($store.Contains('href="/calendar/"')) "Calendar linked from $path"
}
Write-Output 'Calendar HTTP smoke checks completed (read-only).'
