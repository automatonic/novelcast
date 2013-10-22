<Query Kind="Program">
  <NuGetReference Prerelease="true">Google.Apis.Authentication</NuGetReference>
  <NuGetReference Prerelease="true">Google.Apis.Drive.v2</NuGetReference>
  <Namespace>Google.Apis.Drive.v2</Namespace>
  <Namespace>Google.Apis.Drive.v2.Data</Namespace>
</Query>

void Main()
{
//https://code.google.com/p/google-api-dotnet-client/source/browse/Drive.Sample/Program.cs?repo=samples
	DriveService service = new DocumentsService("code.google.com/p/exult/");
  	service.Credentials = new GDataCredentials(Username, Password);	
}
