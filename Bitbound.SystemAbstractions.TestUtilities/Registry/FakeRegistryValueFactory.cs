using System.Globalization;

using Bitbound.SystemAbstractions.Windows.Registry;

namespace Bitbound.SystemAbstractions.TestUtilities.Registry;

/// <summary>
/// Applies the same value-kind inference and storage rules the Windows registry uses, so a value written through
/// <see cref="FakeRegistryKey"/> reads back the same way it would from the real registry.
/// </summary>
internal static class FakeRegistryValueFactory
{
  public static FakeRegistryValue Create(object value, RegistryValueKind? requestedKind)
  {
    ArgumentNullException.ThrowIfNull(value);

    if (value is Array && value is not byte[] and not string[])
    {
      // The real registry throws without a parameter name, so the message matches it verbatim.
      throw new ArgumentException(
        $"RegistryKey.SetValue does not support arrays of type '{value.GetType().Name}'. " +
        "Only Byte[] and String[] are supported.");
    }

    var kind = requestedKind ?? InferKind(value);

    return kind switch
    {
      RegistryValueKind.DWord => new FakeRegistryValue(Convert.ToInt32(value, CultureInfo.InvariantCulture), kind),
      RegistryValueKind.QWord => new FakeRegistryValue(Convert.ToInt64(value, CultureInfo.InvariantCulture), kind),
      RegistryValueKind.Binary => new FakeRegistryValue((byte[])value, kind),
      RegistryValueKind.MultiString => new FakeRegistryValue((string[])value, kind),
      RegistryValueKind.ExpandString => new FakeRegistryValue(ToStringValue(value), kind),
      RegistryValueKind.Unknown => new FakeRegistryValue(ToStringValue(value), RegistryValueKind.String),
      _ => new FakeRegistryValue(ToStringValue(value), RegistryValueKind.String)
    };
  }

  public static object ReadValue(FakeRegistryValue value)
  {
    return value.Kind == RegistryValueKind.ExpandString
      ? Environment.ExpandEnvironmentVariables((string)value.Value)
      : value.Value;
  }

  private static RegistryValueKind InferKind(object value)
  {
    // The real registry only special-cases int, string, byte[], and string[]. Everything else is stored as a string.
    return value switch
    {
      string => RegistryValueKind.String,
      int => RegistryValueKind.DWord,
      byte[] => RegistryValueKind.Binary,
      string[] => RegistryValueKind.MultiString,
      _ => RegistryValueKind.String
    };
  }

  private static string ToStringValue(object value)
  {
    return value switch
    {
      string stringValue => stringValue,
      IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
      _ => value.ToString() ?? string.Empty
    };
  }
}
