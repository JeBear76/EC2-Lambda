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
![EC2-Lambda![EC2-LambdaProject Diagram](https://github.com/JeBear76/python-diagrams/blob/main/ec2-lambda.png?raw=true)

ts Names

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
                "logs:PutLogEvents",
                "logs:DescribeLogStreams"
            ],
            "Resource": [
                "arn:aws:logs:eu-west-1:523759632228:log-group:ec2-lambda:*"
            ]
        }
    ]
}
```
- ec2-access-for-ec2
```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "VisualEditor0",
            "Effect": "Allow",
            "Action": "ec2:DescribeTags",
            "Resource": "*"
        }
    ]
}
```
- system-manager-access-for-ec2
```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "VisualEditor0",
            "Effect": "Allow",
            "Action": [
                "ssm:DescribeParameters",
                "ssm:GetParameter"
            ],
            "Resource": "*"
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
    * _ec2-access-for-ec2_ policy: Get the 'Project' tag for the running instance
    * _system-manager-access-for-ec2_ policy: Get the code bucket parameter based on the 'Project' tag

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
    * _code_bucket_ environment variable must contain the name of the bucket where the code is found
    * _instance_profile_ environment variable must contain the name of the instance profile to be used by the machine
    * _project_ environment variable must contain the name of the project for tagging
```
import os
import json
import boto3
def lambda_handler(event, context):
    client = boto3.client("ec2")
    
    dryRun=eval(event.get('dryRun', 'False'))
    
    subnetId = os.environ['subnet_id']
    securityGroupId = os.environ['security_group_id']
    projectName = os.environ['project']
    instanceProfile = os.environ['instance_profile']
    baseAmi = os.environ['killable_appserver_ami']
    keyPair = os.environ['key_pair']
    codeBucket = os.environ['code_bucket']

    response = client.run_instances(
        ImageId=baseAmi,
        KeyName=keyPair,
        InstanceType='t2.micro',
        SubnetId= subnetId,
        SecurityGroupIds=[
            securityGroupId,
        ],
        MinCount=1,
        MaxCount=1,
        InstanceInitiatedShutdownBehavior='terminate',
        IamInstanceProfile={
            'Name': instanceProfile
        },
        TagSpecifications=[
            {
                'ResourceType': 'instance',
                'Tags': [
                    {
                        'Key': 'Project',
                        'Value': projectName
                    },
                    {
                        'Key': 'Name',
                        'Value': 'dotnetRunner'
                    },
                ]
            },
            {
                'ResourceType': 'network-interface',
                'Tags': [
                    {
                        'Key': 'Project',
                        'Value': projectName
                    },
                ]
            },
            {
                'ResourceType': 'volume',
                'Tags': [
                    {
                        'Key': 'Project',
                        'Value': projectName
                    },
                ]
            }
        ],
        UserData='''
        #!/bin/bash
        sudo service awslogs start
        (TAG_NAME="Project"
        INSTANCE_ID="`wget -qO- http://instance-data/latest/meta-data/instance-id`"
        REGION="`wget -qO- http://instance-data/latest/meta-data/placement/availability-zone | sed -e 's:\([0-9][0-9]*\)[a-z]*\$:\\1:'`"
        TAG_VALUE="`aws ec2 describe-tags --filters "Name=resource-id,Values=$INSTANCE_ID" "Name=key,Values=$TAG_NAME" --region $REGION --output=text | cut -f5`"
        
        aws ssm get-parameter --name /${TAG_VALUE}/codeBucket --region ${REGION} > param
        S3_CODE_BUCKET=`awk -F '"' '/Value/{print $(NF-1)}' param`
        
        aws s3 sync s3://$S3_CODE_BUCKET ~/apps/processStarter
        
        . ~/apps/processStarter/runProcess.sh) 2>&1
        ''',
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
TAG_NAME="Project"
INSTANCE_ID="`wget -qO- http://instance-data/latest/meta-data/instance-id`"
REGION="`wget -qO- http://instance-data/latest/meta-data/placement/availability-zone | sed -e 's:\([0-9][0-9]*\)[a-z]*\$:\\1:'`"
TAG_VALUE="`aws ec2 describe-tags --filters "Name=resource-id,Values=$INSTANCE_ID" "Name=key,Values=$TAG_NAME" --region $REGION --output=text | cut -f5`"

aws ssm get-parameter --name /${TAG_VALUE}/codeBucket --region ${REGION} > param
S3_CODE_BUCKET=`awk -F '"' '/Value/{print $(NF-1)}' param`

aws s3 sync s3://$S3_CODE_BUCKET ~/apps/processStarter

. ~/apps/processStarter/runProcess.sh
```
_runProcess.sh_ (in solution) contains all instructions for the instance, including termination call to lambda.
