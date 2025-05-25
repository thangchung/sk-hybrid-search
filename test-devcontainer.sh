#!/bin/bash

# DevContainer Test Script for HyDE Search
# This script tests the devcontainer setup and Docker-in-Docker functionality

echo "🚀 Testing HyDE Search DevContainer Setup"
echo "=========================================="
echo

# Test 1: .NET Environment
echo "1. Testing .NET Environment..."
dotnet --version
if [ $? -eq 0 ]; then
    echo "✅ .NET SDK is available"
else
    echo "❌ .NET SDK not found"
    exit 1
fi
echo

# Test 2: Docker Installation
echo "2. Testing Docker Installation..."
docker --version
if [ $? -eq 0 ]; then
    echo "✅ Docker CLI installed"
else
    echo "❌ Docker CLI not available"
    exit 1
fi
echo

# Test 3: Docker Daemon
echo "3. Testing Docker Daemon..."
docker info > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Docker daemon is running"
else
    echo "⚠️  Docker daemon not running, attempting to start..."
    sudo service docker start
    sleep 5
    docker info > /dev/null 2>&1
    if [ $? -eq 0 ]; then
        echo "✅ Docker daemon started successfully"
    else
        echo "❌ Failed to start Docker daemon"
        exit 1
    fi
fi
echo

# Test 4: Docker Compose
echo "4. Testing Docker Compose..."
docker compose version
if [ $? -eq 0 ]; then
    echo "✅ Docker Compose v2 installed"
else
    echo "❌ Docker Compose not available"
    exit 1
fi
echo

# Test 5: Project Build
echo "5. Testing Project Build..."
dotnet build --verbosity quiet
if [ $? -eq 0 ]; then
    echo "✅ Project builds successfully"
else
    echo "❌ Project build failed"
    exit 1
fi
echo

# Test 6: Simple Docker Test
echo "6. Testing Docker Run..."
docker run --rm hello-world > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Docker container execution works"
else
    echo "❌ Docker container execution failed"
    exit 1
fi
echo

# Test 7: Git Configuration
echo "7. Testing Git Configuration..."
git config --global user.name "DevContainer Test" 2>/dev/null
git config --global user.email "test@devcontainer.local" 2>/dev/null
git status > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Git is working"
else
    echo "⚠️  Git status check (may be normal if not in a git repo)"
fi
echo

# Test 8: GitHub CLI (if available)
echo "8. Testing GitHub CLI..."
gh --version > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ GitHub CLI installed"
else
    echo "⚠️  GitHub CLI not available (optional)"
fi
echo

# Test 9: Docker BuildKit
echo "9. Testing Docker BuildKit..."
DOCKER_BUILDKIT=1 docker build --help | grep -q "buildkit" 2>/dev/null
if [ $? -eq 0 ]; then
    echo "✅ Docker BuildKit available"
else
    echo "⚠️  Docker BuildKit may not be enabled"
fi
echo

# Test 10: Environment Variables
echo "10. Testing Environment Variables..."
if [ "$DOTNET_CLI_TELEMETRY_OPTOUT" = "1" ] && [ "$DOTNET_NOLOGO" = "1" ]; then
    echo "✅ .NET environment variables configured"
else
    echo "⚠️  Some .NET environment variables not set"
fi
echo

echo "🎉 DevContainer basic functionality verified!"
echo
echo "Next steps to test full functionality:"
echo "  • Create a simple Dockerfile for your app"
echo "  • Test 'docker build' with your project"
echo "  • Test 'docker compose' for multi-service scenarios"
echo "  • Access your app at forwarded ports (5000, 5001, 8080, 8081)"
