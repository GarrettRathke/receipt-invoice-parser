# This documents lessons learned while building this project

## Don't Rush System Design
* spend more time up-front thinking about system design
	- initially, I assumed I would want to build the project as a containerized app b/c having the
	  frontend closely tied to the backend would make development and deployment simple. it did make
	  development simple, but I realized that deploying to the cloud would be overkill and potentially
	  time-consuming for a POC project (time to create new containers, orchestration). in retrospect,
	  I might have used Elastic Beanstalk for a managed environment (similar to Amplify).
	- the result was that I initially built a locally functioning containerized system but then had to
	  port over the backend into its own Lambda function 
* need to familiarize myself more w/ how the different pieces fit together when considering system design
	- I didn't realize that API Gateway has to be configured in order to accept binary media types.
	  By default, API Gateway handles all payloads as text unless explicitly configured otherwise. This caused a lot of time spent debugging.
	- __Google Search__: "aws angular how to send image in request to C# lambda with api gateway"
	__Response__: "The recommended approach for uploading images is to use Base64 encoding to send the image data within the request body. The Angular frontend will convert the image to a Base64 string, which the C# Lambda function will then decode and process"
	- Claude AI chat session link https://claude.ai/share/d1883ef0-214b-41e8-b69b-535a2a87a0d9
	- VS Code Co-Pilot 

## Further Areas to Study
1. I need a deep-dive understanding of "Angular zones" and Angular's change detection mechanism
2. I need to familiarize myself w/ .NET C# dev tools (VSCode vs Visual Studio vs Rider)
3. I need to better understand how dotnet secrets management works in local development
4. Best practices for AWS-Angular-C# tech stack (containerization vs )