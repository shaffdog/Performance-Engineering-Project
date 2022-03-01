using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Started Picture Uploader");
//Put Key to Function here!
string functionKey = string.Empty;
string httpTriggerURL = $"http://localhost:7071/api/PicSegmentationHttpTrigger/{functionKey}";
//string? picturePath = string.Empty;
string? picturePath = @"C:\Users\mshaffer-surfacebook\OneDrive\Pictures\profile pic.jpg";

if(args.Count() > 0)
{
    picturePath = args[0];
}
    //var stringresult = string.Compare(picturePath,string.Empty);
    var uriResult = Uri.IsWellFormedUriString(picturePath, UriKind.RelativeOrAbsolute);
while(string.Compare(picturePath,string.Empty) == 0)
{
    Console.WriteLine($"Current PicturePath Value: \"{picturePath}\"");
    Console.WriteLine("Please enter a valid path to a picture.");
    picturePath = Console.ReadLine();
}

using var client = new HttpClient();

var contentType = new MediaTypeWithQualityHeaderValue("image/jpeg");
// var contentType = new MediaTypeWithQualityHeaderValue("application/json");
client.BaseAddress = new Uri(httpTriggerURL);
client.DefaultRequestHeaders.Accept.Add(contentType);
//new FileStream(picturePath, FileMode.Open);;
var contentData = new ByteArrayContent(File.ReadAllBytes(picturePath));
// var data = new Dictionary<string, string>
// {
//     {"name", "Matthew Shaffer"}
// };

//var jsonData = JsonConvert.SerializeObject(data);
//var contentData = new StringContent(jsonData, Encoding.UTF8, "application/json");

var result = await client.PostAsync("", contentData);
//var resultString = await result.Content.ReadAsStringAsync();
Console.WriteLine($"result: {result.Content}");

