$url = 'http://localhost:5070'
$durationSeconds = 10
$threads = 20

Write-Host "Benchmarking $url for $durationSeconds seconds with $threads threads..."

$script = {
    param($url, $durationSeconds)
    $endTime = (Get-Date).AddSeconds($durationSeconds)
    $count = 0
    $client = [System.Net.Http.HttpClient]::new()
    while ((Get-Date) -lt $endTime) {
        try {
            $response = $client.GetAsync($url).GetAwaiter().GetResult()
            if ($response.IsSuccessStatusCode) {
                $count++
            }
        } catch { }
    }
    $client.Dispose()
    return $count
}

$jobs = @()
for ($i = 0; $i -lt $threads; $i++) {
    $jobs += Start-Job -ScriptBlock $script -ArgumentList $url, $durationSeconds
}

Wait-Job $jobs | Out-Null
$results = Receive-Job $jobs

$totalRequests = 0
foreach ($r in $results) {
    $totalRequests += $r
}

$rps = [math]::Round($totalRequests / $durationSeconds)
Write-Host "Total Requests: $totalRequests"
Write-Host "Requests Per Second (RPS): $rps"
