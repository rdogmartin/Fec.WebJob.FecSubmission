using System.Xml.Schema;
using System.Xml.Serialization;

namespace Fec.WebJob.FecSubmission.WebServiceClient;

[XmlType(Namespace = "http://service.webload.efo.fec.gov/")]
public partial class WebServiceClientException
{
    private string _message = string.Empty;

    [XmlElement(Form = XmlSchemaForm.Unqualified, Order = 0)]
    public string Message
    {
        get { return _message; }
        set { _message = value; }
    }
}
