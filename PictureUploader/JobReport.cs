using Newtonsoft.Json;
namespace JobReport
{
    public class JobReportEvent
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }
        public string ClientId {get;set;}
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalTimeToCompleteJob{get; set;}
        public TimeSpan TimeWaitingOnAzureFunction{get; set;}
        public string JobStatus{get; set;}

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}