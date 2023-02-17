using Fec.Core.Interfaces;
using Fec.Core.Services;
using Fec.WebJob.FecSubmission;
using Fec.WebJob.FecSubmission.Interfaces;
using Fec.WebJobs.Core;
using Microsoft.Extensions.Logging;

//WebJob object for setting up common Web Job stuff
FecWebJob fecWebJob = new(useJava: true);

//Common WebJob initialization
HostBuilder hostBuilder = fecWebJob.GetHostBuilder();

//Add our Queue Message Handler
hostBuilder.ConfigureServices(services => {
    services.AddScoped<IAzureQueueService, AzureQueueService>();        //To post queue messages
    services.AddScoped<IAzureStorageService, AzureStorageService>();    //To upload the complete fec file
    services.AddScoped<IFecSubmission, FecSubmission>();                //Submits fec files via Web Service
    services.AddScoped<IFileSystemService, FileSystemService>();
    services.AddScoped<IAzureStorageService, AzureStorageService>();
});

//Create the Host Interface and run it asynchronously
using IHost iHost = hostBuilder.Build();

//Ensure required Storage Queues exist
fecWebJob.ValidateQueues(iHost, fecWebJob.Configuration["FecSubmission:QueueName"]);

//Ensure required Storage Containers exist
var logger = iHost.Services.GetService<ILogger<AzureStorageService>>()!;
fecWebJob.ValidateStorageContainers(logger, fecWebJob.Configuration["FecDataFilesContainerName"]);

await iHost.RunAsync();
