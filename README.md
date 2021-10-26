# FetchRewardsPointsService
How to run:
1. Framework dependent
	- Need to install .net 5 sdk first
	- Navigate to project root folder
	- use command: dotnet run
	- this should start up the application for you
2. Use pre-compiled self-contained deployments
	- after cloning the repo, find the zip file under folder /FetchRewardsPointsService/deployment
	- decompressing the file for your operating system (windows or linux)
		- for window version, simply double click "PointsService.exe"
		- for linux version, should be able to run "PointsService" direction (Not tested)
3. Docker
	- Please use the dockerfile provided to build image
	- If need more information about docker image or use docker-compose, I can provide later.

After successfully running the service:
following url should be available (port 5000 is default .net application, if run with docker port may be different depend on port mapping):
http://localhost:5000/swagger/index.html

The swagger page should list all rest endpoints. You can directly test it with swagger.
You can also use tools like postman to test the restApis. Please refer to swagger page for detailed Api information. (Swagger.json can be found udner folder: /deployment)
