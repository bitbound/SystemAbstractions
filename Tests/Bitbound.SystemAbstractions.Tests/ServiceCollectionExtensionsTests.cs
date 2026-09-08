using Bitbound.SystemAbstractions.FileSystem;
using Bitbound.SystemAbstractions.Processes;
using Bitbound.SystemAbstractions.Windows.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bitbound.SystemAbstractions.Tests;

public class ServiceCollectionExtensionsTests
{
  [Fact]
  public void AddFileSystem_WhenBuilt_ResolvesFileAccessPermissions()
  {
    var permissions = new ServiceCollection()
      .AddFileSystem()
      .BuildServiceProvider()
      .GetRequiredService<IFileAccessPermissions>();

    Assert.NotNull(permissions);
  }

  [Fact]
  public void AddFileSystem_WhenLoggingIsNotRegistered_ResolvesWithANullLogger()
  {
    var fileSystem = new ServiceCollection()
      .AddFileSystem()
      .BuildServiceProvider()
      .GetRequiredService<IFileSystem>();

    Assert.NotNull(fileSystem);
  }

  [Fact]
  public async Task AddFileSystem_WhenOpenGenericLoggingIsRegistered_UsesTheContainerLogger()
  {
    var recorder = new RecordingLogger();
    var services = new ServiceCollection();
    services.AddSingleton(recorder);
    services.AddSingleton(typeof(ILogger<>), typeof(RecordingLogger<>));

    var fileSystem = services.AddFileSystem().BuildServiceProvider().GetRequiredService<IFileSystem>();

    var result = await fileSystem.ResolveFilePath("bitbound-no-such-command-9f3a2b");

    Assert.False(result.IsSuccess);
    Assert.NotEmpty(recorder.Messages);
    Assert.Contains("bitbound-no-such-command-9f3a2b", string.Join(Environment.NewLine, recorder.Messages));
  }

  [Fact]
  public void AddFileSystem_WhenTheCollectionIsNull_ThrowsArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddFileSystem(null!));
    Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddProcesses(null!));
    Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddSystemEnvironment(null!));
    Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddRegistry(null!));
  }

  [Fact]
  public void AddProcesses_WhenBuilt_ResolvesTheSameSingletonEveryTime()
  {
    var provider = new ServiceCollection().AddProcesses().BuildServiceProvider();

    Assert.Same(provider.GetRequiredService<IProcessManager>(), provider.GetRequiredService<IProcessManager>());
  }

  [Fact]
  public void AddRegistry_WhenNotOnWindows_ThrowsPlatformNotSupportedException()
  {
    var services = new ServiceCollection();

    if (OperatingSystem.IsWindows())
    {
      Assert.Same(services, services.AddRegistry());
      Assert.NotNull(services.BuildServiceProvider().GetRequiredService<IRegistryManager>());
    }
    else
    {
      Assert.Throws<PlatformNotSupportedException>(() => services.AddRegistry());
    }
  }

  [Fact]
  public void AddSystemAbstractions_WhenBuiltOnWindows_ResolvesEveryAbstraction()
  {
    var provider = new ServiceCollection().AddSystemAbstractions().BuildServiceProvider();

    Assert.NotNull(provider.GetRequiredService<IFileSystem>());
    Assert.NotNull(provider.GetRequiredService<IFileAccessPermissions>());
    Assert.NotNull(provider.GetRequiredService<IProcessManager>());
    Assert.NotNull(provider.GetRequiredService<ISystemEnvironment>());

    if (OperatingSystem.IsWindows())
    {
      Assert.NotNull(provider.GetRequiredService<IRegistryManager>());
    }
  }

  [Fact]
  public void AddSystemAbstractions_WhenCalled_ReturnsTheSameCollection()
  {
    var services = new ServiceCollection();

    Assert.Same(services, services.AddSystemAbstractions());
  }

  [Fact]
  public void AddSystemAbstractions_WhenTheCollectionIsNull_ThrowsArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddSystemAbstractions(null!));
  }

  [Fact]
  public void AddSystemEnvironment_WhenBuilt_ResolvesTheSameSingletonEveryTime()
  {
    var provider = new ServiceCollection().AddSystemEnvironment().BuildServiceProvider();

    Assert.Same(provider.GetRequiredService<ISystemEnvironment>(), provider.GetRequiredService<ISystemEnvironment>());
  }

  private sealed class RecordingLogger
  {
    public List<string> Messages { get; } = [];

    public void Add(string message)
    {
      lock (Messages)
      {
        Messages.Add(message);
      }
    }
  }
  private sealed class RecordingLogger<T>(RecordingLogger recorder) : ILogger<T>
  {
    public IDisposable? BeginScope<TState>(TState state)
      where TState : notnull
    {
      return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
      return true;
    }

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      recorder.Add(formatter(state, exception));
    }
  }
}
