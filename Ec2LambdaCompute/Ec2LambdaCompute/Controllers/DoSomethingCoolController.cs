using Amazon.Lambda;
using Amazon.S3;
using Amazon.Util;
using Ec2LambdaModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Ec2LambdaCompute.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DoSomethingCoolController : ControllerBase
    {
        readonly string _lambdaFunctionArn;
        readonly IAmazonS3 _amazonS3;
        readonly IAmazonLambda _amazonLambda;
        public DoSomethingCoolController(IAmazonS3 amazonS3, IAmazonLambda amazonLambda, IConfiguration configuration)
        {
            _amazonLambda = amazonLambda;
            _amazonS3 = amazonS3;
            _lambdaFunctionArn = configuration.GetValue<string>("ec2KillerLambda");
        }

        [HttpPost]
        public IActionResult ProcessAnS3File([FromBody] S3Event s3Event)
        {

            Thread.Sleep(5000);
            return Ok(s3Event);
        }

        [HttpGet]
        public IActionResult KillThisMachine()
        {
            _amazonLambda.InvokeAsync(new Amazon.Lambda.Model.InvokeRequest()
            {
                FunctionName = _lambdaFunctionArn,
                InvocationType = InvocationType.Event,
                Payload = JsonSerializer.Serialize(new Ec2KillerLambdaTrigger() { Instance = EC2InstanceMetadata.InstanceId })
            });

            return Ok();
        }
    }

}
