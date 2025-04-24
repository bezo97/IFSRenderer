#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

using IFSEngine.Model;

using WpfDisplay.Serialization;

namespace WpfDisplay.Helper;

internal static class PngMetadataHelper
{
    public static BitmapMetadata CreateMetadataFromParams(IFS ifs, bool includeSerializedParams)
    {
        //prepare fields
        var authorNames = ifs.Authors.Select(author => author.Name).ToHashSet();
        string[] keywords = [
            "IFSRenderer",
            "IFS",
            "fractal",
            ifs.Title,
            .. authorNames,
            .. ifs.Iterators.Select(i => i.Transform.Name).Distinct(),
            ifs.Palette.Name];
        //optionally include params in the description field
        var serializedParams = includeSerializedParams ? IfsNodesSerializer.SerializeJsonString(ifs) : "";

        //serialize XMP metadata
        var xmpMetadata = XMPMetadataSerializer.Serialize(
            title: ifs.Title,
            authors: authorNames,
            description: serializedParams,
            subject: keywords);

        //embed serialized XMP metadata in PNG
        var metadata = new BitmapMetadata("png");
        metadata.SetQuery("/iTXt/Keyword", "XML:com.adobe.xmp".ToCharArray());
        metadata.SetQuery("/iTXt/TextEntry", xmpMetadata);

        //embed params as comment metadata in PNG
        metadata.SetQuery("/tEXt/Comment", serializedParams);

        return metadata;
    }

    public static bool TryExtractParamsFromImage(BitmapSource png, IEnumerable<Transform> transforms, IEnumerable<PostFx> postfxs, out IFS embeddedParams)
    {
        try
        {
            var serializedParams = (string)((BitmapMetadata)png.Metadata).GetQuery("/tEXt/Comment");
            embeddedParams = IfsNodesSerializer.DeserializeJsonString(serializedParams, transforms, postfxs, true);
            return true;
        }
        catch
        {
            embeddedParams = null!;
            return false;
        }
    }
}
