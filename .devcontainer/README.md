# HyDE Search Development Container

This devcontainer provides a complete .NET 9 development environment with Docker-in-Docker support for containerized development workflows.

## Features

### Core Development Environment
- **.NET 9 SDK** - Latest .NET runtime and development tools
- **C# Dev Kit** - Enhanced C# development experience in VS Code
- **Docker-in-Docker** - Full Docker support within the container
- **Git & GitHub CLI** - Version control and GitHub integration
- **Zsh with Oh My Zsh** - Enhanced shell experience
- **PowerShell** - Cross-platform PowerShell support

### Docker Integration
- **Docker Engine** - Full Docker daemon running inside the container
- **Docker Buildx** - Advanced build capabilities with BuildKit
- **Docker Compose v2** - Multi-container application management
- **VS Code Docker Extension** - Visual Docker management and debugging

## Quick Start

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (with privileged container support)
- [VS Code](https://code.visualstudio.com/)
- [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)

### Launch Container
1. Open the project folder in VS Code
2. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
3. Select "Dev Containers: Reopen in Container"
4. Wait for the container to build and start (first time may take several minutes)

### Verify Setup
Once the container is running, open a terminal and run:
```bash
# Test the devcontainer setup
./test-devcontainer.sh
```

## Docker-in-Docker Capabilities

### Build Your Application
```bash
# Build your HyDE Search app as a Docker image
docker build -t hydesearch:latest .

# Run the containerized application
docker run --rm -p 5000:5000 -p 5001:5001 hydesearch:latest
```

### Multi-Service Development
```bash
# Create a docker-compose.yml for your services
# Then start your application stack
docker compose up --build

# View running services
docker compose ps

# Stop services
docker compose down
```

### Container Registry Operations
```bash
# Tag and push to registry
docker tag hydesearch:latest your-registry.com/hydesearch:dev
docker push your-registry.com/hydesearch:dev
```

## VS Code Integration

### Extensions Included
- **ms-dotnettools.csharp** - C# language support
- **ms-dotnettools.csdevkit** - Enhanced C# development kit
- **ms-azuretools.vscode-docker** - Docker management and debugging
- **GitHub.copilot** - AI-powered code completion (if available)

### Port Forwarding
The following ports are automatically forwarded:
- `5000`, `5001` - ASP.NET Core default ports
- `8080`, `8081` - Alternative web server ports

### Tasks
Use VS Code tasks for common operations:
- `Ctrl+Shift+P` → "Tasks: Run Task"
- Available tasks: build, run, clean, docker-build, docker-run

## Performance Optimizations

### Persistent Volumes
- **NuGet Cache**: `~/.nuget` - Faster package restoration
- **VS Code Extensions**: `~/.vscode-server/extensions` - Preserves extensions
- **Docker Data**: `/var/lib/docker` - Maintains Docker images/containers

### Environment Variables
- `DOTNET_CLI_TELEMETRY_OPTOUT=1` - Disables .NET telemetry
- `DOTNET_NOLOGO=1` - Removes .NET startup logo
- `DOCKER_BUILDKIT=1` - Enables faster Docker builds
- `ASPNETCORE_ENVIRONMENT=Development` - Sets development mode

## Security Considerations

### Privileged Mode
This container runs in **privileged mode** to support Docker-in-Docker. This is required for:
- Docker daemon access
- Container networking
- Volume mounting for nested containers

**⚠️ Important**: Only use this container in trusted development environments.

### Best Practices
1. Don't run untrusted containers in this environment
2. Regularly clean up unused Docker resources
3. Use specific image tags rather than `latest` in production scenarios
4. Be mindful of port mappings and network security

## Troubleshooting

### Docker Issues
**Docker daemon not responding:**
```bash
# Check Docker service
sudo service docker status

# Restart if needed
sudo service docker restart
```

**Permission errors:**
```bash
# Add user to docker group (may require container restart)
sudo usermod -aG docker $USER
```

### Performance Issues
**Slow startup:**
- First startup downloads base images (normal)
- Subsequent starts use cached volumes
- Consider allocating more memory to Docker Desktop (6GB+ recommended)

**High resource usage:**
```bash
# Monitor Docker resource usage
docker stats

# Clean up unused resources
docker system prune -f
```

### Common Problems
**Port conflicts:**
- Check if ports 5000, 5001, 8080, 8081 are available
- Modify `forwardPorts` in `devcontainer.json` if needed

**Build failures:**
- Ensure Dockerfile is properly configured
- Check .dockerignore excludes unnecessary files
- Verify all dependencies are included

## Advanced Usage

### Custom Docker Configuration
Create a custom Docker daemon configuration if needed:
```json
// .docker/daemon.json
{
  "experimental": true,
  "features": {
    "buildkit": true
  }
}
```

### Adding More Development Tools
Extend the devcontainer by modifying `devcontainer.json`:
```json
"features": {
  "ghcr.io/devcontainers/features/kubectl-helm-minikube:1": {},
  "ghcr.io/devcontainers/features/azure-cli:1": {}
}
```

## Getting Help

1. **Documentation**: Check the official [Dev Containers documentation](https://code.visualstudio.com/docs/devcontainers/containers)
2. **Docker Issues**: Consult [Docker documentation](https://docs.docker.com/)
3. **VS Code**: Use the built-in help or visit [VS Code documentation](https://code.visualstudio.com/docs)

Happy coding with your containerized HyDE Search development environment! 🚀
