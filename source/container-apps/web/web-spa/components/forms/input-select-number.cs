#region Purpose
// InputSelect variant that lets forms bind a <select> directly to int-typed model properties.
#endregion

#region Design
// HTML option values arrive as strings and InputSelect's default converter rejects numeric
// targets, so int is parsed explicitly here; every other T delegates to the base converter
// to keep behavior identical for the types the framework already handles.
#endregion

namespace TimeWarp.Architecture.Components;

public class InputSelectNumber<T> : InputSelect<T>
{
  protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out T result, [NotNullWhen(false)] out string? validationErrorMessage)
  {
    if (typeof(T) == typeof(int))
    {
      if (int.TryParse(value, out int resultInt))
      {
        result = (T)(object)resultInt;
        validationErrorMessage = null;
        return true;
      }
      else
      {
        result = default;
        validationErrorMessage = "The chosen value is not a valid number.";
        return false;
      }
    }
    else
    {
      return base.TryParseValueFromString(value, out result, out validationErrorMessage);
    }
  }
}
