# Fix SSL for dev lanes: ensure *-dev DNS exists, remove nested dev.* records (no Universal cert).
# Optional: enable Total TLS in dashboard for nested names (see macroverse repo ops/ENABLE-TOTAL-TLS.txt).
param(
  [string]$Token = $env:CLOUDFLARE_DNS_API_TOKEN,
  [string]$ZoneName = "aday.net.au",
  [string]$OriginIp = "172.105.171.251",
  [switch]$KeepNested
)

$createNames = @("macroverse-dev.aday.net.au", "artbastard-dev.aday.net.au")
$removeNames = @("dev.macroverse.aday.net.au", "dev.artbastard.aday.net.au")

if (-not $Token) {
  Write-Host "Set CLOUDFLARE_DNS_API_TOKEN (Zone DNS Edit on aday.net.au)."
  exit 1
}

$headers = @{
  Authorization = "Bearer $Token"
  "Content-Type" = "application/json"
}

$zoneRes = Invoke-RestMethod -Uri "https://api.cloudflare.com/client/v4/zones?name=$([uri]::EscapeDataString($ZoneName))" -Headers $headers
$zoneId = $zoneRes.result[0].id
Write-Host "Zone: $ZoneName ($zoneId)"

function Upsert-A($name) {
  $q = "type=A&name=$([uri]::EscapeDataString($name))"
  $list = Invoke-RestMethod -Uri "https://api.cloudflare.com/client/v4/zones/$zoneId/dns_records?$q" -Headers $headers
  $body = @{ type = "A"; name = $name; content = $OriginIp; ttl = 1; proxied = $true } | ConvertTo-Json
  if ($list.result.Count -gt 0) {
    $id = $list.result[0].id
    $res = Invoke-RestMethod -Uri "https://api.cloudflare.com/client/v4/zones/$zoneId/dns_records/$id" -Headers $headers -Method Put -Body $body
    Write-Host "Updated $name (proxied)"
  } else {
    $res = Invoke-RestMethod -Uri "https://api.cloudflare.com/client/v4/zones/$zoneId/dns_records" -Headers $headers -Method Post -Body $body
    Write-Host "Created $name (proxied)"
  }
}

function Remove-A($name) {
  $q = "type=A&name=$([uri]::EscapeDataString($name))"
  $list = Invoke-RestMethod -Uri "https://api.cloudflare.com/client/v4/zones/$zoneId/dns_records?$q" -Headers $headers
  foreach ($rec in $list.result) {
    Invoke-RestMethod -Uri "https://api.cloudflare.com/client/v4/zones/$zoneId/dns_records/$($rec.id)" -Headers $headers -Method Delete | Out-Null
    Write-Host "Deleted DNS $name (id $($rec.id)) - nested name has no Universal SSL cert"
  }
  if (-not $list.result.Count) {
    Write-Host "No DNS record for $name"
  }
}

foreach ($n in $createNames) { Upsert-A $n }

if (-not $KeepNested) {
  foreach ($n in $removeNames) { Remove-A $n }
} else {
  Write-Host "Kept nested dev.* records (-KeepNested). Enable Total TLS in Cloudflare for HTTPS on those names."
}

Write-Host ""
Write-Host "Use HTTPS:"
Write-Host "  https://macroverse-dev.aday.net.au/"
Write-Host "  https://artbastard-dev.aday.net.au/"
Write-Host ""
Write-Host "Total TLS (only if you need dev.macroverse URLs):"
Write-Host "  https://dash.cloudflare.com/$zoneId/ssl-tls/settings"
