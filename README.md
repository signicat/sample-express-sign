# Express Signature Sample for ASP.NET Core

Sample application using the [Express Signature API](https://developer.signicat.com/apis/electronic-signing/express/).

Check out the [documentation](https://developer.signicat.com/express/docs/signature/) for more information about the signature service.

Please note that this example only shows a signature flow without any authentication. This means that the end-user does not have to authenticate themselves before signing and that anyone can sign and download documents.

## Requirements
- [Node.js](https://nodejs.org/en/)
- [.NET 7.0+ SDK](https://dotnet.microsoft.com/download)

## Running the sample frontend application

1. Install dependencies with `npm install`
2. Run the application with `npm start`
3. Go to http://localhost:3000 to view the application

## Running the sample backend application

1. Navigate to the `/Server` directory
2. Add your OAuth client ID and client secret to `appsettings.json`
3. Run the server with `dotnet run`
