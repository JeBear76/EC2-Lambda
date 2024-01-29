using System.Text.Json.Serialization;

namespace Ec2LambdaModels
{
    public class Ec2KillerLambdaTrigger
    {
        [JsonPropertyName("instance")]
        public string Instance { get; set; } = null!;
    }
}
