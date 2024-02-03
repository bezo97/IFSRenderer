using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfDisplay.Serialization;

/// <summary>
/// Creates an xmp metadata document with the given parameters using a template.
/// It only supports a fixed set of xmp fields that IFSRenderer uses.
/// </summary>
public static class XMPMetadataSerializer
{
    public static string Serialize(string title, IEnumerable<string> authors, string description, IEnumerable<string> subject)
    {
        var creatorTool = $"IFSRenderer {App.VersionString}";
        var currenDate = DateTime.Now;
        var format = "image/png";
        var copyrightNotice = $"Copyright © {currenDate.Year} - {string.Join(", ", authors)}";
        var documentId = Guid.NewGuid();

        return
$"""
<x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="Adobe XMP Core 9.0-c001 79.14ecb42, 2022/12/02-19:12:44        ">
    <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
        <rdf:Description rdf:about=""
            xmlns:xmp="http://ns.adobe.com/xap/1.0/"
            xmlns:dc="http://purl.org/dc/elements/1.1/"
            xmlns:xmpRights="http://ns.adobe.com/xap/1.0/rights/"
            xmlns:xmpMM="http://ns.adobe.com/xap/1.0/mm/"
            xmlns:stEvt="http://ns.adobe.com/xap/1.0/sType/ResourceEvent#">
            <xmp:CreatorTool>{creatorTool}</xmp:CreatorTool>
            <xmp:CreateDate>{currenDate:o}</xmp:CreateDate>
            <xmp:ModifyDate>{currenDate:o}</xmp:ModifyDate>
            <xmp:MetadataDate>{currenDate:o}</xmp:MetadataDate>
            <dc:format>{format}</dc:format>
            <dc:title>
                <rdf:Alt>
                    <rdf:li xml:lang="x-default">{title}</rdf:li>
                </rdf:Alt>
            </dc:title>
            <dc:creator>
                <rdf:Seq>
                    {string.Join(Environment.NewLine, authors.Select(a => $"<rdf:li>{a}</rdf:li>"))}
                </rdf:Seq>
            </dc:creator>
            <dc:description>
                <rdf:Alt>
                    <rdf:li xml:lang="x-default">{description}</rdf:li>
                </rdf:Alt>
            </dc:description>
            <dc:subject>
                <rdf:Bag>
                    {string.Join(Environment.NewLine, subject.Select(s => $"<rdf:li>{s}</rdf:li>"))}
                </rdf:Bag>
            </dc:subject>
            <dc:rights>
                <rdf:Alt>
                    <rdf:li xml:lang="x-default">{copyrightNotice}</rdf:li>
                </rdf:Alt>
            </dc:rights>
            <xmpRights:Marked>True</xmpRights:Marked>
            <xmpMM:InstanceID>xmp.iid:{documentId}</xmpMM:InstanceID>
            <xmpMM:DocumentID>xmp.did:{documentId}</xmpMM:DocumentID>
            <xmpMM:OriginalDocumentID>xmp.did:{documentId}</xmpMM:OriginalDocumentID>
            <xmpMM:History>
                <rdf:Seq>
                    <rdf:li rdf:parseType="Resource">
                        <stEvt:action>saved</stEvt:action>
                        <stEvt:instanceID>xmp.iid:{documentId}</stEvt:instanceID>
                        <stEvt:when>{currenDate:o}</stEvt:when>
                        <stEvt:softwareAgent>{creatorTool}</stEvt:softwareAgent>
                        <stEvt:changed>/</stEvt:changed>
                    </rdf:li>
                </rdf:Seq>
            </xmpMM:History>
        </rdf:Description>
    </rdf:RDF>
</x:xmpmeta>
""";
    }
}
