using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScreenTranslate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly OverlayWindow overlay;

        public MainWindow()
        {
            InitializeComponent();

            overlay = new OverlayWindow();
            overlay.Show();

            overlay.SetRect(900, 100, 800, 800);
        }

        private async void OnOcrClick(object sender, RoutedEventArgs e)
        {
            var softwareBitmap = await CaptureScreenAsync();
            if (softwareBitmap == null) return;

            var result = await RecognizeTextAsync(softwareBitmap);
            if (result == null) return;

            var linesWithMeta = ProcessAndDrawOcrResult(result);

            UpdateTranslationUi(linesWithMeta);
        }

        private async Task<SoftwareBitmap?> CaptureScreenAsync()
        {
            // ① 前回の描画をクリア（下線は写ってもOCRに拾われにくいため、全体の非表示化とDelayを撤廃）
            overlay.ClearRectangles();

            // ② オーバーレイの表示領域（枠線の内側の純粋なキャプチャ領域）とDPIを取得
            var (pxX, pxY, pxWidth, pxHeight, dpiX, dpiY) = overlay.GetCaptureInfo();

            if (pxWidth <= 0 || pxHeight <= 0) 
            {
                return null;
            }

            using var bmp = new Bitmap(pxWidth, pxHeight);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(pxX, pxY, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
            }

            // ③ Bitmap → SoftwareBitmap に変換
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            var randomAccessStream = new InMemoryRandomAccessStream();
            await randomAccessStream.WriteAsync(ms.ToArray().AsBuffer());
            randomAccessStream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            return await decoder.GetSoftwareBitmapAsync();
        }

        private async Task<OcrResult?> RecognizeTextAsync(SoftwareBitmap softwareBitmap)
        {
            // ④ OCR
            var engine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (engine == null) return null;

            return await engine.RecognizeAsync(softwareBitmap);
        }

        private List<string> ProcessAndDrawOcrResult(OcrResult result)
        {
            var (_, _, _, _, dpiX, dpiY) = overlay.GetCaptureInfo();

            // ⑤ 表示（OCR の改行をそのまま反映する）
            return result.Lines.Select(line =>
            {
                if (!line.Words.Any())
                {
                    return $"{line.Text} [no words detected]";
                }

                var firstWord = line.Words.First();
                var minX = firstWord.BoundingRect.X;
                var minY = firstWord.BoundingRect.Y;
                var maxX = firstWord.BoundingRect.X + firstWord.BoundingRect.Width;
                var maxY = firstWord.BoundingRect.Y + firstWord.BoundingRect.Height;

                foreach (var word in line.Words.Skip(1))
                {
                    var rect = word.BoundingRect;
                    minX = Math.Min(minX, rect.X);
                    minY = Math.Min(minY, rect.Y);
                    maxX = Math.Max(maxX, rect.X + rect.Width);
                    maxY = Math.Max(maxY, rect.Y + rect.Height);
                }

                var w = maxX - minX;
                var h = maxY - minY;

                // 物理ピクセル(画像の座標)からWPFの論理ピクセルに変換
                double drawX = minX / dpiX;
                double drawY = minY / dpiY;
                double drawW = w / dpiX;
                double drawH = h / dpiY;

                // オーバーレイに行の下線を描画（矩形を使いたい場合は DrawRectangle に戻す）
                overlay.DrawUnderline(drawX, drawY, drawW, drawH);
                // overlay.DrawRectangle(drawX, drawY, drawW, drawH);

                return($"{line.Text} [x:{drawX:0}, y:{drawY:0}, w:{drawW:0}, h:{drawH:0}]");
            }).ToList(); 
        }

        private void UpdateTranslationUi(List<string> linesWithMeta)
        {
            SourceBox.Text = string.Join(Environment.NewLine, linesWithMeta);

            // ⑥ ここで翻訳 API に投げる（今回はダミーで元のテキストに [翻訳] を付加するだけ）
            var translatedLines = linesWithMeta.Select(line => $"[翻訳] {line}");

            ResultBox.Text = string.Join(Environment.NewLine, translatedLines);
        }
    }
}