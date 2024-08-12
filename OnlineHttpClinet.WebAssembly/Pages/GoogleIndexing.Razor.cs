using BootstrapBlazor.Components;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace OnlineHttpClinet.WebAssembly.Pages
{
    public partial class GoogleIndexing
    {
        string jsonFilePlaceHolder = """
{
    "type": "service_account",
    "project_id": "xxxxx",
    "private_key_id": "xxxxxx45dc588c3c433974e0eda",
    "private_key": "-----BEGIN PRIVATE KEY-----\n\n-----END PRIVATE KEY-----\n",
    "client_email": "xxx@xxx.iam.gserviceaccount.com",
    "client_id": "106002683364xxxxx",
    "auth_uri": "https://accounts.google.com/o/oauth2/auth",
    "token_uri": "https://oauth2.googleapis.com/token",
    "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
    "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/xxxx.iam.gserviceaccount.com",
    "universe_domain": "googleapis.com"
}
""";
        string uriDataplaceholder = """
https://codefrydev.in
https://codefrydev.in/Updates/app1ication/ocr/google-lens-ocr/
https://codefrydev.in/Updates/games/tinyfish/privacy-policy/
https://codefrydev.in/lJpdates/games/tinyfish/term-condition/
https://codefrydev.in/Updates/application/ocr/
""";
        string jsonKeyContent = string.Empty;
        string uriData = "";

        List<ConsoleMessageItem> logs = [];

        async Task AddItem(string message, Color color = Color.Info)
        {
            logs.Add(new ConsoleMessageItem()
            {
                Message = message,
                Color = color
            });
        }
        async Task ExecuteCommand()
        {
            if (string.IsNullOrWhiteSpace(uriData) || string.IsNullOrWhiteSpace(jsonKeyContent))
            {
                await AddItem("PLease Fill all Data", Color.Danger);
                StateHasChanged();
                return;
            }

            try
            {
                string[] scopes = { "https://www.googleapis.com/auth/indexing" };

                GoogleCredential credential;
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonKeyContent)))
                {
                    credential = GoogleCredential.FromStream(stream).CreateScoped(scopes);
                }

                HttpClient httpClient = new HttpClient();
                var oauth = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oauth);
                var urls = uriData.Split("\n");

                foreach (var url in urls)
                {
                    var result = await IndexUrl(url, httpClient);
                    if (result.ContainsKey("error"))
                    {
                        await AddItem($"Error({result["error"]["code"]} - {result["error"]["status"]}): {result["error"]["message"]}");
                    }
                    else
                    {
                        var metadata = result["urlNotificationMetadata"];
                        var latestUpdate = metadata["latestUpdate"];

                        await AddItem($"urlNotificationMetadata.url: {metadata["url"]}");
                        await AddItem($"urlNotificationMetadata.latestUpdate.url: {latestUpdate["url"]}");
                        await AddItem($"urlNotificationMetadata.latestUpdate.type: {latestUpdate["type"]}");
                        await AddItem($"urlNotificationMetadata.latestUpdate.notifyTime: {latestUpdate["notifyTime"]}");
                    }
                    StateHasChanged();
                }

                static async Task<Dictionary<string, dynamic>> IndexUrl(string url, HttpClient httpClient)
                {
                    string endpoint = "https://indexing.googleapis.com/v3/urlNotifications:publish";
                    var content = new
                    {
                        url = url.Trim(),
                        type = "URL_UPDATED"
                    };

                    var jsonContent = JsonConvert.SerializeObject(content);
                    var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(endpoint, httpContent);
                    var responseString = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseString);
                }
            }
            catch (Exception ex)
            {

                await AddItem(ex.Message);
                StateHasChanged();
            }
        }
    }
}
