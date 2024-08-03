using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LeagueIconsReplacer.Parser {
    public class AtlasReader {

        public const string LoLIdRegexPattern = @"^(\d+)_";
        public class ItemDetails {
            public string ItemName { get; set; }

            public int ItemId { get; set; }

            public string AtlasPath { get; set; }
            public double StartX { get; set; }
            public double StartY { get; set; }
            public double EndX { get; set; }
            public double EndY { get; set; }

            public RectangleF CalculateRect(int atlasWidth, int atlasHeight) {

                int x = (int)(StartX * atlasWidth);
                int y = (int)(StartY * atlasHeight);
                int width = (int)((EndX - StartX) * atlasWidth);
                int height = (int)((EndY - StartY) * atlasHeight);

                return new RectangleF(x, y, width, height);
            }
        }

            public static List<ItemDetails> Parse(string filename) {
                string jsonData = File.ReadAllText(filename);
                return ParseFromJson(jsonData);
            }

            public static List<ItemDetails> ParseFromJson(string jsonData) {
                var jsonObject = JToken.Parse(jsonData);

                List<ItemDetails> items = new List<ItemDetails>();

                foreach (var item in jsonObject.Children<JProperty>()) {
                    string itemName = item.Name;
                    var details = item.Value;

                    var itemNameNormalized = Path.GetFileNameWithoutExtension(itemName);

                    var match = Regex.Match(itemNameNormalized,LoLIdRegexPattern);
                    int itemId = 0;
                    if (match.Success) {
                        itemId = int.Parse(match.Groups[1].Value);
                    }
                    ItemDetails newItem = new ItemDetails {
                        ItemName = itemNameNormalized,
                        AtlasPath = details["atlasPath"].ToString(),
                        StartX = details["startX"].ToObject<double>(),
                        StartY = details["startY"].ToObject<double>(),
                        EndX = details["endX"].ToObject<double>(),
                        EndY = details["endY"].ToObject<double>(),
                        ItemId = itemId,
                    };

                    items.Add(newItem);
                }


            return items;
        }
    }
}
