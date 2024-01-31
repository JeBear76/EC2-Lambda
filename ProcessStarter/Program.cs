using Amazon.Lambda;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Util;
using Ec2LambdaModels;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
          .WriteTo.File("processStarter.log")
          .CreateLogger();

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var lambdaFunctionArn = config.GetSection("ec2KillerLambda").Value;
Log.Information($"lambdaFunctionArn: {lambdaFunctionArn}");
var inputBucket = config.GetSection("ec2-lambda-input").Value;
Log.Information($"inputBucket: {inputBucket}");
var outputBucket = config.GetSection("ec2-lambda-output").Value;
Log.Information($"outputBucket: {outputBucket}");

IAmazonS3 amazonS3Client = new AmazonS3Client();
IAmazonLambda amazonLambda = new AmazonLambdaClient();
try
{
    ListObjectsV2Response listObjectsV2Response = await amazonS3Client.ListObjectsV2Async(new ListObjectsV2Request()
    {
        BucketName = inputBucket
    });

    var s3Event = new S3Event()
    {
        BucketName = listObjectsV2Response.S3Objects[0].BucketName,
        ObjectKey = listObjectsV2Response.S3Objects[0].Key
    };

    Log.Information($"s3Event: {JsonSerializer.Serialize(s3Event)}");

    GetObjectRequest getObjectRequest = new GetObjectRequest()
    {
        BucketName = s3Event.BucketName,
        Key = s3Event.ObjectKey
    };

    GetObjectResponse getObjectResponse = await amazonS3Client.GetObjectAsync(getObjectRequest);
    using StreamReader reader = new StreamReader(getObjectResponse.ResponseStream);

    string content = await reader.ReadToEndAsync();
    Log.Information($"content: {content.Length}");

    PutObjectRequest putObjectRequest = new PutObjectRequest
    {
        BucketName = outputBucket,
        Key = s3Event.ObjectKey,
        ContentBody = content
    };

    var putObjectResponse = await amazonS3Client.PutObjectAsync(putObjectRequest);

    await amazonLambda.InvokeAsync(new Amazon.Lambda.Model.InvokeRequest()
    {
        FunctionName = lambdaFunctionArn,
        InvocationType = InvocationType.Event,
        Payload = JsonSerializer.Serialize(new Ec2KillerLambdaTrigger() { Instance = EC2InstanceMetadata.InstanceId })
    });
}
catch (Exception ex)
{
    Log.Fatal(ex.Message);
    Log.Fatal(ex.StackTrace);
}

