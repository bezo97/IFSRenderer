using System;

namespace IFSEngine.Serialization;

public class UnknownPluginException : Exception
{
    public string PluginName { get; }
    public string PluginVersion { get; }

    public UnknownPluginException(string pluginName, string pluginVersion)
        : base($"The Plugin '{pluginName}' (Version: {pluginVersion}) is unknown.")
    {
        PluginName = pluginName;
        PluginVersion = pluginVersion;
    }

}
