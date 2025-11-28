using LeagueIconsReplacer.CDragon.Enum;

namespace LeagueIconsReplacer.CDragon.Models {
    public class SingletonItem {
        public string Name { get; set; }
        public int Id { get; set; }
        public string RelativePath { get; set; }
        public ItemIconType ItemIconType { get; set; }
        public string Url { get; set; }

        public string GetExtension() {
            return ItemIconType == ItemIconType.Normal ? ".dds" : ".tex";
        }
    }
}
