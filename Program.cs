using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using static GoogleApplePhotoSync.Models;

namespace GoogleApplePhotoSync
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        static void Main(string[] args)
        {


            string mediaPath = @"/Users/Jen/Pictures/";


            clsResponseRootObject responseObject = new clsResponseRootObject();
            AlbumResponseRootObject albumResponseObject = new AlbumResponseRootObject();

            UserCredential credential;
            string[] scopes = {
            "https://www.googleapis.com/auth/photoslibrary.sharing",
            "https://www.googleapis.com/auth/photoslibrary.readonly"
         };
            string UserName = "andersonn.jen@gmail.com";
            string ClientID = "920734361355-9jmsiu5k0cvt6kqhedvudaq9v6922ijv.apps.googleusercontent.com";
            string ClientSecret = "69CFh-cE-F8nrIj1f6zZmdwv";

            using (var stream = new FileStream($@"/Users/Jen/Projects/GoogleApplePhotoSync/credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    UserName,
                    CancellationToken.None,
                    new FileDataStore("/Users/Jen/Projects/GoogleApplePhotoSync/", true)).Result;
            }

            client.DefaultRequestHeaders.Add("client_id", ClientID);
            client.DefaultRequestHeaders.Add("client_secret", ClientSecret);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(credential.Token.TokenType,credential.Token.AccessToken);

            var response = client.GetAsync("https://photoslibrary.googleapis.com/v1/albums").Result;
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                if (content.Length != 0)
                {
                    albumResponseObject = JObject.Parse(content).ToObject<AlbumResponseRootObject>();
                }

                foreach (var item in albumResponseObject.albums)
                {
                    Directory.CreateDirectory(mediaPath + item.title);
                    var values = new Dictionary<string, string>
                        {
                            { "pageSize", "100" },
                            { "albumId", item.id.ToString() }
                        };
                    var bcontent = new StringContent(JsonConvert.SerializeObject(values), Encoding.UTF8, "application/json");

                    response = client.PostAsync("https://photoslibrary.googleapis.com/v1/mediaItems:search", bcontent).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        using (var wc = new WebClient())
                        {
                            //download to this directory
                            content = response.Content.ReadAsStringAsync().Result;
                            if (content.Length != 0)
                            {
                                var clsResponseRootObject = JObject.Parse(content).ToObject<clsResponseRootObject>();
                                foreach (var root in clsResponseRootObject.mediaItems)
                                {
                                    var path = mediaPath + "/" + item.title + "/" + root.filename;

                                    if (root.mimeType.Contains("video"))
                                        wc.DownloadFile(root.baseUrl + "=dv", path);
                                    else
                                        wc.DownloadFile(root.baseUrl + "=d", path);

                                    Console.WriteLine(string.Format("Filename:{0}", root.filename));
                                }
                            }
                        }
                    }


                }

            }
        }
    }
}