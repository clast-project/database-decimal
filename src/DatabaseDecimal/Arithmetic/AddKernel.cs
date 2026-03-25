using DatabaseDecimal.Values;

namespace DatabaseDecimal.Arithmetic;

/// <summary>
/// Performs addition and subtraction on fixed-point decimal values.
/// Both operands are rescaled to the result scale before the integer operation.
/// </summary>
public static class AddKernel
{
    public static Decimal32 Add(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType)
    {
        int l = ScaleHelper.Rescale32(left.Mantissa, leftType.Scale, resultType.Scale);
        int r = ScaleHelper.Rescale32(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal32(checked(l + r));
    }

    public static Decimal64 Add(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType)
    {
        long l = ScaleHelper.Rescale64(left.Mantissa, leftType.Scale, resultType.Scale);
        long r = ScaleHelper.Rescale64(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal64(checked(l + r));
    }

    public static Decimal128 Add(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType)
    {
        Int128 l = ScaleHelper.Rescale128(left.Mantissa, leftType.Scale, resultType.Scale);
        Int128 r = ScaleHelper.Rescale128(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal128(checked(l + r));
    }

    /// <summary>
    /// Add two 32-bit values, widening to 64-bit result.
    /// Used when the result precision exceeds 9 digits.
    /// </summary>
    public static Decimal64 AddWiden(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType)
    {
        long l = ScaleHelper.Widen32To64(left.Mantissa, leftType.Scale, resultType.Scale);
        long r = ScaleHelper.Widen32To64(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal64(checked(l + r));
    }

    /// <summary>
    /// Add two 64-bit values, widening to 128-bit result.
    /// </summary>
    public static Decimal128 AddWiden(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType)
    {
        Int128 l = ScaleHelper.Widen64To128(left.Mantissa, leftType.Scale, resultType.Scale);
        Int128 r = ScaleHelper.Widen64To128(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal128(checked(l + r));
    }

    // Subtraction mirrors addition with a sign flip

    public static Decimal32 Subtract(Decimal32 left, DecimalType leftType, Decimal32 right, DecimalType rightType, DecimalType resultType)
    {
        int l = ScaleHelper.Rescale32(left.Mantissa, leftType.Scale, resultType.Scale);
        int r = ScaleHelper.Rescale32(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal32(checked(l - r));
    }

    public static Decimal64 Subtract(Decimal64 left, DecimalType leftType, Decimal64 right, DecimalType rightType, DecimalType resultType)
    {
        long l = ScaleHelper.Rescale64(left.Mantissa, leftType.Scale, resultType.Scale);
        long r = ScaleHelper.Rescale64(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal64(checked(l - r));
    }

    public static Decimal128 Subtract(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType)
    {
        Int128 l = ScaleHelper.Rescale128(left.Mantissa, leftType.Scale, resultType.Scale);
        Int128 r = ScaleHelper.Rescale128(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal128(checked(l - r));
    }

    // --- 256-bit ---

    public static Decimal256 Add(Decimal256 left, DecimalType leftType, Decimal256 right, DecimalType rightType, DecimalType resultType)
    {
        Int256 l = ScaleHelper.Rescale256(left.Mantissa, leftType.Scale, resultType.Scale);
        Int256 r = ScaleHelper.Rescale256(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal256(checked(l + r));
    }

    /// <summary>
    /// Add two 128-bit values, widening to 256-bit result.
    /// </summary>
    public static Decimal256 AddWiden(Decimal128 left, DecimalType leftType, Decimal128 right, DecimalType rightType, DecimalType resultType)
    {
        Int256 l = ScaleHelper.Widen128To256(left.Mantissa, leftType.Scale, resultType.Scale);
        Int256 r = ScaleHelper.Widen128To256(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal256(checked(l + r));
    }

    public static Decimal256 Subtract(Decimal256 left, DecimalType leftType, Decimal256 right, DecimalType rightType, DecimalType resultType)
    {
        Int256 l = ScaleHelper.Rescale256(left.Mantissa, leftType.Scale, resultType.Scale);
        Int256 r = ScaleHelper.Rescale256(right.Mantissa, rightType.Scale, resultType.Scale);
        return new Decimal256(checked(l - r));
    }
}
