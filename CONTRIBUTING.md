# Contributing to Windows Optimizer

Thank you for your interest in contributing! This document provides guidelines and instructions.

## Code of Conduct

- Be respectful and inclusive
- Provide constructive feedback
- Focus on code quality and maintainability

## How to Contribute

### 1. Fork and Clone

```bash
git clone https://github.com/siklone/WPF-Windows-optimizer-with-safe-reversible-tweaks.git
cd WPF-Windows-optimizer-with-safe-reversible-tweaks
```

### 2. Create a Branch

```bash
git checkout -b feature/my-new-feature
# or
git checkout -b fix/issue-123
```

### 3. Make Changes

- Write clean, readable code
- Follow existing code style
- Add comments for complex logic
- Update documentation if needed

### 4. Test Your Changes

```bash
dotnet build
dotnet test
# Manual testing recommended
```

### 5. Commit

Use conventional commit messages:

```
feat: Add new tweak provider
fix: Resolve registry access issue
docs: Update README with examples
refactor: Improve provider pattern
test: Add unit tests for TweakExecutionPipeline
```

### 6. Push and Create PR

```bash
git push origin feature/my-new-feature
```

Then create a Pull Request on GitHub.

## Development Setup

### Prerequisites

- Visual Studio 2022 or VS Code
- .NET 8.0 SDK
- Windows 10/11
- Git

### Build

```powershell
dotnet restore
dotnet build
```

### Run

```powershell
dotnet run --project WindowsOptimizer.App
```

### Debug

Open `WindowsOptimizerSuite.slnx` in Visual Studio and press F5.

> Tip: If you run from `dotnet run` and the app can't find the ElevatedHost binary, set the env var:
> `WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH=C:\\path\\to\\WindowsOptimizer.ElevatedHost.exe`

## Adding New Tweaks

### Option 1: Add to Existing Provider

Edit the appropriate provider in `WindowsOptimizer.App/Services/TweakProviders/`:

> Note: `LegacyTweakProvider` is temporary to restore missing tweaks. Do not add new tweaks there.

```csharp
public class PrivacyTweakProvider : BaseTweakProvider
{
    public override IEnumerable<ITweak> CreateTweaks(...)
    {
        return new List<ITweak>
        {
            // Existing tweaks...

            // Add your new tweak:
            CreateRegistryTweak(
                context,
                "privacy.my-new-tweak",
                "My New Privacy Tweak",
                "Description of what this does",
                TweakRiskLevel.Safe,
                RegistryHive.CurrentUser,
                @"Software\Microsoft\...",
                "ValueName",
                RegistryValueKind.DWord,
                1,
                requiresElevation: false)
        };
    }
}
```

### Option 2: Create New Provider

1. Create new file in `WindowsOptimizer.App/Services/TweakProviders/`:

```csharp
public sealed class MyTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "My Category";

    public override IEnumerable<ITweak> CreateTweaks(
        TweakExecutionPipeline pipeline,
        TweakContext context,
        bool isElevated)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(...)
        };
    }
}
```

2. Register in `MainViewModel.cs`:

```csharp
var providers = new ITweakProvider[]
{
    // Existing providers...
    new MyTweakProvider()  // Add here
};
```

### Option 3: Create Plugin

See `WindowsOptimizer.Plugins.HelloWorld` for a complete example.

## Tweak Guidelines

### Tweak IDs
- Format: `category.descriptive-name`
- Examples: `privacy.disable-telemetry`, `network.optimize-smb`
- Use lowercase with hyphens

### Descriptions
- Be clear and concise
- Explain what the tweak does (not how)
- Mention potential side effects
- Example: "Disables Windows telemetry data collection"

### Risk Levels
- **Safe**: No side effects, recommended for all
- **Advanced**: May affect functionality, understand first
- **Risky**: Only for advanced users, may cause issues

### Registry Changes
- Always specify correct `RegistryHive`
- Use `RegistryValueKind` appropriate for value type
- Set `requiresElevation: false` for CurrentUser changes
- Test both apply and rollback

## Code Style

### C# Conventions
- Follow Microsoft C# coding conventions
- Use `var` for obvious types
- Prefer expression-bodied members where appropriate
- Use nullable reference types (`string?`)

### Naming
- PascalCase for classes, methods, properties
- camelCase for local variables, parameters
- _camelCase for private fields
- SCREAMING_SNAKE_CASE for constants

### Comments
```csharp
// Good: Explain why, not what
// Disable paging to keep kernel in RAM for better performance
DisablePagingExecutive = 1;

// Bad: Explain what (obvious from code)
// Set value to 1
value = 1;
```

## Testing

### Unit Tests
Create tests in `WindowsOptimizer.Tests`:

```csharp
[Fact]
public void TweakExecutionPipeline_Should_Execute_All_Steps()
{
    // Arrange
    var tweak = new MockTweak();
    var pipeline = new TweakExecutionPipeline(logger, logStore);

    // Act
    var result = await pipeline.ExecuteAsync(tweak);

    // Assert
    Assert.True(result.Applied);
}
```

### Integration Tests
- Test registry operations with temp keys
- Mock external dependencies
- Use `[Theory]` for data-driven tests

### Manual Testing
Before submitting PR:
1. Build solution
2. Run application
3. Test your tweak:
   - Detect works correctly
   - Apply succeeds
   - Verify confirms
   - Rollback undoes changes
4. Check logs for errors
5. Test on fresh Windows install if possible

## Documentation

### Code Documentation
- XML comments for public APIs
- README updates for new features
- Architecture docs for design changes

### Inline Comments
- Explain complex algorithms
- Document workarounds
- Link to related issues/PRs

## Pull Request Checklist

- [ ] Code builds without errors
- [ ] All tests pass
- [ ] New tests added for new features
- [ ] Documentation updated
- [ ] Commit messages follow convention
- [ ] No unnecessary file changes
- [ ] Code formatted consistently
- [ ] No commented-out code
- [ ] No debug statements (Console.WriteLine, etc.)

## Review Process

1. **Automated Checks**: CI/CD runs build and tests
2. **Code Review**: Maintainer reviews code quality
3. **Testing**: Manual testing if needed
4. **Merge**: PR merged to main branch

## Questions?

- 🐛 **Bug Reports**: GitHub Issues
- 💡 **Feature Requests**: GitHub Discussions
- 💬 **General Questions**: GitHub Discussions
- 📧 **Private Inquiries**: maintainer@example.com

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Recognition

Contributors will be acknowledged in:
- README.md (Contributors section)
- Release notes
- Commit history

Thank you for contributing! 🎉
