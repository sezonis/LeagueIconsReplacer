using Fizzler.Systems.HtmlAgilityPack;
using LeagueIconsReplacer.CDragon.Enum;
using LeagueIconsReplacer.CDragon.Models;
using LeagueIconsReplacer.Parser;
using System.Net;
using System.Text.RegularExpressions;

namespace LeagueIconsReplacer.CDragon {
    public class Downloader {

        const string BaseUrl = "https://raw.communitydragon.org/latest";

        public AtlasResponse DownloadAtlas(AtlasType iconType) {
            var iconTypeStr = iconType.ToString().ToLower();
            var atlasJsonUrl = $"{BaseUrl}/game/assets/items/icons2d/autoatlas/{iconTypeStr}/atlas_info.bin.json";
            var atlasImageUrl = $"{BaseUrl}/game/assets/items/icons2d/autoatlas/{iconTypeStr}/atlas_0.png";
            var json = GetResponse(atlasJsonUrl);
            var atlasImage = DownloadImage(atlasImageUrl);
            return new AtlasResponse() {
                AtlasJson = json,
                Atlas = atlasImage,
                 AtlasType = iconType,
            };
        }

        public List<SingletonItem> GetSingletonNames(){
            var itemsList = new List<SingletonItem>();
            var html = GetResponse($"{BaseUrl}/game/assets/items/icons2d/");
            var htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(html);

            var document = htmlDocument.DocumentNode;
            var tableRows = document.QuerySelectorAll("#list >tbody > tr");
            foreach(var tableRow in tableRows) {
                var nameNode = tableRow.FirstChild;
                var innerText = nameNode.InnerText.Trim();
                var match = Regex.Match(innerText, AtlasReader.LoLIdRegexPattern);
                if (match.Success) {
                    itemsList.Add(new SingletonItem() {
                        Id = int.Parse(match.Groups[1].Value),
                        Name = Path.GetFileNameWithoutExtension(innerText)
                    });
                }
            }
            return itemsList;
        }


        public AtlasResponse DownloadSmallIconsAtlas() {
            return DownloadAtlas(AtlasType.SmallIcons);
        }

        public AtlasResponse DownloadBigIconsAtlas() {
            return DownloadAtlas(AtlasType.LargeIcons);
        }



        Image DownloadImage(string url) {
            using (WebClient webClient = new WebClient()) {
                using (Stream stream = webClient.OpenRead(url)) {
                    return Image.FromStream(stream);
                }
            }
        }

        private string GetResponse(string url) {
            using (var webclient = new WebClient()) {
                return webclient.DownloadString(url);
            }
        }



    }
}
