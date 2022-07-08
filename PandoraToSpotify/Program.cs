<<<<<<< HEAD
﻿using PandoraToSpotify;
using SpotifyAPI.Web;


await AuthServer.Main();

=======
﻿using SpotifyAPI.Web;

// Load file
//FileStream station = File.OpenRead("songs.txt");

// Start spotify api
var config = SpotifyClientConfig.CreateDefault();
String[] lines = File.ReadAllLines(".env");
var request = new ClientCredentialsRequest(lines[0], lines[1]);
var response = await new OAuthClient(config).RequestToken(request);

var spotify = new SpotifyClient(config.WithToken(response.AccessToken));

var song = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, "Kool BENEE"));

if (song.Tracks.Items[0] is FullTrack track) {
    Console.WriteLine(track.Name);
}

Console.ReadLine();
>>>>>>> 8197deb64b8ab3b1e215b03376281a00b67247c6
