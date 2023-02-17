using System.ServiceModel;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Fec.WebJob.FecSubmission.WebServiceClient;

[MessageContract(WrapperName = "upload", WrapperNamespace = "http://service.webload.efo.fec.gov/", IsWrapped = true)]
public partial class UploadRequest
{
    [MessageBodyMember(Namespace = "http://service.webload.efo.fec.gov/", Order = 0)]
    [XmlElement(Form = XmlSchemaForm.Unqualified)]
    public string arg0 = string.Empty;

    [MessageBodyMember(Namespace = "http://service.webload.efo.fec.gov/", Order = 1)]
    [XmlElement(Form = XmlSchemaForm.Unqualified, DataType = "base64Binary")]
    public byte[] arg1 = new byte[1];

    public UploadRequest()
    {
    }

    public UploadRequest(string arg0, byte[] arg1)
    {
        this.arg0 = arg0;
        this.arg1 = arg1;
    }
}
