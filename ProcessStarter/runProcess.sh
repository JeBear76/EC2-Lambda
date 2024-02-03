#!/bin/sh
dotnet ~/apps/processStarter/ProcessStarter.dll

aws s3api put-object --bucket ec2-lambda-output --key processStarter.log --body ~/processStarter.log

instanceId="`wget -q -O - http://169.254.169.254/latest/meta-data/instance-id`"
json="\"instance\":\"${instanceId}\", \"dryRun\":\"False\""
aws lambda invoke --function-name arn:aws:lambda:eu-west-1:523759632228:function:ec2KillerLambda --invocation-type Event --payload "{`echo $json`}" --region eu-west-1 response.json

