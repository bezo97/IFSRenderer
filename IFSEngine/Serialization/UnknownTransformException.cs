using System;

namespace IFSEngine.Serialization;

public class UnknownTransformException : Exception
{
    public string TransformName { get; }
    public string TransformVersion { get; }

    public UnknownTransformException(string transformName, string transformVersion)
        : base($"The Transform '{transformName}' (Version: {transformVersion}) is unknown.")
    {
        TransformName = transformName;
        TransformVersion = transformVersion;
    }

}
