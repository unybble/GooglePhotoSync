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
        private static string mediaPath = @"/Users/Jen/Pictures/";

        private static void MakeAlbumCall(string endPoint)
        {
           


            AlbumResponseRootObject albumResponseObject = new AlbumResponseRootObject();

            string pageToken = null;
            int c = 0;
            while(pageToken!=null || c == 0) { 

            var response = client.GetAsync(endPoint+ (pageToken != null ? "&pageToken=" + pageToken : "")+"&pageSize=2").Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    if (content.Length != 0)
                    {
                        albumResponseObject = JObject.Parse(content).ToObject<AlbumResponseRootObject>();
                        pageToken = albumResponseObject.nextPageToken;
                    }

                    foreach (var album in albumResponseObject.albums)
                    {
                        Directory.CreateDirectory(mediaPath + album.title);
                        var values = new Dictionary<string, string>
                        {
                            { "pageSize", "10" },
                            { "albumId", album.id.ToString() }

                        };


                        var ep = "https://photoslibrary.googleapis.com/v1/mediaItems:search";
                        MakeMediaCall(ep, values, album);
                    }


                }

            }
        }
        private static void MakeMediaCall(string endPoint, Dictionary<string,string> values, Album album)
        {
           
            var c = 0;
            while (values.ContainsKey("pageToken") || c==0)
            {
                var encodedContent = new StringContent(JsonConvert.SerializeObject(values), Encoding.UTF8, "application/json");
                var response = client.PostAsync(endPoint, encodedContent).Result;
                if (response.IsSuccessStatusCode)
                {
                    using (var wc = new WebClient())
                    {
                        //download to this directory
                        var content = response.Content.ReadAsStringAsync().Result;
                        if (content.Length != 0)
                        {
                            var clsResponseRootObject = JObject.Parse(content).ToObject<clsResponseRootObject>();
                           
                            if (!values.ContainsKey("pageToken"))
                                values.Add("pageToken", clsResponseRootObject.nextPageToken);
                            else
                                values["pageToken"] = clsResponseRootObject.nextPageToken;

                            foreach (var root in clsResponseRootObject.mediaItems)
                            {
                                var path = mediaPath + "/" + album.title + "/" + root.filename;
                                if (!File.Exists(mediaPath))
                                {
                                    if (root.mimeType.Contains("video"))
                                        wc.DownloadFile(root.baseUrl + "=dv", path);
                                    else
                                        wc.DownloadFile(root.baseUrl + "=d", path);

                                    Console.WriteLine(string.Format("({2}) {0} | {1}", album.title, root.filename, c++));
                                }
                            }
                        }
                    }
                }
            }
        }
        static void Main(string[] args)
        {


           
            UserCredential credential;
            string[] scopes = {
            "https://www.googleapis.com/auth/photoslibrary.sharing",
            "https://www.googleapis.com/auth/photoslibrary.readonly"
         };
            string UserName = "andersonn.jen@gmail.com";
            string ClientID = "920734361355-9jmsiu5k0cvt6kqhedvudaq9v6922ijv.apps.googleusercontent.com";
            string ClientSecret = "69CFh-cE-F8nrIj1f6zZmdwv";

            using (var stream = new FileStream($@"/Users/Jen/Projects/GooglePhotoSync/credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    UserName,
                    CancellationToken.None,
                    new FileDataStore("/Users/Jen/Projects/GooglePhotoSync/", true)).Result;
            }

            client.DefaultRequestHeaders.Add("client_id", ClientID);
            client.DefaultRequestHeaders.Add("client_secret", ClientSecret);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(credential.Token.TokenType,credential.Token.AccessToken);

            var ep = "https://photoslibrary.googleapis.com/v1/albums?excludeNonAppCreatedData=false";
            MakeAlbumCall(ep);

            ep = "https://photoslibrary.googleapis.com/v1/sharedAlbums?excludeNonAppCreatedData=false";

            MakeAlbumCall(ep);

        }
    }
}