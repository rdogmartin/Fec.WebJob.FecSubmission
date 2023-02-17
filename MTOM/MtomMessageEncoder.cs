using System.Net.Http.Headers;
using System.Net.Mime;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Fec.Core.Helpers;

namespace Fec.WebJob.FecSubmission.MTOM;

/// <summary>
/// Message Encoder with MTOM (Message Transmission Optimization Mechanism) support
/// </summary>
public class MtomMessageEncoder : MessageEncoder
{
    private readonly MessageEncoder _innerEncoder;

    public MtomMessageEncoder(MessageEncoder innerEncoder)
    {
        _innerEncoder = innerEncoder;
    }

    public override string ContentType => _innerEncoder.ContentType;
    public override string MediaType => _innerEncoder.MediaType;
    public override MessageVersion MessageVersion => _innerEncoder.MessageVersion;

    public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
    {
        using MemoryStream memoryStream = new(buffer.ToArray());
        Message message = ReadMessage(memoryStream, 1024, contentType);
        bufferManager.ReturnBuffer(buffer.Array);
        return message;
    }
    
    public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
    {
        if (_innerEncoder.IsContentTypeSupported(contentType))
        {
            MemoryStream memoryStream = new(); // We need an non disposed stream
            stream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return _innerEncoder.ReadMessage(memoryStream, maxSizeOfHeaders, contentType);
        }
        else
        {
            List<MtomPart> mtomParts = (from p in GetMultipartContent(stream, contentType) select new MtomPart(p)).ToList();

            MtomPart mainPart = (
                from part in mtomParts
                where part.ContentId == new ContentType(contentType).Parameters?["start"]
                select part).SingleOrDefault() ?? mtomParts.First();

            if (mainPart.ContentType is null)
            {
                throw new AppException($"Message Type not specified");
            }
            else
            {
                string mainContent = ResolveRefs(mainPart.GetStringContentForEncoder(_innerEncoder), mtomParts);
                Stream mainContentStream = CreateStream(mainContent, mainPart.ContentType);

                return _innerEncoder.ReadMessage(mainContentStream, maxSizeOfHeaders, mainPart.ContentType.ToString());
            }
        }
    }

    public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
    {
        return _innerEncoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
    }

    public override void WriteMessage(Message message, Stream stream)
    {
        _innerEncoder.WriteMessage(message, stream);
    }

    public override bool IsContentTypeSupported(string contentType)
    {
        if (_innerEncoder.IsContentTypeSupported(contentType))
        {
            return true;
        }

        List<string> contentTypes = contentType.Split(';').Select(c => c.Trim()).ToList();

        return (contentTypes.Contains("multipart/related", StringComparer.OrdinalIgnoreCase) && contentTypes.Contains(@"type=""application/xop+xml""", StringComparer.OrdinalIgnoreCase));
    }

    public override T GetProperty<T>()
    {
        return _innerEncoder.GetProperty<T>();
    }

    private static IEnumerable<HttpContent> GetMultipartContent(Stream stream, string contentType)
    {
        StreamContent streamContent = new StreamContent(stream);

        streamContent.Headers.Add("Content-Type", contentType);

        return streamContent.ReadAsMultipartAsync().Result.Contents;
    }

    private static string ResolveRefs(string mainContent, IList<MtomPart> parts)
    {
        XDocument xDocument = XDocument.Parse(mainContent);

        IEnumerable<XElement> xElements = xDocument.Descendants(XName.Get("Include", "http://www.w3.org/2004/08/xop/include")).ToList();

        foreach (XElement xElement in xElements)
        {
            XAttribute? xAttribute = xElement.Attribute("href");

            if (xAttribute is not null)
            {
                MtomPart referencedPart = (from part in parts where ReferenceMatch(xAttribute, part) select part).Single();

                xElement.ReplaceWith(Convert.ToBase64String(referencedPart.GetRawContent()));
            }
        }
        return xDocument.ToString(SaveOptions.DisableFormatting);
    }

    private static Stream CreateStream(string content, MediaTypeHeaderValue mediaTypeHeaderValue)
    {
        Encoding encoding = !string.IsNullOrEmpty(mediaTypeHeaderValue.CharSet) ? Encoding.GetEncoding(mediaTypeHeaderValue.CharSet) : Encoding.Default;

        return new MemoryStream(encoding.GetBytes(content));
    }

    private static bool ReferenceMatch(XAttribute hrefAttr, MtomPart part)
    {
        if (string.IsNullOrEmpty(part.ContentId))
        {
            return false;
        }
        else
        {
            var partId = Regex.Match(part.ContentId, "<(?<uri>.*)>");
            var href = Regex.Match(hrefAttr.Value, "cid:(?<uri>.*)");

            return href.Groups["uri"].Value == partId.Groups["uri"].Value;
        }
    }
}
