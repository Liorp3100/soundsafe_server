using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace server
{
    class Api
    {
        static readonly string apiKey = "sVagPQtMkk2vNm7spPP7xmfjKNFNqs3vLv2n5Swh"; // Replace with your API key
        static readonly string apiBase = "https://freesound.org/apiv2";

        public static async Task api()
        {
            await Api.api();
            int soundId = 415209; // Known working .wav sound on Freesound (e.g., "click.wav")

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Token " + apiKey);

                Console.WriteLine("Getting sound info...");
                string soundInfoUrl = $"{apiBase}/sounds/{soundId}/";
                HttpResponseMessage response = await client.GetAsync(soundInfoUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Failed to get sound info: " + response.StatusCode);
                    return;
                }

                string soundInfoJson = await response.Content.ReadAsStringAsync();
                JObject soundInfo = JObject.Parse(soundInfoJson);
                string soundName = (string)soundInfo["name"];
                string downloadUrl = (string)soundInfo["download"];

                // Sanitize filename for Windows
                string cleanFileName = string.Join("_", soundName.Split(Path.GetInvalidFileNameChars()));
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string filePath = Path.Combine(desktop, cleanFileName);

                Console.WriteLine("Downloading to: " + filePath);

                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, downloadUrl))
                {
                    request.Headers.Add("Authorization", "Token " + apiKey);
                    var fileResponse = await client.SendAsync(request);

                    if (!fileResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Download failed: " + fileResponse.StatusCode);
                        return;
                    }

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await fileResponse.Content.CopyToAsync(fs);
                    }

                    Console.WriteLine("Download complete!");
                }
            }
        }
    }
}
