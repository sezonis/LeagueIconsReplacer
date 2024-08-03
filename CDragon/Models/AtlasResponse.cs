using System.Drawing;
using static LeagueIconsReplacer.Parser.AtlasReader;

namespace LeagueIconsReplacer.CDragon.Models {
    public class AtlasResponse {
        public Image Atlas { get; set; }

        public string AtlasJson { get; set; }

        public Enum.AtlasType AtlasType { get; set; }

        private List<ItemDetails> items = new List<ItemDetails>();

        public List<ItemDetails> Parsed() {
            if(items.Count == 0) {
                items = Parser.AtlasReader.ParseFromJson(AtlasJson);
            }
            return items;
        }
    }
}
