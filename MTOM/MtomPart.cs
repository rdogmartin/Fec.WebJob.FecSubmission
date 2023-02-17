using System.Net.Http.Headers;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;

namespace Fec.WebJob.FecSubmission.MTOM;

internal class MtomPart
{
    private readonly HttpContent _part;

    public MtomPart(HttpContent part)
    {
        _part = part;
    }

    public MediaTypeHeaderValue? ContentType
    {
        get
        {
            string? contentTypeHeaderValue = _part.Headers.GetValues("Content-Type").FirstOrDefault();
            return !string.IsNullOrEmpty(contentTypeHeaderValue) && MediaTypeHeaderValue.TryParse(contentTypeHeaderValue.TrimEnd(';'), out var parsedValue) ? parsedValue : _part.Headers.ContentType;
        }
    }

    public string? ContentTransferEncoding => _part.Headers.TryGetValues("Content-Transfer-Encoding", out var values) ? values.Single() : null;
    public string? ContentId => _part.Headers.TryGetValues("Content-ID", out var values) ? values.Single() : null;

    public byte[] GetRawContent()
    {
        return ContentTransferEncoding  is null 
            ? throw new NotSupportedException()
            : !Regex.IsMatch(ContentTransferEncoding, "((7|8)bit)|binary", RegexOptions.IgnoreCase) 
                ? throw new NotSupportedException()
                : ReadFromStream();
    }

    public string GetStringContentForEncoder(MessageEncoder encoder)
    {
        if (ContentType is null || !ContentType.Parameters.Any(p => p.Name == "type" && encoder.IsContentTypeSupported(CleanNameValueHeaderValue(p))))
        {
            throw new NotSupportedException();
        }

        var encoding = ContentType.CharSet is not null ? Encoding.GetEncoding(ContentType.CharSet) : Encoding.Default;

        return encoding.GetString(GetRawContent());
    }

    private static string CleanNameValueHeaderValue(NameValueHeaderValue nameValueHeaderValue) => string.IsNullOrEmpty(nameValueHeaderValue.Value) ? "" : nameValueHeaderValue.Value.Replace("\"", "");

    private byte[] ReadFromStream()
    {
        using MemoryStream memoryStream = new();
        _part.ReadAsStreamAsync().Result.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
