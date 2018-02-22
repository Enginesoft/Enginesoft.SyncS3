# Enginesoft.SyncS3

Windows service that monitors a local folder and sends all files added in this folder to an Amazon AWS S3 bucket



## Installing the service

- Copy the files from folder /compiled
- In folder "\Config" create a new file "main.json" using the template of file "main.json.example", this file need contains the aws access key
- Execute the file: "service_install.bat" to install the service in Windows, the user will be requested, normally this service work better using: ".\administrator"
- In Service Manager (Start / Services) start the service: "Enginesoft.SyncS3.Service"


## Log

- Application logs are stored in folder: "/Log"


## Running the project in Visual Studio

- Open the solution in Visual Studio 2017
- In folder "\Config" create a new file "main.json" using the template of file "main.json.example", this file need contains the aws access key
- Run the project pressing F5
- To debug, put a break point in Start() method of file: "EntryService.cs", this is the first method executed

