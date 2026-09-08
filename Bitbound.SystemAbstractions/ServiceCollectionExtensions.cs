using Bitbound.SystemAbstractions.FileSystem;
using Bitbound.SystemAbstractions.Processes;
using Bitbound.SystemAbstractions.Windows.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bitbound.SystemAbstractions;

/// <summary>
/// Registers the operating system abstractions on an <see cref="IServiceCollection"/>.
/// Implementations are internal, so these extensions are the intended way to obtain them.
/// </summary>
public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Registers <see cref="IFileSystem"/> and <see cref="IFileAccessPermissions"/>.
  /// </summary>
  /// <param name="services">The service collection to add to.</param>
  /// <returns>The same <paramref name="services"/> instance, so calls can be chained.</returns>
  public static IServiceCollection AddFileSystem(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddSingleton<IFileSystem>(serviceProvider => new FileSystem.FileSystem(
      ResolveLogger<FileSystem.FileSystem>(serviceProvider)));
    services.AddSingleton<IFileAccessPermissions, FileAccessPermissions>();

    return services;
  }

  /// <summary>
  /// Registers <see cref="IProcessManager"/>.
  /// </summary>
  /// <param name="services">The service collection to add to.</param>
  /// <returns>The same <paramref name="services"/> instance, so calls can be chained.</returns>
  public static IServiceCollection AddProcesses(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddSingleton<IProcessManager, ProcessManager>();

    return services;
  }

  /// <summary>
  /// Registers <see cref="IRegistryManager"/>.
  /// </summary>
  /// <param name="services">The service collection to add to.</param>
  /// <returns>The same <paramref name="services"/> instance, so calls can be chained.</returns>
  /// <exception cref="PlatformNotSupportedException">Thrown when the current operating system is not Windows.</exception>
  public static IServiceCollection AddRegistry(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    if (!OperatingSystem.IsWindows())
    {
      throw new PlatformNotSupportedException("The Windows registry is only available on Windows.");
    }

    services.AddSingleton<IRegistryManager, RegistryManager>();

    return services;
  }

  /// <summary>
  /// Registers every operating system abstraction whose implementation supports the current operating system.
  /// The Windows registry services are only registered when running on Windows.
  /// </summary>
  /// <param name="services">The service collection to add to.</param>
  /// <returns>The same <paramref name="services"/> instance, so calls can be chained.</returns>
  public static IServiceCollection AddSystemAbstractions(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddFileSystem();
    services.AddProcesses();
    services.AddSystemEnvironment();

    if (OperatingSystem.IsWindows())
    {
      services.AddRegistry();
    }

    return services;
  }

  /// <summary>
  /// Registers <see cref="ISystemEnvironment"/>.
  /// </summary>
  /// <param name="services">The service collection to add to.</param>
  /// <returns>The same <paramref name="services"/> instance, so calls can be chained.</returns>
  public static IServiceCollection AddSystemEnvironment(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddSingleton<ISystemEnvironment, SystemEnvironment>();

    return services;
  }

  // Logging is resolved opportunistically so these services work in containers that have no logging registered.
  // Registering ILogger<> outright would compete with the application's own logging configuration.
  private static ILogger<T> ResolveLogger<T>(IServiceProvider serviceProvider)
  {
    return serviceProvider.GetService<ILogger<T>>() ?? NullLogger<T>.Instance;
  }
}
