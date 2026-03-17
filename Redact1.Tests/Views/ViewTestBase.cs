using Avalonia;
using Avalonia.Headless;
using Redact1;

[assembly: AvaloniaTestApplication(typeof(Redact1.Tests.Views.TestAppBuilder))]

namespace Redact1.Tests.Views;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions())
        .WithInterFont();
}
