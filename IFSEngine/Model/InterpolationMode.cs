namespace IFSEngine.Model;

/// <summary>
/// The method used to interpolate between color keys in a palette gradient.
/// </summary>
public enum InterpolationMode
{
    /// <summary>
    /// Simple linear interpolation in RGB space.
    /// </summary>
    LinearRGB,

    /// <summary>
    /// Decode colors to linear space using the sRGB gamma curve,
    /// interpolate linearly, then re-encode. Produces smoother transitions than raw RGB.
    /// </summary>
    Srgb,

    /// <summary>
    /// Pigment-based color mixing using the Mixbox library (simulates paint mixing).
    /// </summary>
    Mixbox
}
