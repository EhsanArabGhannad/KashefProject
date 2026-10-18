param([string]$BaseUrl='http://127.0.0.1:5196', [string]$AdminEmail='calendar-admin@example.invalid', [string]$AdminPassword='Preview!Calendar2585')
$ErrorActionPreference='Stop'
$uri=[uri]$BaseUrl
if (!$uri.IsAbsoluteUri -or $uri.Host -notin @('127.0.0.1','localhost') -or $uri.UserInfo -or $uri.AbsolutePath -ne '/' -or $uri.Query -or $uri.Fragment) { throw 'Mutating test: only use an isolated localhost database.' }
function Assert($value,$label) { if (!$value) { throw "FAIL: $label" }; Write-Output "PASS: $label" }
function Token($html) { [regex]::Match($html,'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value }
function Revision($html) { [regex]::Match($html,'id="Revision"[^>]*value="([^"]*)"').Groups[1].Value }
function GetPage($path,$session) { Invoke-WebRequest ($BaseUrl+$path) -WebSession $session -SkipHttpErrorCheck -TimeoutSec 20 }
function PostPage($path,$body,$session) { Invoke-WebRequest ($BaseUrl+$path) -Method Post -Body $body -WebSession $session -SkipHttpErrorCheck -TimeoutSec 20 }
function PngFile {
    $path=Join-Path $PSScriptRoot ('../artifacts/calendar-admin-preview/image-'+[Guid]::NewGuid().ToString('N')+'.png')
    [IO.File]::WriteAllBytes($path,[Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jR1cAAAAASUVORK5CYII='))
    Get-Item $path
}
$admin=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
$stranger=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
$gate=GetPage '/admin/calendar-events' $stranger
Assert ($gate.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/admin/login') 'Anonymous visitors cannot manage events'
$login=GetPage '/admin/login' $admin
$signed=PostPage '/admin/login' @{Email=$AdminEmail;Password=$AdminPassword;__RequestVerificationToken=(Token $login.Content)} $admin
Assert ($signed.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/admin') 'Local test admin signs in'
$index=GetPage '/admin/calendar-events' $admin
Assert ($index.Content.Contains('Nowruz') -and $index.Content.Contains('Calendar events')) 'Built-in events are manageable'
# Only explicitly disposable test rows from earlier interrupted localhost runs.
foreach ($row in [regex]::Matches($index.Content,'(?s)<article class="product-admin-row calendar-admin-row">(?:(?!</article>).)*<h2>CalendarSmoke-[^<]+</h2>(?:(?!</article>).)*</article>')) {
    $oldId=[regex]::Match($row.Value,'/admin/calendar-events/(\d+)/delete').Groups[1].Value
    $confirm=GetPage "/admin/calendar-events/$oldId/delete" $admin
    $oldRevision=[regex]::Match($confirm.Content,'name="revision" value="([^"]+)"').Groups[1].Value
    $null=PostPage "/admin/calendar-events/$oldId/delete" @{revision=$oldRevision;__RequestVerificationToken=(Token $confirm.Content)} $admin
}
$editor=GetPage '/admin/calendar-events/create' $admin
$name='CalendarSmoke-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$form=@{Id='0';Revision='';TitleEn=$name;TitleFa='آزمایش رویداد';DescriptionEn='Editable test event';DescriptionFa='رویداد آزمایشی';Category='ancient';Rule='Solar';Month='7';Day='24';Duration='1';IsPublished='true';__RequestVerificationToken=(Token $editor.Content)}
$blocked=PostPage '/admin/calendar-events/create' @{TitleEn=$name} $admin
Assert ($blocked.StatusCode -eq 400) 'Creation requires CSRF token'
$form.Day='31'
$invalid=PostPage '/admin/calendar-events/create' $form $admin
Assert ($invalid.Content.Contains('does not exist') -and !$invalid.Content.Contains('Calendar event added.')) 'Invalid calendar date rejected'
$form.Day='24'
$created=PostPage '/admin/calendar-events/create' $form $admin
Assert ($created.Content.Contains('Calendar event added.')) 'Event can be created'
$row=[regex]::Match($created.Content,'(?s)<article class="product-admin-row calendar-admin-row">(?:(?!</article>).)*'+$name+'(?:(?!</article>).)*</article>').Value
$id=[regex]::Match($row,'/admin/calendar-events/(\d+)/edit').Groups[1].Value
Assert (![string]::IsNullOrEmpty($id)) 'Created event appears in admin list'
$public=GetPage '/calendar/?year=2585&month=7&day=24' $stranger
Assert ($public.Content.Contains($name)) 'Published event appears immediately'
$persian=GetPage '/calendar/?year=2585&month=7&day=24&lang=fa' $stranger
Assert ([System.Net.WebUtility]::HtmlDecode($persian.Content).Contains('آزمایش رویداد')) 'Persian content appears in Persian view'
$edit=GetPage "/admin/calendar-events/$id/edit" $admin
$noToken=PostPage "/admin/calendar-events/$id/edit" @{Id=$id;TitleEn='Blocked edit'} $admin
Assert ($noToken.StatusCode -eq 400) 'Editing requires CSRF token'
$form.Id=$id; $form.Revision=Revision $edit.Content; $form.__RequestVerificationToken=Token $edit.Content
$originalRevision=$form.Revision
$form.TitleEn=$name+'-edited'
$edited=PostPage "/admin/calendar-events/$id/edit" $form $admin
Assert ($edited.Content.Contains('Calendar event updated.')) 'Event can be edited'
$stale=PostPage "/admin/calendar-events/$id/edit" $form $admin
Assert ($stale.Content.Contains('changed by another editor')) 'Stale form cannot overwrite newer edits'
$edit=GetPage "/admin/calendar-events/$id/edit" $admin
$form.Revision=Revision $edit.Content; $form.__RequestVerificationToken=Token $edit.Content
$form.IsPublished='false'
$hidden=PostPage "/admin/calendar-events/$id/edit" $form $admin
Assert ($hidden.Content.Contains('Calendar event updated.')) 'Event can be hidden'
$public=GetPage '/calendar/?year=2585&month=7&day=24' $stranger
Assert (!$public.Content.Contains($name)) 'Hidden event is absent from public calendar'
$edit=GetPage "/admin/calendar-events/$id/edit" $admin
$form.Revision=Revision $edit.Content; $form.__RequestVerificationToken=Token $edit.Content; $form.IsPublished='true'; $form.Image=PngFile
$withImage=Invoke-WebRequest ($BaseUrl+"/admin/calendar-events/$id/edit") -Method Post -Form $form -WebSession $admin -SkipHttpErrorCheck
Assert ($withImage.Content.Contains('Calendar event updated.')) 'Image upload accepted'
$public=GetPage '/calendar/?year=2585&month=7&day=24' $stranger
$image=[regex]::Match($public.Content,'class="day-background" src="(/uploads/[^"/]+)"').Groups[1].Value
Assert (![string]::IsNullOrEmpty($image)) 'Image is a faded day-card background'
Assert ((GetPage $image $stranger).StatusCode -eq 200) 'Uploaded image is served'
$badFixture=Join-Path $PSScriptRoot '../artifacts/calendar-admin-preview/invalid.png'
[IO.File]::WriteAllBytes($badFixture,[byte[]]::new(100))
$edit=GetPage "/admin/calendar-events/$id/edit" $admin
$form.Revision=Revision $edit.Content; $form.__RequestVerificationToken=Token $edit.Content; $form.Image=Get-Item $badFixture
$badImage=Invoke-WebRequest ($BaseUrl+"/admin/calendar-events/$id/edit") -Method Post -Form $form -WebSession $admin -SkipHttpErrorCheck
Assert ($badImage.Content.Contains('file contents must match')) 'Disguised non-image upload is rejected'
Assert ((GetPage '/calendar/?year=2585&month=7&day=24' $stranger).Content.Contains($image)) 'Invalid replacement preserves previous image'
$form.Image=PngFile
$replacement=Invoke-WebRequest ($BaseUrl+"/admin/calendar-events/$id/edit") -Method Post -Form $form -WebSession $admin -SkipHttpErrorCheck
Assert ($replacement.Content.Contains('Calendar event updated.')) 'Image can be replaced'
$replacementPage=GetPage '/calendar/?year=2585&month=7&day=24' $stranger
$replacementPath=[regex]::Match($replacementPage.Content,'class="day-background" src="(/uploads/[^"/]+)"').Groups[1].Value
Assert ($replacementPath -and $replacementPath -ne $image -and (GetPage $image $stranger).StatusCode -eq 404) 'Replacement gets a new path and cleans the previous file'
$image=$replacementPath
$edit=GetPage "/admin/calendar-events/$id/edit" $admin
$form.Revision=Revision $edit.Content; $form.__RequestVerificationToken=Token $edit.Content; $form.Remove('Image'); $form.RemoveImage='true'
$removed=PostPage "/admin/calendar-events/$id/edit" $form $admin
Assert ($removed.Content.Contains('Calendar event updated.')) 'Image can be removed independently'
$public=GetPage '/calendar/?year=2585&month=7&day=24' $stranger
Assert (!$public.Content.Contains('class="day-background"')) 'Removed background is absent'
Assert ((GetPage $image $stranger).StatusCode -eq 404) 'Removed image file is cleaned up'
$edit=GetPage "/admin/calendar-events/$id/edit" $admin
$form.Revision=Revision $edit.Content; $form.__RequestVerificationToken=Token $edit.Content; $form.Image=PngFile; $form.Remove('RemoveImage')
$restored=Invoke-WebRequest ($BaseUrl+"/admin/calendar-events/$id/edit") -Method Post -Form $form -WebSession $admin -SkipHttpErrorCheck
Assert ($restored.Content.Contains('Calendar event updated.')) 'Image can be re-added after removal'
$image=[regex]::Match((GetPage '/calendar/?year=2585&month=7&day=24' $stranger).Content,'class="day-background" src="(/uploads/[^"/]+)"').Groups[1].Value
$delete=GetPage "/admin/calendar-events/$id/delete" $admin
Assert ($delete.Content.Contains('Delete event?') -and (GetPage '/calendar/?year=2585&month=7&day=24' $stranger).Content.Contains($name)) 'GET delete is confirmation only'
$blocked=PostPage "/admin/calendar-events/$id/delete" @{} $admin
Assert ($blocked.StatusCode -eq 400) 'Deletion requires CSRF token'
$revision=[regex]::Match($delete.Content,'name="revision" value="([^"]+)"').Groups[1].Value
$staleDelete=PostPage "/admin/calendar-events/$id/delete" @{revision='stale';__RequestVerificationToken=(Token $delete.Content)} $admin
Assert ($staleDelete.StatusCode -eq 409) 'Stale deletion cannot remove a newer event'
$deleted=PostPage "/admin/calendar-events/$id/delete" @{revision=$revision;__RequestVerificationToken=(Token $delete.Content)} $admin
Assert ($deleted.Content.Contains('Calendar event deleted.')) 'Event can be deleted'
Assert (!(GetPage '/calendar/?year=2585&month=7&day=24' $stranger).Content.Contains($name)) 'Deleted event is absent immediately'
Assert ((GetPage "/admin/calendar-events/$id/edit" $admin).StatusCode -eq 404) 'Deleted event cannot be edited'
Assert ((GetPage $image $stranger).StatusCode -eq 404) 'Deleting an event cleans its image file'
Write-Output 'Calendar admin HTTP smoke completed on local test data.'
