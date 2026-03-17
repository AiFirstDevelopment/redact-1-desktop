using Avalonia;
using FluentAssertions;
using Redact1;

namespace Redact1.Tests;

public class ProgramTests
{
    [Fact]
    public void BuildAvaloniaApp_ReturnsAppBuilder()
    {
        // Use reflection to call the static method
        var programType = typeof(App).Assembly.GetType("Redact1.Program");
        var method = programType!.GetMethod("BuildAvaloniaApp",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        var result = method!.Invoke(null, null);

        result.Should().NotBeNull();
        result.Should().BeOfType<AppBuilder>();
    }
}
