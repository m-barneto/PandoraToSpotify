using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandoraToSpotify {
    internal class AuthServer {
        private static EmbedIOAuthServer _server;
        public static Func<Task<string>> Authenticated;
        private static bool _authenticated = false;
        private static String[] lines;

        public static async Task Main() {
            _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            lines = File.ReadAllLines(".env");
            var request = new LoginRequest(_server.BaseUri, lines[0], LoginRequest.ResponseType.Code) {
                Scope = new List<string> { Scopes.UserReadEmail, Scopes.PlaylistModifyPrivate, Scopes.PlaylistReadPrivate, Scopes.PlaylistModifyPublic }
            };
            BrowserUtil.Open(request.ToUri());
            await Task.Run(async () => {
                while (!_authenticated) {
                    await Task.Delay(100);
                }
            });
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response) {
            await _server.Stop();

            var config = SpotifyClientConfig.CreateDefault().WithRetryHandler(new SimpleRetryHandler() { RetryAfter = TimeSpan.FromSeconds(1) });
            var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                lines[0], lines[1], response.Code, new Uri("http://localhost:5000/callback")
              )
            );


            SpotifyClient client = new SpotifyClient(tokenResponse.AccessToken);

            var playlist = await client.Playlists.Create("5y0swptawl1jj40jyhusctnhh", new PlaylistCreateRequest("BENEE Radio Playlist"));
            string[] songs = File.ReadAllLines("BENEE Radio.txt");
            List<string> songIds = new List<string>();
            // loop through each song
            for (int i = 0; i < songs.Length; i++) {
                if (songs[i].Length == 0) continue;
                // Split song into search terms
                string first = songs[i].Split("^")[0];
                string songName = songs[i].Split("^")[1];
                string artist = first.Split(" - ")[0];
                string album = first.Split(" - ")[1];

                var result = await client.Search.Item(new SearchRequest(SearchRequest.Types.Track, $"{songName} {artist} {album}"));
                if (result.Tracks?.Items?.Count == 0) {
                    Console.Error.WriteLine("Unable to find track: " + songs[i]);
                }
                else {
                    songIds.Add("spotify:track:" + result.Tracks.Items[0].Id);
                }
                Console.WriteLine($"Name: {songName} Artist: {artist} Album: {album}");
                if (i % 20 == 0) {
                    Console.WriteLine("Starting song append");
                    await client.Playlists.AddItems(playlist.Id, new PlaylistAddItemsRequest(songIds));
                    songIds.Clear();
                    Console.WriteLine("Completed song append");
                }
            }
            await client.Playlists.AddItems(playlist.Id, new PlaylistAddItemsRequest(songIds));
            Console.WriteLine("Complete");
            _authenticated = true;
        }

        private static async Task OnErrorReceived(object sender, string error, string state) {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await _server.Stop();
        }
    }
}
