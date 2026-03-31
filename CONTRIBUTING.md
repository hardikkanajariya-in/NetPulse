# Contributing to NetPulse

Thank you for your interest in contributing to NetPulse! This document provides guidelines and information for contributors.

## Getting Started

1. **Fork** the repository on GitHub
2. **Clone** your fork locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/net-pulse.git
   cd net-pulse
   ```
3. **Create a branch** from `develop`:
   ```bash
   git checkout develop
   git checkout -b feature/your-feature-name
   ```
4. **Make your changes**, commit, and push
5. **Open a Pull Request** against the `develop` branch

## Branch Naming

| Prefix | Purpose | Example |
|--------|---------|---------|
| `feature/` | New features | `feature/dark-mode` |
| `fix/` | Bug fixes | `fix/tray-icon-crash` |
| `docs/` | Documentation | `docs/update-readme` |
| `refactor/` | Code refactoring | `refactor/cleanup-services` |

See [Branching Strategy](docs/BRANCHING.md) for the full workflow.

## Commit Messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add dark mode support
fix: handle network adapter disconnect gracefully
docs: update installation instructions
refactor: simplify speed formatting logic
chore: update NuGet packages
```

## Pull Request Guidelines

- Fill out the PR template completely
- Target the `develop` branch (not `main`)
- Keep PRs focused — one feature or fix per PR
- Include screenshots for any UI changes
- Ensure the project builds without errors: `dotnet build`
- Do not include unrelated formatting or refactoring changes

## Code Style

- Follow the project's [.editorconfig](.editorconfig) settings
- Use file-scoped namespaces
- Use `var` when the type is obvious from the right side
- Keep methods short and focused
- No unused `using` directives

## Reporting Issues

- Use the [Bug Report](https://github.com/hardikkanajariya-in/NetPulse/issues/new?template=bug_report.yml) template for bugs
- Use the [Feature Request](https://github.com/hardikkanajariya-in/NetPulse/issues/new?template=feature_request.yml) template for ideas
- Search existing issues before creating a new one
- For large changes, open an issue first to discuss the approach

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to uphold this code.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
