# PowerShell script to test fraud detection API
$headers = @{
    "Authorization" = "Bearer sesoqefHnKglaJKJwRtE4DZW6aqDLGxNRRu/qhiCUug="
    "Content-Type" = "application/json"
}

$body = @{
    model = "fraud-detector:stable"
    messages = @(
        @{
            role = "user"
            content = "User Profile Summary:`n- Regular grocery trader.`n- Today made 5 transactions.`n`nTransaction Context:`n- Amount Risk Score: 3`n- Amount Z-Score: 1.5`n- High Amount Flag: No`n`nTransaction Details:`n- CNIC: CN421012345678`n- FromAccount: 1063123456789`n- FromName: HUSSAIN TRADERS`n- ToAccount: PK45HBL001234567890123`n- ToName: K-ELECTRIC`n- Amount: 25000`n- ActivityCode: Bill Payment`n- UserType: MOBILE`n- ToBank: HABLPKKA001`n- TransactionComments: Electricity Bill`n- TransactionDateTime: 30/08/2025, 07:25:00`n- UserId: user12345`n- TransactionId: TXN20250830001"
        }
    )
    stream = $false
} | ConvertTo-Json -Depth 10

Write-Host "Testing Fraud Detection API..."
Write-Host "URL: https://10.10.110.107:443/validate-chat"
Write-Host "Bearer Token: sesoqefHnKglaJKJwRtE4DZW6aqDLGxNRRu/qhiCUug="
Write-Host ""

try {
    # Skip SSL certificate validation for testing
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls13
    
    $response = Invoke-RestMethod -Uri "https://10.10.110.107:443/validate-chat" -Method POST -Headers $headers -Body $body -TimeoutSec 30
    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 5
} catch {
    Write-Host "ERROR!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        Write-Host "Status Description: $($_.Exception.Response.StatusDescription)" -ForegroundColor Red
    }
}
