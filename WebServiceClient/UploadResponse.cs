using System.ServiceModel;
using System.Text.Json;
using System.Xml.Schema;
using System.Xml.Serialization;
using Fec.WebJobs.Core;

namespace Fec.WebJob.FecSubmission.WebServiceClient;

[MessageContract(WrapperName = "uploadResponse", WrapperNamespace = "http://service.webload.efo.fec.gov/", IsWrapped = true)]
public class UploadResponse
{
    [MessageBodyMember(Namespace = "http://service.webload.efo.fec.gov/", Order = 0)]
    [XmlElement(Form = XmlSchemaForm.Unqualified)]
    public string @return = string.Empty;

    //Deserialize @return (JSON) into a FECSubmisisonResponse record
    public FecSubmissionResponse? FecSubmissionResponse => JsonSerializer.Deserialize<FecSubmissionResponse>(@return);

    public UploadResponse()
    {
    }

    public UploadResponse(string @return)
    {
        this.@return = @return;
    }
}
