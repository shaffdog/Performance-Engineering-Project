using Newtonsoft.Json;
using System.Drawing;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Azure.Cosmos;
using JobReport;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Started Picture Uploader");

Stopwatch sw = new Stopwatch();
JobReportEvent jobReport = new JobReportEvent(){Id = Guid.NewGuid().ToString(), StartTime = DateTime.UtcNow};
//Assign function key to Function Key
string functionKey = string.Empty;
string httpTriggerURL = $"http://localhost:7071/api/PicSegmentationHttpTrigger/{functionKey}";
//picturePath is path to input picture for segmentation
string? picturePath = string.Empty;
//Update Path to Output Path for segmented pictures
string outputPicId = Guid.NewGuid().ToString();
string? outputPicturePath = string.Empty;
outputPicturePath = $"{outputPicturePath}{outputPicId}.jpg";
//Guid for assigning to client 
const string idFile = "id.txt";
//Assign EndpointUri to CosmosDB here
string EndpointUri = string.Empty;
//Assign PrimaryKey to CosmosDB here
string PrimaryKey = string.Empty;
CosmosClient cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() {ApplicationName = "PictureUploader" });
string databaseId = "TestDatabase";
string containerId = "TestContainerClientJobs";
Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
Container container = await database.CreateContainerIfNotExistsAsync(containerId, "/partitionKey");

string? id = string.Empty;
if(File.Exists(idFile))
{
    using(StreamReader sr =File.OpenText(idFile))
    {
        id = await sr.ReadLineAsync();
    }
}
if(string.IsNullOrEmpty(id))
{
    if(File.Exists(idFile))    
    {    
        File.Delete(idFile);    
    }
    using(StreamWriter swriter = File.CreateText(idFile))
    {
        id = Guid.NewGuid().ToString();
        await swriter.WriteLineAsync(id);
    }
    
}
jobReport.ClientId = id;
jobReport.PartitionKey = id;

if(args.Count() > 0)
{
    picturePath = args[0];
}

while(string.Compare(picturePath,string.Empty) == 0 || picturePath is null)
{
    Console.WriteLine($"Current PicturePath Value: \"{picturePath}\"");
    Console.WriteLine("Please enter a valid path to a picture.");
    picturePath = Console.ReadLine();
}

HttpResponseMessage result;
using (var client = new HttpClient())
{
    var contentType = new MediaTypeWithQualityHeaderValue("image/jpeg");
    client.BaseAddress = new Uri(httpTriggerURL);
    client.DefaultRequestHeaders.Accept.Add(contentType);
    var byteArrayOPic = File.ReadAllBytes(picturePath);
    var contentData = new ByteArrayContent(byteArrayOPic);
    Console.WriteLine($"Size of ByteArray of Original Pic: {byteArrayOPic.Length}");
    try
    {
        sw.Start();
        result = await client.PostAsync("", contentData);                
        //Need to add checks for correct HttpResponseCode and ensure platform is Windows
        //Check correct HttpResponse Code for Success or Failure
        if(result != null && result.StatusCode == HttpStatusCode.OK)
        {
            var outputContent = await result.Content.ReadAsByteArrayAsync();
            if(new PlatformID[]{PlatformID.Win32NT}.Contains(Environment.OSVersion.Platform))
            {
                sw.Stop();
                jobReport.TimeWaitingOnAzureFunction = sw.Elapsed;
                jobReport.JobStatus = "Successful Job - Windows";
                using (var ms = new MemoryStream(outputContent))
                {
                    Console.WriteLine($"Size of ByteArray of Output Pic: {outputContent.Length}");
                    var outputImage = Image.FromStream(ms, true, false);
                    outputImage.Save(outputPicturePath);    
                }                
            }else
            {
                sw.Stop();
                jobReport.TimeWaitingOnAzureFunction = sw.Elapsed;
                jobReport.JobStatus = "Successful Job - Not Windows";
            }                
            
        }  
        else
        {
            sw.Stop();
            jobReport.TimeWaitingOnAzureFunction = sw.Elapsed;
            jobReport.JobStatus = "Failed Job";
        }
    }
    catch(Exception e)
    {
        sw.Stop();
        jobReport.TimeWaitingOnAzureFunction = sw.Elapsed;
        jobReport.JobStatus = "Failed Job";
        throw e;
    }
    finally
    {
        jobReport.EndTime = DateTime.UtcNow;
        jobReport.TotalTimeToCompleteJob = jobReport.EndTime - jobReport.StartTime;
        await container.CreateItemAsync<JobReportEvent>(jobReport, new PartitionKey(jobReport.PartitionKey));
    }
    
}