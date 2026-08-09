# PowerShell E2E smoke test against running API (http://localhost:5080)
$ErrorActionPreference = "Stop"
$base = "http://localhost:5080"

$admin = Invoke-RestMethod -Method Post -Uri "$base/api/auth/login" -ContentType "application/json" -Body (@{ email="admin@fynexpay.iq"; password="Admin@12345" } | ConvertTo-Json)
$email = "merchant$(Get-Random)@test.iq"
$reg = Invoke-RestMethod -Method Post -Uri "$base/api/auth/register" -ContentType "application/json" -Body (@{
  email=$email; password="Merchant@123"; fullName="Smoke Merchant"; businessName="Smoke Shop"
} | ConvertTo-Json)

$adminHeaders = @{ Authorization = "Bearer $($admin.token)" }
$merchants = Invoke-RestMethod -Method Get -Uri "$base/api/admin/merchants" -Headers $adminHeaders
$m = $merchants | Where-Object { $_.contactEmail -eq $email } | Select-Object -First 1
Invoke-RestMethod -Method Patch -Uri "$base/api/admin/merchants/$($m.id)" -Headers $adminHeaders -ContentType "application/json" -Body (@{ status="Active"; commissionPercent=2.5 } | ConvertTo-Json) | Out-Null

$merchantHeaders = @{ Authorization = "Bearer $($reg.token)" }
$keyRes = Invoke-RestMethod -Method Post -Uri "$base/api/merchant/api-keys" -Headers $merchantHeaders -ContentType "application/json" -Body (@{ name="Smoke" } | ConvertTo-Json)
$pay = Invoke-RestMethod -Method Post -Uri "$base/v1/payments" -Headers @{ "X-Api-Key"=$keyRes.apiKey } -ContentType "application/json" -Body (@{ amount=1000; serviceType="Smoke service" } | ConvertTo-Json)

if (-not $pay.checkoutUrl -or $pay.checkoutUrl -notmatch '/checkout/') { throw "Expected hosted checkout URL, got $($pay.checkoutUrl)" }
if ($pay.provider -ne "PendingSelection") { throw "Expected PendingSelection, got $($pay.provider)" }

# Customer selects Fib (mock if no credentials)
$init = Invoke-WebRequest -Method Post -Uri "$base/checkout/$($pay.id)/pay" -ContentType "application/x-www-form-urlencoded" -Body "provider=Fib" -MaximumRedirection 0 -SkipHttpErrorCheck
# After select, payment should have provider
$pay2 = Invoke-RestMethod -Method Get -Uri "$base/v1/payments/$($pay.id)" -Headers @{ "X-Api-Key"=$keyRes.apiKey }
Write-Host "After select provider=$($pay2.provider) checkout=$($pay2.checkoutUrl)"

Invoke-RestMethod -Method Post -Uri "$base/api/webhooks/mock/complete/$($pay.id)" | Out-Null
$wallet = Invoke-RestMethod -Method Get -Uri "$base/v1/wallet" -Headers @{ "X-Api-Key"=$keyRes.apiKey }

Write-Host "OK payment=$($pay.id) hosted=$($pay.checkoutUrl) netCredited=$($wallet.availableBalance)"
