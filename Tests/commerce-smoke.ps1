param(
    [string]$BaseUrl = 'http://127.0.0.1:5190',
    [Parameter(Mandatory)][string]$AdminEmail,
    [Parameter(Mandatory)][string]$AdminPassword
)
$ErrorActionPreference = 'Stop'
if (([uri]$BaseUrl).Host -notin @('127.0.0.1', 'localhost')) { throw 'This test creates local test orders. Only run against localhost.' }
function Assert($condition, $label) { if (!$condition) { throw "FAIL: $label" }; Write-Output "PASS: $label" }
function Token($html) { [regex]::Match($html, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value }
function Review($html) { [System.Net.WebUtility]::HtmlDecode([regex]::Match($html, 'id="ReviewToken"[^>]*value="([^"]+)"').Groups[1].Value) }
function Get-Page($path, $session) { Invoke-WebRequest ($BaseUrl + $path) -WebSession $session -SkipHttpErrorCheck }
function Post-Page($path, $body, $session) { Invoke-WebRequest ($BaseUrl + $path) -Method Post -Body $body -WebSession $session -SkipHttpErrorCheck }
$admin = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$buyer = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$stranger = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$login = Get-Page '/admin/login' $admin
$signedIn = Post-Page '/admin/login' @{ Email=$AdminEmail; Password=$AdminPassword; __RequestVerificationToken=(Token $login.Content) } $admin
Assert ($signedIn.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/admin') 'admin login'
Assert ($signedIn.Content.Contains('STORE OVERVIEW') -and $signedIn.Content.Contains('Recent orders')) 'admin overview dashboard'
$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)

$registration = Get-Page '/account/register' $buyer
Assert ($registration.Content.Contains('Create an account') -and ![string]::IsNullOrEmpty((Token $registration.Content))) 'customer registration page is available and CSRF protected'
$customerEmail = "customer-$suffix@example.invalid"
$registered = Post-Page '/account/register' @{FullName='Account Smoke';Email=$customerEmail;Password='Strong!Smoke123';ConfirmPassword='Strong!Smoke123';AcceptPolicies='true';__RequestVerificationToken=(Token $registration.Content)} $buyer
Assert ($registered.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/account/check-email' -and $registered.Content.Contains('Check your email')) 'customer registration requires email confirmation'
$unconfirmedLogin = Get-Page '/account/login' $buyer
$unconfirmedResult = Post-Page '/account/login' @{Email=$customerEmail;Password='Strong!Smoke123';__RequestVerificationToken=(Token $unconfirmedLogin.Content)} $buyer
Assert ($unconfirmedResult.Content.Contains('confirm your email')) 'unconfirmed customer cannot sign in'
$forgot = Get-Page '/account/forgot-password' $stranger
$forgotResult = Post-Page '/account/forgot-password' @{Email='missing@example.invalid';__RequestVerificationToken=(Token $forgot.Content)} $stranger
Assert ($forgotResult.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/account/forgot-password-sent' -and $forgotResult.Content.Contains('If the address belongs')) 'password recovery does not reveal whether an account exists'

$privacy = Get-Page '/privacy/' $buyer
$terms = Get-Page '/terms/' $buyer
$shippingPolicy = Get-Page '/shipping-returns/' $buyer
Assert ($privacy.Content.Contains('Privacy Policy') -and $privacy.Content.Contains('Stripe') -and $privacy.Content.Contains('Resend')) 'privacy policy discloses data practices and processors'
Assert ($terms.Content.Contains('Terms of Service') -and $terms.Content.Contains('Commonwealth of Virginia')) 'store terms disclose governing terms'
Assert ($shippingPolicy.Content.Contains('7–10 business days') -and $shippingPolicy.Content.Contains('$15') -and $shippingPolicy.Content.Contains('$150') -and $shippingPolicy.Content.Contains('30 calendar days')) 'shipping and returns policy matches store configuration'

$contactName = 'Contact Smoke ' + $suffix
$contact = Get-Page '/contact/' $buyer
Assert ($contact.Content.Contains('START A CONVERSATION') -and -not [string]::IsNullOrEmpty((Token $contact.Content))) 'contact form is server backed and CSRF protected'
$contactBlocked = Post-Page '/contact' @{Name=$contactName;Email='contact@example.com';Interest='Product question';Message='A valid message that must be blocked without its request token.'} $buyer
Assert ([int]$contactBlocked.StatusCode -eq 400) 'contact submission without CSRF rejected'
$contactInvalid = Post-Page '/contact' @{Name=$contactName;Email='invalid';Interest='Product question';Message='short';__RequestVerificationToken=(Token $contact.Content)} $buyer
Assert ($contactInvalid.Content.Contains('not a valid e-mail address') -and $contactInvalid.Content.Contains('minimum length')) 'contact fields validated on the server'
$contact = Get-Page '/contact/' $buyer
$contactSent = Post-Page '/contact' @{Name=$contactName;Email='contact@example.com';Interest='Product question';Message='I would like more details about a dimensional wall art piece.';__RequestVerificationToken=(Token $contact.Content)} $buyer
Assert ($contactSent.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/contact/thanks' -and $contactSent.Content.Contains('Message received')) 'valid contact inquiry saved'
$inquiries = Get-Page '/admin/inquiries' $admin
$inquiryRow = [regex]::Match($inquiries.Content, '(?s)<tr[^>]*>(?:(?!</tr>).)*' + $contactName + '(?:(?!</tr>).)*</tr>').Value
$inquiryId = [regex]::Match($inquiryRow, '/admin/inquiries/(\d+)').Groups[1].Value
Assert (![string]::IsNullOrEmpty($inquiryId) -and $inquiryRow.Contains('Pending')) 'contact inquiry appears in admin with queued email'
$inquiryDetail = Get-Page ("/admin/inquiries/$inquiryId") $admin
Assert ($inquiryDetail.Content.Contains('contact@example.com') -and $inquiryDetail.Content.Contains('dimensional wall art')) 'admin can read contact inquiry'
$archivedInquiry = Post-Page ("/admin/inquiries/$inquiryId/archive") @{__RequestVerificationToken=(Token $inquiryDetail.Content)} $admin
$archivedList = Get-Page '/admin/inquiries?status=archived' $admin
Assert ($archivedList.Content.Contains($contactName)) 'admin can archive contact inquiry'

$name = 'Commerce Smoke ' + $suffix
$slug = 'commerce-smoke-' + $suffix
$editor = Get-Page '/admin/products/create' $admin
$form = @{
    Name=$name; Slug=$slug; CategoryId='1'; PriceDollars='19.95'; ShortDescription='Local commerce smoke test.';
    Description='Temporary test product.'; Size='Test size'; Material='Test material'; Finish='Test gold'; Badge='TEST';
    CardClass='product-card--cream'; HighlightsText='Test'; IsFeatured='false'; IsPublished='true'; DisplayOrder='999';
    __RequestVerificationToken=(Token $editor.Content);
    NewImages=(Get-Item (Join-Path $PSScriptRoot '../KashefProject/KashefProject/wwwroot/images/products/collection/shahyad-tower-02.jpg'))
}
$created = Invoke-WebRequest ($BaseUrl + '/admin/products/create') -Method Post -Form $form -WebSession $admin
$block = [regex]::Match($created.Content, '(?s)<article class="product-admin-row">(?:(?!</article>).)*' + $name + '(?:(?!</article>).)*</article>').Value
$id = [regex]::Match($block, '/admin/products/(\d+)/edit').Groups[1].Value
Assert (![string]::IsNullOrEmpty($id)) 'priced test product created'
$orderId = $null
try {
    $product = Get-Page ("/shop/$slug/") $buyer
    $csrf = Token $product.Content
    Assert (![string]::IsNullOrEmpty($csrf)) 'buy form has CSRF protection'
    $blocked = Post-Page '/cart/add' @{productId=$id; quantity='2'} $buyer
    Assert ([int]$blocked.StatusCode -eq 400) 'missing CSRF rejected'
    $unpriced = Post-Page '/cart/add' @{productId='1'; quantity='1'; __RequestVerificationToken=$csrf} $buyer
    Assert ($unpriced.Content.Contains('not available to order') -and $unpriced.Content.Contains('Your bag is empty')) 'unpriced catalog pieces cannot be ordered'
    $bad = Post-Page '/cart/add' @{productId=$id; quantity='21'; __RequestVerificationToken=$csrf} $buyer
    Assert ($bad.Content.Contains('Your bag is empty')) 'quantity limit enforced'
    $bag = Post-Page '/cart/add' @{productId=$id; quantity='2'; price='0.01'; __RequestVerificationToken=$csrf} $buyer
    Assert ($bag.Content.Contains('$39.90')) 'server price overrides client price'
    $removed = Post-Page '/cart/update' @{productId=$id; quantity='0'; __RequestVerificationToken=(Token $bag.Content)} $buyer
    Assert ($removed.Content.Contains('Your bag is empty')) 'items can be removed'
    $bag = Post-Page '/cart/add' @{productId=$id; quantity='2'; __RequestVerificationToken=$csrf} $buyer
    Assert ((Get-Page '/cart/' $stranger).Content.Contains('Your bag is empty')) 'carts isolated between visitors'
    $badUpdate = Post-Page '/cart/update' @{productId=$id; quantity='-1'; __RequestVerificationToken=(Token $bag.Content)} $buyer
    Assert ($badUpdate.Content.Contains('$39.90')) 'negative quantity rejected'
    $checkout = Get-Page '/checkout/' $buyer
    Assert ($checkout.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/account/login' -and $checkout.Content.Contains('Sign in to continue checkout')) 'guest checkout is disabled'
    $checkoutLogin = Post-Page '/account/login' @{Email=$AdminEmail;Password=$AdminPassword;ReturnUrl='/checkout/';__RequestVerificationToken=(Token $checkout.Content)} $buyer
    Assert ($checkoutLogin.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/checkout/' -and $checkoutLogin.Content.Contains('$39.90')) 'sign in preserves and transfers the guest bag'
    $checkout = $checkoutLogin
    $review = Review $checkout.Content
    Assert (![string]::IsNullOrEmpty($review)) 'signed checkout review generated'
    Assert ($checkout.Content.Contains('/terms/') -and $checkout.Content.Contains('/shipping-returns/') -and $checkout.Content.Contains('7–10 business days')) 'checkout requires linked store policies and shows production timing'
    Assert ($checkout.Content.Contains('Standard U.S. shipping') -and $checkout.Content.Contains('$15.00') -and $checkout.Content.Contains('$54.90')) 'flat shipping included before payment'
    Assert ($checkout.Content.Contains('readonly') -and $checkout.Content.Contains($AdminEmail)) 'checkout uses the verified account email'
    $details = @{FullName='Local Test Customer';Email='attacker@example.com';AcknowledgePending='true';ReviewToken=$review;__RequestVerificationToken=(Token $checkout.Content);SubtotalCents='1';Country='ZZ';Status='Paid'}

    $editor = Get-Page ("/admin/products/$id/edit") $admin
    $form.Remove('NewImages'); $form.Id=$id; $form.PriceDollars='24.95'; $form.__RequestVerificationToken=Token $editor.Content
    Post-Page ("/admin/products/$id/edit") $form $admin | Out-Null
    $changed = Post-Page '/checkout/' $details $buyer
    Assert ($changed.Content.Contains('details have changed') -and $changed.Content.Contains('$49.90')) 'price changes require a fresh review'
    $details.ReviewToken = Review $changed.Content; $details.__RequestVerificationToken=Token $changed.Content
    $tampered = $details.Clone(); $tampered.ReviewToken='invalid-token'
    $rejected = Post-Page '/checkout/' $tampered $buyer
    Assert ($rejected.Content.Contains('review is invalid')) 'tampered checkout review rejected'
    $details.ReviewToken=Review $rejected.Content; $details.__RequestVerificationToken=Token $rejected.Content

    # Send the same approved review twice concurrently. Both must resolve to one order.
    $handler = [System.Net.Http.HttpClientHandler]::new(); $handler.CookieContainer=$buyer.Cookies
    $client = [System.Net.Http.HttpClient]::new($handler)
    try {
        $pairs = [System.Collections.Generic.Dictionary[string,string]]::new()
        foreach ($key in $details.Keys) { $pairs[$key]=[string]$details[$key] }
        $firstContent=[System.Net.Http.FormUrlEncodedContent]::new($pairs)
        $secondContent=[System.Net.Http.FormUrlEncodedContent]::new($pairs)
        $firstTask=$client.PostAsync(($BaseUrl+'/checkout/'),$firstContent)
        $secondTask=$client.PostAsync(($BaseUrl+'/checkout/'),$secondContent)
        $first=$firstTask.GetAwaiter().GetResult(); $second=$secondTask.GetAwaiter().GetResult()
        $savedPath=$first.RequestMessage.RequestUri.AbsolutePath
        Assert ($savedPath.StartsWith('/checkout/saved/') -and $savedPath -eq $second.RequestMessage.RequestUri.AbsolutePath) 'concurrent submissions create only one order'
        $savedHtml=$first.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Assert ($savedHtml.Contains('$49.90') -and $savedHtml.Contains('$64.90') -and $savedHtml.Contains('Pending payment')) 'subtotal, shipping, and unpaid status are server controlled'
    } finally { $client.Dispose() }
    Assert ((Get-Page '/cart/' $buyer).Content.Contains('Your bag is empty')) 'successful save clears the bag'
    Assert ((Get-Page $savedPath $stranger).BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/account/login') 'another visitor cannot read the order'
    $retry=Post-Page '/checkout/' $details $buyer
    Assert ($retry.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq $savedPath) 'retry after cart clearing is idempotent'
    $reference=$savedPath.Split('/')[-1]
    $orders=Get-Page '/admin/orders' $admin
    $orderRow=[regex]::Match($orders.Content, '(?s)<tr>(?:(?!</tr>).)*' + $reference + '(?:(?!</tr>).)*</tr>').Value
    $orderId=[regex]::Match($orderRow, 'href="/admin/orders/(\d+)"').Groups[1].Value
    Assert (![string]::IsNullOrEmpty($orderId)) 'saved order appears in admin'
    $order=Get-Page ("/admin/orders/$orderId") $admin
    Assert ($order.Content.Contains($AdminEmail) -and !$order.Content.Contains('attacker@example.com') -and $order.Content.Contains('Delivery address will be added after Stripe') -and $order.Content.Contains('$64.90')) 'verified account email and server amount snapshots are enforced'
    $account = Get-Page '/account/' $buyer
    Assert ($account.Content.Contains($reference) -and $account.Content.Contains('Your orders')) 'customer dashboard lists the saved order'
    Assert ((Get-Page ("/account/orders/$reference") $buyer).Content.Contains('$64.90')) 'customer can open own order details'
    Assert ([int](Get-Page ("/account/orders/$reference/invoice") $buyer).StatusCode -eq 404) 'invoice is not issued before payment'
    $fulfillmentDenied=Post-Page ("/admin/orders/$orderId/fulfillment") @{FulfillmentStatus='Shipped';TrackingCarrier='USPS';TrackingNumber='QA123';__RequestVerificationToken=(Token $order.Content)} $admin
    Assert ($fulfillmentDenied.Content.Contains('only be updated after Stripe confirms payment') -and $fulfillmentDenied.Content.Contains('Payment not received')) 'unpaid orders cannot be fulfilled'
    Assert ((Get-Page '/admin/orders' $stranger).BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/admin/login') 'order administration requires authentication'

    $freeBuyer = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $freeProduct = Get-Page ("/shop/$slug/") $freeBuyer
    $freeBag = Post-Page '/cart/add' @{productId=$id;quantity='7';__RequestVerificationToken=(Token $freeProduct.Content)} $freeBuyer
    $freeLogin = Get-Page '/account/login?returnUrl=%2Fcheckout%2F' $freeBuyer
    $freeCheckout = Post-Page '/account/login' @{Email=$AdminEmail;Password=$AdminPassword;ReturnUrl='/checkout/';__RequestVerificationToken=(Token $freeLogin.Content)} $freeBuyer
    Assert ($freeCheckout.Content.Contains('Standard U.S. shipping') -and $freeCheckout.Content.Contains('Free') -and $freeCheckout.Content.Contains('$174.65')) 'free shipping threshold applied'
    Post-Page '/cart/update' @{productId=$id;quantity='0';__RequestVerificationToken=(Token $freeBag.Content)} $freeBuyer | Out-Null

    # Put a product in another bag, then unpublish it.
    $anotherProduct=Get-Page ("/shop/$slug/") $stranger
    Post-Page '/cart/add' @{productId=$id;quantity='1';__RequestVerificationToken=(Token $anotherProduct.Content)} $stranger | Out-Null
    $editor=Get-Page ("/admin/products/$id/edit") $admin
    $form.PriceDollars='99.95'; $form.IsPublished='false'; $form.Name=$name+' changed'; $form.__RequestVerificationToken=Token $editor.Content
    Post-Page ("/admin/products/$id/edit") $form $admin | Out-Null
    $unavailable=Get-Page '/cart/' $stranger
    Assert ($unavailable.Content.Contains('Unavailable')) 'unpublished products cannot be ordered'
    $order=Get-Page ("/admin/orders/$orderId") $admin
    Assert ($order.Content.Contains('$49.90') -and !$order.Content.Contains($name+' changed')) 'order snapshots survive catalog edits'
    Post-Page ("/admin/orders/$orderId/cancel") @{__RequestVerificationToken=(Token $order.Content)} $admin | Out-Null
    Assert ((Get-Page $savedPath $buyer).Content.Contains('Cancelled')) 'admin cancellation reflected to order owner'
} finally {
    $adminList=Get-Page '/admin/products' $admin
    Post-Page ("/admin/products/$id/delete") @{__RequestVerificationToken=(Token $adminList.Content)} $admin | Out-Null
    if ($orderId) { Assert ([int](Get-Page ("/admin/orders/$orderId") $admin).StatusCode -eq 200) 'order retained after product deletion' }
    Write-Output 'Temporary product and uploaded image cleaned up. Test order remains only in the local test database.'
}
