namespace DatabaseDecimal;

/// <summary>
/// The backing integer width for a decimal value's mantissa.
/// </summary>
public enum DecimalWidth : byte
{
    W32,
    W64,
    W128,
    W256,
}
