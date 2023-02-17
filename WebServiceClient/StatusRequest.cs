using System.ServiceModel;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Fec.WebJob.FecSubmission.WebServiceClient;

[MessageContract(WrapperName = "status", WrapperNamespace = "http://service.webload.efo.fec.gov/", IsWrapped = true)]
public partial class StatusRequest
{
    [MessageBodyMember(Namespace = "http://service.webload.efo.fec.gov/", Order = 0)]
    [XmlElement(Form = XmlSchemaForm.Unqualified)]
    public string arg0 = string.Empty;

    public StatusRequest()
    {
    }

    public StatusRequest(string arg0)
    {
        this.arg0 = arg0;
    }
}
