# Test script for Hybrid Search API
# This script tests the BM25 + HyDE hybrid search functionality

Write-Host "🔍 Testing Hybrid Search API (BM25 + HyDE)" -ForegroundColor Cyan
Write-Host "=" * 50

# Test data
$testQueries = @(
    "machine learning algorithms",
    "data structures",
    "cloud computing",
    "artificial intelligence",
    "web development"
)

# Get the HydeSearch service URL from Aspire (typically http://localhost:5000 or similar)
# For now, let's assume it's running on a standard port
$baseUrl = "http://localhost:5000" # This will be the actual port from Aspire

foreach ($query in $testQueries) {
    Write-Host ""
    Write-Host "🔍 Testing query: '$query'" -ForegroundColor Yellow
    Write-Host "-" * 30
    
    # Test the hybrid search endpoint
    $hybridRequest = @{
        query = $query
        limit = 3
        includeScores = $true
    } | ConvertTo-Json
    
    try {
        Write-Host "📊 Hybrid Search Results:" -ForegroundColor Green
        $response = Invoke-RestMethod -Uri "$baseUrl/api/search/hybrid" -Method POST -Body $hybridRequest -ContentType "application/json"
        
        Write-Host "Results found: $($response.results.Count)"
        Write-Host "BM25 Weight: $($response.searchMetrics.bm25Weight)"
        Write-Host "HyDE Weight: $($response.searchMetrics.hydeWeight)"
        Write-Host "Normalization: $($response.searchMetrics.normalizationStrategy)"
        
        foreach ($result in $response.results) {
            Write-Host "  📄 $($result.document.title)" -ForegroundColor White
            Write-Host "     Combined Score: $([math]::Round($result.combinedScore, 4))" -ForegroundColor Cyan
            Write-Host "     BM25 Score: $([math]::Round($result.bm25Score, 4))" -ForegroundColor Magenta
            Write-Host "     HyDE Score: $([math]::Round($result.hydeScore, 4))" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "❌ Error testing hybrid search: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "✅ Hybrid Search API testing completed!" -ForegroundColor Green
