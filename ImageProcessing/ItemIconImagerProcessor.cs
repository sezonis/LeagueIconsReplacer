using ImageMagick;
using LeagueIconsReplacer.CDragon;
using LeagueIconsReplacer.CDragon.Models;
using LeagueIconsReplacer.Properties;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace LeagueIconsReplacer.Processing {
    public sealed class ItemIconImagerProcessor : IDisposable {

        public static readonly HashSet<string> PrismaticRainbowBorderIconFilesSet = new HashSet<string> {
            "3032_innervatinglocket",
            "3055_fulmination",
            "3056_demonkingscrown",
            "3058_shieldofmoltenstone",
            "3059_cloakofstarrynight",
            "3061_forceofentropy",
            "3062_sanguinegift",
            "3064_talismanofascension",
            "3069_hamstringer",
            "3131_swordofthedivine",
            "3193_gargoylestoneplate",
            "443080_twinmasks",
            "443081_hexboltcompanion",
            "443090_reaperstoll",
            "447114_reverberation",
            "447115_regicide",
            "447116_kinkoujitte",
            "447118_pyromancerscloak",
            "447119_lightningrod",
            "447120_diamondtippedspear",
            "447121_twilightsedge",
            "447122_blackholegauntlet",
            "447123_puppeteer",
            "4636_nightharvester",
            "4637_demonicembrace",
            "4644_crownoftheshatteredqueen",
            "6630_goredrinker",
            "6656_everfrost",
            "6664_turbochemtank",
            "6667_radiantvirtue",
            "6671_galeforce",
            "6693_prowlersclaw",
            "7100_mirageblade",
            "7101_gamblersblade",
            "7102_realityfracture",
            "7103_hemomancershelm",
            "7106_dragonheart",
            "7107_decapitator",
            "7108_runecarver",
            "7110_moonflairspellblade",
            "7112_flesheater",
            "7113_detonationorb",
        };

        private Dictionary<string, MagickImage> _arenaBorderImagesDict = new Dictionary<string, MagickImage>();


        public void ProcessImageSingleton(SingletonItem singletonItem, Image itemIcon, string outputFilePath) {
            switch (singletonItem.ItemIconType) {
                case CDragon.Enum.ItemIconType.Normal:
                default:
                SaveAsDds(itemIcon, outputFilePath); //DDS
                break;

                case CDragon.Enum.ItemIconType.ArenaBorderd:
                SaveBorderedImage(singletonItem, GetBorderType(singletonItem), itemIcon, outputFilePath);
                break;
            }
        }

        private string GetBorderType(SingletonItem singletonItem) {
            return PrismaticRainbowBorderIconFilesSet.Contains(singletonItem.Name) ? "rainbow" : "gold";
        }

        private void SaveBorderedImage(SingletonItem singletonItem, string borderType, Image itemIcon, string outputFilePath) {
            MagickImage borderImage;
            if (!_arenaBorderImagesDict.ContainsKey(borderType)) {
                var downloadedImage = Downloader.DownloadImage(singletonItem.Url);
                if (downloadedImage == null) { //If it's null then most likely the website is offline
                    throw new ArgumentNullException("Image is null, couldn't download it");
                }
                borderImage = ConvertImageToMagickDds(downloadedImage);
                _arenaBorderImagesDict.Add(borderType, borderImage);
            } else {
                borderImage = _arenaBorderImagesDict[borderType];
            }
            using var borderedIcon = CreateBorderedIconImageFromBorderedImage(borderImage, itemIcon);
            borderedIcon.SaveAsTex(outputFilePath);
        }

        /* 
         * Backup function incase of changes
         */
        private void RunDds2Tex(string inputFile, string outputFile, bool deleteInputFile = true) {
            var tex2ddsExePath = "tex2dds.exe";
            if (!File.Exists(tex2ddsExePath)) {
                var tex2ddsBytes = (byte[])Properties.Resources.ResourceManager.GetObject("tex2dds");
                using var fileStream = File.Create(tex2ddsExePath);
                fileStream.Write(tex2ddsBytes, 0, tex2ddsBytes.Length);
            }
            var processStartInfo = new ProcessStartInfo {
                FileName = tex2ddsExePath,
                Arguments = $"\"{inputFile}\" \"{outputFile}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            if (deleteInputFile) {
                File.Delete(inputFile);
            }
        }

        private MagickImage CreateBorderedIconImageFromBorderedImage(MagickImage borderedImage, Image itemIcon) {
            using var itemIconMagick = ConvertImageToMagickDds(itemIcon);
            return CreateBorderedIconImage(borderedImage, itemIconMagick);
        }


        private MagickImage CreateBorderedIconImage(MagickImage borderedImage, MagickImage itemIcon, int outerSize = 128, int innerSize = 88) {

            var icon = itemIcon;
            // --- SCALE WITH HIGH QUALITY (Lanczos) ---
            icon.FilterType = FilterType.Lanczos;        // best for sharp icons
            icon.Resize(new MagickGeometry(innerSize, innerSize) {
                IgnoreAspectRatio = false
            });

            // After resize, actual dimensions:
            int newW = icon.Width;
            int newH = icon.Height;

            // --- CENTER ICON WITHIN 88x88 ---
            int borderMargin = (outerSize - innerSize) / 2;     // = 20px
            int offsetX = borderMargin + (innerSize - newW) / 2;
            int offsetY = borderMargin + (innerSize - newH) / 2;

            // Create final canvas
            var result = new MagickImage(MagickColors.Transparent, outerSize, outerSize);

            // Draw border first
            result.Composite(borderedImage, CompositeOperator.Over);

            // Composite scaled icon
            result.Composite(icon, offsetX, offsetY, CompositeOperator.Over);

            return result;
        }


        public static void SaveAsDds(Image image, string outputPath) {
            using (var tempImage = ConvertImageToMagickDds(image)) {
                SaveAsDds(tempImage, outputPath);
            }
        }

        public static void SaveAsDds(MagickImage image, string outputPath) {
            image.Write(Path.ChangeExtension(outputPath,".dds"));
        }

        private static MagickImage ConvertImageToMagickDds(Image image) {
            using var memStream = new MemoryStream();
            image.Save(memStream, ImageFormat.Png);
            memStream.Position = 0;
            return new MagickImage(memStream) { Format = MagickFormat.Dds};
        }


        public void Dispose() {
            foreach (var image in _arenaBorderImagesDict.Values) {
                image?.Dispose();
            }
        }
    }
}
