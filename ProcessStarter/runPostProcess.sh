#!/bin/sh
aws s3api put-object --bucket ec2-lambda-output --key processStarter.log --body ~/processStarter.log
