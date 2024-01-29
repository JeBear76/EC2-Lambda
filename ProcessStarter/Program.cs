// See https://aka.ms/new-console-template for more information
using Amazon.S3;
using Amazon.S3.Model;
using Ec2LambdaModels;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

IAmazonS3 amazonS3 = new AmazonS3Client();

ListObjectsV2Response listObjectsV2Response = await amazonS3.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request()
{
    BucketName = config.GetSection("ec2-lambda-input").Value
});

HttpClient client = new HttpClient();
var response = await client.PostAsync("http://localhost:5000/DoSomethingCool",
    new StringContent(
        JsonSerializer.Serialize(new S3Event()
        {
            BucketName = listObjectsV2Response.S3Objects[0].BucketName,
            ObjectKey = listObjectsV2Response.S3Objects[0].Key
        })
    ));

await client.GetAsync("http://localhost:5000/DoSomethingCool");