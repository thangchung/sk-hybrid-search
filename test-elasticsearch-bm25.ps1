# ElasticSearch BM25 Provider Test

# This script tests the ElasticSearch BM25 provider functionality

# Prerequisites:
# 1. Docker must be running
# 2. ElasticSearch container must be available

Write-Host "Testing ElasticSearch BM25 Provider..." -ForegroundColor Green

# Test 1: Build the application
Write-Host "`n1. Building the application..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build successful!" -ForegroundColor Green

# Test 2: Start ElasticSearch container (if not already running)
Write-Host "`n2. Starting ElasticSearch container..." -ForegroundColor Yellow
$elasticContainer = docker ps --filter "name=elasticsearch" --format "{{.Names}}"
if (-not $elasticContainer) {
    Write-Host "Starting new ElasticSearch container..."
    docker run -d --name elasticsearch -p 9200:9200 -e "discovery.type=single-node" -e "xpack.security.enabled=false" -e "ES_JAVA_OPTS=-Xms512m -Xmx512m" docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    
    # Wait for ElasticSearch to be ready
    Write-Host "Waiting for ElasticSearch to be ready..."
    $maxRetries = 30
    $retries = 0
    do {
        Start-Sleep -Seconds 2
        $retries++
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:9200/_cluster/health" -Method Get -ErrorAction SilentlyContinue
            if ($response.status -eq "yellow" -or $response.status -eq "green") {
                Write-Host "ElasticSearch is ready!" -ForegroundColor Green
                break
            }
        }
        catch {
            # Continue waiting
        }
        Write-Host "Still waiting... ($retries/$maxRetries)"
    } while ($retries -lt $maxRetries)
    
    if ($retries -eq $maxRetries) {
        Write-Host "ElasticSearch failed to start!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "ElasticSearch container is already running: $elasticContainer" -ForegroundColor Green
}

# Test 3: Test with in-memory BM25 (default)
Write-Host "`n3. Testing with in-memory BM25..." -ForegroundColor Yellow
$env:ASPNETCORE_URLS = "http://localhost:5000"
Start-Process -FilePath "dotnet" -ArgumentList "run --project HydeSearch" -NoNewWindow -PassThru
Start-Sleep -Seconds 5

try {
    # Test health endpoint
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/health" -Method Get
    Write-Host "Health check passed: $($healthResponse.status)" -ForegroundColor Green
    
    # Add a test document
    $document = @{
        title = "Machine Learning Basics"
        content = "Machine learning is a subset of artificial intelligence that focuses on algorithms and statistical models."
        metadata = @{}
    } | ConvertTo-Json
    
    $addResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/documents" -Method Post -Body $document -ContentType "application/json"
    Write-Host "Document added: $($addResponse.id)" -ForegroundColor Green
    
    # Test search
    $searchRequest = @{
        query = "artificial intelligence"
        maxResults = 5
    } | ConvertTo-Json
    
    $searchResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/search/hybrid" -Method Post -Body $searchRequest -ContentType "application/json"
    Write-Host "Hybrid search returned $($searchResponse.results.Count) results" -ForegroundColor Green
}
catch {
    Write-Host "Error testing in-memory BM25: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    # Stop the application
    Get-Process -Name "dotnet" | Where-Object { $_.MainWindowTitle -eq "" } | Stop-Process -Force -ErrorAction SilentlyContinue
}

Write-Host "`n4. Testing with ElasticSearch BM25..." -ForegroundColor Yellow

# Copy the ElasticSearch configuration
Copy-Item "HydeSearch\appsettings.elasticsearch.json" "HydeSearch\appsettings.Development.json" -Force

# Start the application with ElasticSearch configuration
Start-Process -FilePath "dotnet" -ArgumentList "run --project HydeSearch" -NoNewWindow -PassThru
Start-Sleep -Seconds 5

try {
    # Test health endpoint
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/health" -Method Get
    Write-Host "Health check passed with ElasticSearch: $($healthResponse.status)" -ForegroundColor Green
    
    # Add a test document
    $document = @{
        title = "ElasticSearch Testing"
        content = "ElasticSearch is a distributed search and analytics engine built on Apache Lucene."
        metadata = @{}
    } | ConvertTo-Json
    
    $addResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/documents" -Method Post -Body $document -ContentType "application/json"
    Write-Host "Document added to ElasticSearch: $($addResponse.id)" -ForegroundColor Green
    
    # Test search
    $searchRequest = @{
        query = "distributed search"
        maxResults = 5
    } | ConvertTo-Json
    
    $searchResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/search/hybrid" -Method Post -Body $searchRequest -ContentType "application/json"
    Write-Host "ElasticSearch hybrid search returned $($searchResponse.results.Count) results" -ForegroundColor Green
    
    Write-Host "`nElasticSearch BM25 provider test completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error testing ElasticSearch BM25: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    # Stop the application
    Get-Process -Name "dotnet" | Where-Object { $_.MainWindowTitle -eq "" } | Stop-Process -Force -ErrorAction SilentlyContinue
    
    # Restore original configuration
    if (Test-Path "HydeSearch\appsettings.json.backup") {
        Move-Item "HydeSearch\appsettings.json.backup" "HydeSearch\appsettings.Development.json" -Force
    }
}

Write-Host "`nTo use ElasticSearch BM25 provider:" -ForegroundColor Cyan
Write-Host "1. Set BM25.Provider to 'ElasticSearch' in appsettings.json" -ForegroundColor White
Write-Host "2. Configure the ElasticSearch connection string" -ForegroundColor White
Write-Host "3. Ensure ElasticSearch is running and accessible" -ForegroundColor White
