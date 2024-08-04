using ImageMagick;
using LeagueIconsReplacer.CDragon.Models;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace LeagueIconsReplacer {
    public partial class Form1 : Form {

        static string? IconsDirectory { get; set; } = null;

        CDragon.Downloader Downloader { get; } = new CDragon.Downloader();
        public Form1() {
            InitializeComponent();
            this.MinimizeBox = false;
            this.MaximizeBox = false;
        }



        private void SaveAsDDS(Image image, string outputPath) {
            using (var memStream = new MemoryStream()) {
                image.Save(memStream, ImageFormat.Png);
                memStream.Position = 0;
                using (var ddsImage = new MagickImage(memStream)) {
                    ddsImage.Write(outputPath);
                }
            }
        }


        public Image ReplaceFiles(AtlasResponse atlasResponse, HashSet<int> ItemIdsToReplace) {
            var itemDetails = atlasResponse.Parsed();

            var graphics = Graphics.FromImage(atlasResponse.Atlas);
            foreach (var item in itemDetails) {

                if (!ItemIdsToReplace.Contains(item.ItemId)) {
                    continue;
                }

                var imagePath = $@"{IconsDirectory}\{item.ItemId}.png";
                using (var image = Image.FromFile(imagePath)) {
                    var rect = item.CalculateRect(atlasResponse.Atlas.Width, atlasResponse.Atlas.Height);
                    var resized = ResizeImageToFitRectangle((Bitmap)image, rect);
                    graphics.DrawImage(resized, new Point((int)rect.X, (int)rect.Y));
                    graphics.DrawImage(resized, new Point((int)rect.X, (int)rect.Y));
                };
            }
            graphics.Dispose();
            return atlasResponse.Atlas;
        }

        public static Bitmap ResizeImageToFitRectangle(Bitmap originalImage, RectangleF targetRectangle) {
            // Create a new bitmap with the dimensions of the target rectangle
            Bitmap resizedImage = new Bitmap((int)targetRectangle.Width, (int)targetRectangle.Height);

            using (Graphics graphics = Graphics.FromImage(resizedImage)) {
                // Set the interpolation mode to high quality
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                // Draw the original image onto the new bitmap
                graphics.DrawImage(originalImage, 0, 0, resizedImage.Width, resizedImage.Height);
            }

            return resizedImage;
        }

        private void buttonStart_Click(object sender, EventArgs e) {

            if (IconsDirectory == null) {
                MessageBox.Show(this, "Please choose a directory first");
                return;
            }

            if (Directory.Exists("temp")) {
                Directory.Delete("temp", true);
            }

            var tempDir = Directory.CreateDirectory("temp");
            var atlasDir = Directory.CreateDirectory($"{tempDir.FullName}\\assets\\items\\icons2d\\autoatlas");
            var smallIconsDir = Directory.CreateDirectory($"{atlasDir.FullName}\\smallicons");
            var bigIconsDir = Directory.CreateDirectory($"{atlasDir.FullName}\\largeicons");

            var oldIconFiles = Directory.GetFiles(IconsDirectory);
            var itemsToReplaceSet = new HashSet<int>();

            //Verify each name is itemid only
            foreach (var oldIconFile in oldIconFiles) {
                var name = Path.GetFileNameWithoutExtension(oldIconFile).Trim();
                if (int.TryParse(name, out var result)) {
                    itemsToReplaceSet.Add(result);
                }
            };


            //Replaces small icons atlas
            var smallIcons = Downloader.DownloadSmallIconsAtlas();
            var smallAtlas = ReplaceFiles(smallIcons, itemsToReplaceSet);
            SaveAsDDS(smallAtlas, $"{smallIconsDir.FullName}\\atlas_0.dds");

            var bigIcons = Downloader.DownloadBigIconsAtlas();

            //Replaces big icons atlas

            var bigAtlas = ReplaceFiles(bigIcons, itemsToReplaceSet);
            SaveAsDDS(bigAtlas, $"{bigIconsDir.FullName}\\atlas_0.dds");

            //Replace all Singletons

            foreach (var item in Downloader.GetSingletonNames()) {
                if (!itemsToReplaceSet.Contains(item.Id)) continue;
                var imageFilePath = $"{IconsDirectory}\\{item.Id}.png";
                if (File.Exists(imageFilePath)) {
                    using var img = Image.FromFile(imageFilePath);
                    SaveAsDDS(img, $"{atlasDir.Parent}\\{item.Name}.dds");
                }
            }

            //Save as wad
            Directory.CreateDirectory("WadOutputs");
            var wadOutputDir = "WadOutputs\\Global.wad.client";
            MakeWadWithWadMake(tempDir.FullName, wadOutputDir);

            //Delete Temp Dir
            EmptyAndDeleteDir(tempDir);

            MessageBox.Show("All done! Saved to WadOutputs");
        }

        private void MakeWadWithWadMake(string directory, string outputPath) {
            var wadMakeExePath = "wad-make.exe";
            if (!File.Exists(wadMakeExePath)) {
                var wadMakerBytes = (byte[])Properties.Resources.ResourceManager.GetObject("wad-make");
                using var fileStream = File.Create(wadMakeExePath);
                fileStream.Write(wadMakerBytes, 0, wadMakerBytes.Length);
            }
            var processStartInfo = new ProcessStartInfo {
                FileName = wadMakeExePath,
                Arguments = $"\"{directory}\" \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process {
                StartInfo = processStartInfo
            };

            process.Start();
            process.WaitForExit();
        }

        public static void EmptyAndDeleteDir(DirectoryInfo directory) {
            foreach (FileInfo file in directory.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
            directory.Delete();
        }

        private void buttonSetDirectory_Click(object sender, EventArgs e) {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog() { IsFolderPicker = true }) {
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                    IconsDirectory = dialog.FileName;
                }
            }

        }
    }
}