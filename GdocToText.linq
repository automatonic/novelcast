<Query Kind="Program">
  <NuGetReference Prerelease="true">Google.Apis.Drive.v2</NuGetReference>
  <Namespace>Google</Namespace>
  <Namespace>Google.Apis</Namespace>
  <Namespace>Google.Apis.Auth</Namespace>
  <Namespace>Google.Apis.Auth.OAuth2</Namespace>
  <Namespace>Google.Apis.Authentication</Namespace>
  <Namespace>Google.Apis.Drive.v2</Namespace>
  <Namespace>Google.Apis.Drive.v2.Data</Namespace>
  <Namespace>Google.Apis.Services</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Google.Apis.Download</Namespace>
</Query>

private const int KB = 0x400;
private const string EXT = "tex";
private const int DownloadChunkSize = 256 * KB;
private string OutDirectory = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), "out");
private static readonly string[] Scopes = new[] { DriveService.Scope.Drive };
static Regex MatchIllegalChars;
static Regex MatchMultipleUnderscores;

void Main()
{
  string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()) + " " + Path.PathSeparator + Path.DirectorySeparatorChar + Path.AltDirectorySeparatorChar + ".'\"";
  MatchIllegalChars = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
  MatchMultipleUnderscores = new Regex("__+");
	var task = Run();
	task.Wait();
}

async Task Run()
{
	GoogleWebAuthorizationBroker.Folder = "GdocToText";
	UserCredential credential;

    credential = await 
		GoogleWebAuthorizationBroker.AuthorizeAsync(
			new ClientSecrets()
			{
				ClientId=Util.GetPassword("net.automatonic.novelcast.clientid"),
				ClientSecret=Util.GetPassword("net.automatonic.novelcast.clientsecret")
			}, 
			Scopes, 
			Util.GetPassword("net.automatonic.novelcast.user"), 
			CancellationToken.None);


	DriveService service = new DriveService(
		new BaseClientService.Initializer(){
 			HttpClientInitializer = credential,
            ApplicationName = "novelcast"
			
		});
	
	var folderList = service.Files.List();
	string collectionTitle = Util.ReadLine("Title");
	folderList.Q = "mimeType = 'application/vnd.google-apps.folder' and Title='"+collectionTitle+"'";
	
	var collection = (await folderList.ExecuteAsync()).Items.Single();
	collection.Id.Dump(collection.Title);
	
	var fileList = service.Files.List();
	fileList.Q = "mimeType = 'application/vnd.google-apps.document' and '"+collection.Id+"' in parents";
	
	var results = (await fileList.ExecuteAsync())
		.Items
		//https://developers.google.com/drive/manage-downloads
		.Select(item => new {Title = item.Title, ExportUrl= item.ExportLinks["text/plain"]})
		.OrderBy(item => item.Title);
	
	var folder = Path.Combine(OutDirectory, SanitizeTitle(collectionTitle));
	RequireDirectory(folder);
	
	Task.Factory.ContinueWhenAll(
		results.Select(item => 
			DownloadFile(
				service, 
				Path.ChangeExtension(
					Path.Combine(
						folder, 
						SanitizeTitle(item.Title)), 
					EXT),
				item.ExportUrl))
		.ToArray(),
		tasks => "Finished".Dump());
}

public string SanitizeTitle(string title)
{
	title = title.ToLower();
	title = title.Replace("'", "").Replace(",", "");
	return MatchMultipleUnderscores.Replace(MatchIllegalChars.Replace(title, "_"), "_");
}

public static void RequireDirectory(string directory)
{
	string fullDirectory = Path.GetFullPath(directory);
	if (System.IO.File.Exists(fullDirectory))
		throw new ArgumentException("Not a directory", "directory");
	if (!Directory.Exists(fullDirectory))
	{
		Directory.CreateDirectory(fullDirectory);
	}
}

private async Task DownloadFile(DriveService service, string filename, string url)
{
	var downloader = new MediaDownloader(service);
	downloader.ChunkSize = DownloadChunkSize;
	// add a delegate for the progress changed event for writing to console on changes
	downloader.ProgressChanged += Download_ProgressChanged;
	
	using (var fileStream = new System.IO.FileStream(
		filename,
		System.IO.FileMode.Create, 
		System.IO.FileAccess.Write))
	{
		var progress = await downloader.DownloadAsync(url, fileStream);
		if (progress.Status == DownloadStatus.Completed)
		{
			Console.WriteLine(filename + " was downloaded successfully");
		}
		else
		{
			Console.WriteLine(
				"Download {0} was interpreted in the middle. Only {1} were downloaded. ",
				filename, 
				progress.BytesDownloaded);
		}
	}
}

static void Download_ProgressChanged(IDownloadProgress progress)
{
	Console.WriteLine(progress.Status + " " + progress.BytesDownloaded);
}