# EC2 Lambda 

## Description
Process 
- A file dropped in an S3 bucket 
- This event triggers a function.
- The function starts an ec2 instance.  
- The instance processes the file and drop another file in an s3 bucket.
- The instance triggers a lambda function.
- That function terminates the ec2 instance.

## Diagram
![EC2-LambdaProject Diagram](https://github.com/JeBear76/python-diagrams/blob/main/ec2-lambda.png?raw=true)


## AWS Objects Names

### Policies
- ec2-access-for-lambda  
```
{
	"Version": "2012-10-17",
	"Statement": [
		{
			"Sid": "DescribeTags",
			"Effect": "Allow",
			"Action": "ec2:DescribeTags",
			"Resource": "*"
		},
		{
			"Sid": "StartStopTerminateInstancesInAccount",
			"Effect": "Allow",
			"Action": [
				"ec2:StartInstances",
				"ec2:RunInstances",
				"ec2:TerminateInstances",
				"ec2:StopInstances",
                "ec2:CreateTags"
			],
			"Resource": [
				"arn:aws:ec2:eu-west-1::image/ami-*",
				"arn:aws:ec2:eu-west-1:523759632228:key-pair/*",
				"arn:aws:ec2:eu-west-1:523759632228:instance/*",
				"arn:aws:ec2:eu-west-1:523759632228:network-interface/*",
				"arn:aws:ec2:eu-west-1:523759632228:security-group/*",
				"arn:aws:ec2:eu-west-1:523759632228:subnet/*",
				"arn:aws:ec2:eu-west-1:523759632228:volume/*"
			]
		}
	]
}
```
- iam-access-for-lambda
```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "VisualEditor0",
            "Effect": "Allow",
            "Action": "iam:PassRole",
            "Resource": "arn:aws:iam::523759632228:role/EC2-Processor-Role"
        }
    ]
}
```
- s3-access-for-ec2  
```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "s3AccesForEc2",
            "Effect": "Allow",
            "Action": [
                "s3:PutObject",
                "s3:GetObject",
                "s3:ListBucket",
                "s3:DeleteObject"
            ],
            "Resource": [
                "arn:aws:s3:::ec2-lambda-input",
                "arn:aws:s3:::ec2-lambda-output",
                "arn:aws:s3:::ec2-lambda-code",
                "arn:aws:s3:::*/*"
            ]
        }
    ]
}
```
- lambda-access-for-ec2  
```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "VisualEditor0",
            "Effect": "Allow",
            "Action": [
                "lambda:InvokeFunction",
                "lambda:InvokeAsync"
            ],
            "Resource": "arn:aws:lambda:*:523759632228:function:*"
        }
    ]
}
```
- cloudwatch-access-for-ec2  
```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "logs:CreateLogGroup",
                "logs:DescribeLogGroups"
            ],
            "Resource": "arn:aws:logs:eu-west-1:523759632228:*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "logs:CreateLogStream",
                "logs:PutLogEvents"
            ],
            "Resource": [
                "arn:aws:logs:eu-west-1:523759632228:log-group:ec2-lambda:*"
            ]
        }
    ]
}
```
###
 Roles
- Lambda-EC2-Runner-Role  
    * _ec2-access-for-lambda_ policy: Allows a Lambda function to start, stop and terminate and ec2 instance using   
    * _iam-access-for-lambda_ policy: Allows lambda to attache the Instance Profile to the instance
    * _AWSLambdaBasicExecutionRole_ built-in policy: Cloudwatch access
    * _AWSLambdaVPCAccessExecutionRole_ built-in policy: Needed to put functions in the VPC  

- EC2-Processor-Role  
    * _s3-access-for-ec2_ policy: Interacts with S3 using   
    * _lambda-access-for-ec2_ policy: Allows triggering lambda functions 
    * _cloudwatch-access-for-ec2_ policy: Log to cloudwatch

### Buckets
- ec2-lambda-input
- ec2-lambda-output
- ec2-lambda-code

### Lambdas
[Boto3 Reference](https://boto3.amazonaws.com/v1/documentation/api/latest/reference/services/ec2/client/run_instances.html)
- ec2Lambda-starter  
    * _killable_appserver_ami_ environment variable must contain the AMI Id  
    * _key_pair_ environment variable must contain a valid key pair for ssh  
    * _subnet_id_ environment variable must contain a subnet id in a VPC with appropriate connectivity to S3, Lambda and Cloudwatch  
```
import os
import json
import boto3

def lambda_handler(event, context):
    client = boto3.client("ec2")
    
    dryRun=eval(event['dryRun'])
    
    #base Amazon Linux 2 with dotnet 6.0 'ami-056d2deb35634ac41'
    
    baseAmi = os.environ['killable_appserver_ami']
    keyPair = os.environ['key_pair']
    subnetId = os.environ['subnet_id']
    
    response = client.run_instances(
        
        ImageId=baseAmi, 
        KeyName=keyPair,
        InstanceType='t2.micro',
        SubnetId= subnetId,
        MinCount=1,
        MaxCount=1,
        InstanceInitiatedShutdownBehavior='terminate',
        IamInstanceProfile={
            'Name':'EC2-Processor-Role'
        },
        TagSpecifications=[
            {
                'ResourceType': 'instance',
                'Tags': [
                    {
                        'Key': 'Project',
                        'Value': 'EC2-Lambda'
                    },
                ]
            },
            {
                'ResourceType': 'network-interface',
                'Tags': [
                    {
                        'Key': 'Project',
                        'Value': 'EC2-Lambda'
                    },
                ]
            },
            {
                'ResourceType': 'volume',
                'Tags': [
                    {
                        'Key': 'Project',
                        'Value': 'EC2-Lambda'
                    },
                ]
            }
        ],
        DryRun=dryRun
    )
    return {
        'statusCode': 200,
        'body': json.dumps(response, default=str)
    }
```

- ec2KillerLambda  
```
import json
import boto3 
def lambda_handler(event, context):
    client = boto3.client("ec2")
    
    instance = event['instance']
    dryRun = eval(event['dryRun'])
    
    response = client.terminate_instances(
        InstanceIds=[
            instance
        ],
        DryRun=dryRun
    )
    return {
        'statusCode': 200,
        'body': json.dumps(response)
    }
```
This function is not really necessary.  
Setting _InstanceInitiatedShutdownBehavior_ to 'True' in the run_instances call in ec2Lambda-starter will cause it to terminate if it shuts down by itself. So the shutdown could be run in the runProcess.sh  

### AMIs
[Reference 1](https://operavps.com/docs/run-command-after-boot-in-linux/)  
[Reference 2](https://azole.medium.com/how-to-send-message-to-cloudwatch-when-script-has-an-error-79c96ca515f0)  

Getting the instance Id for an ec2 instance from within the machine.  
`wget -q -O - http://169.254.169.254/latest/meta-data/instance-id`  
This will be useful if shutting down the instance from within dotnet core is an issue.  

- killable-appserver-ami
Script running on boot
```
aws s3 sync s3://ec2-lambda-code/process-starter/publish/ ~/apps/processStarter
. ~/apps/processStarter/runProcess.sh
```
_runProcess.sh_ (in solution) contains all instructions for the instance, including termination call to lambda.
