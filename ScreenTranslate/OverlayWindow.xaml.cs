using System.Windows;
using System.Windows.Controls;

namespace ScreenTranslate;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void SetRect(int x, int y, int width, int height)
    {
        // ウィンドウ自体の位置とサイズを更新
        if (this.WindowState == WindowState.Maximized) this.WindowState = WindowState.Normal;

        this.Left = x;
        this.Top = y;
        this.Width = width;
        this.Height = height;
    }

    /// <summary>
    /// 画面キャプチャ用の物理座標とDPIを取得します。
    /// ウィンドウが最大化された時のはみ出し（1ディスプレイ外の余分な領域）を取り除き、
    /// 現在表示されている正確なキャプチャ領域を返します。
    /// </summary>
    /// <summary>
    /// 画面キャプチャ用の物理座標とDPIを取得します。
    /// ウィンドウが最大化された時のはみ出し（1ディスプレイ外の余分な領域）を取り除き、
    /// 現在表示されている正確なキャプチャ領域を返します。
    /// </summary>
    public (int pxX, int pxY, int pxWidth, int pxHeight, double dpiX, double dpiY) GetCaptureInfo()
    {
        // RootGrid は WindowChrome によって画面外のはみ出しがカットされた「実際の表示領域」
        if (this.RootGrid.ActualWidth == 0 || this.RootGrid.ActualHeight == 0)
        {
            return (0, 0, 0, 0, 1.0, 1.0);
        }

        Point topLeft = this.RootGrid.PointToScreen(new Point(0, 0));
        Point bottomRight = this.RootGrid.PointToScreen(new Point(this.RootGrid.ActualWidth, this.RootGrid.ActualHeight));

        PresentationSource source = PresentationSource.FromVisual(this);
        double dpiX = 1.0, dpiY = 1.0;
        if (source?.CompositionTarget != null)
        {
            dpiX = source.CompositionTarget.TransformToDevice.M11;
            dpiY = source.CompositionTarget.TransformToDevice.M22;
        }

        int pxX = (int)Math.Round(topLeft.X);
        int pxY = (int)Math.Round(topLeft.Y);
        int pxWidth = (int)Math.Round(bottomRight.X - topLeft.X);
        int pxHeight = (int)Math.Round(bottomRight.Y - topLeft.Y);

        return (pxX, pxY, pxWidth, pxHeight, dpiX, dpiY);
    }

    /// <summary>
    /// 描画されている矩形をすべてクリアします。
    /// </summary>
    public void ClearRectangles()
    {
        this.OverlayCanvas.Children.Clear();
    }

    // ウインドウ上に矩形を描画する
    public void DrawRectangle(double x, double y, double width, double height)
    {
        var rect = new System.Windows.Shapes.Rectangle
        {
            Stroke = System.Windows.Media.Brushes.Yellow, // 見やすいように黄色などに変更可能
            StrokeThickness = 2,
            Width = width,
            Height = height,
            IsHitTestVisible = false // クリック透過を維持
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        this.OverlayCanvas.Children.Add(rect);
    }

    // ウインドウ上に下線を描画する
    public void DrawUnderline(double x, double y, double width, double height)
    {
        var line = new System.Windows.Shapes.Line
        {
            Stroke = System.Windows.Media.Brushes.Yellow, // 好きな色に変更できます
            StrokeThickness = 2,
            X1 = x,
            Y1 = y + height + 2, // 下端に合わせる
            X2 = x + width,
            Y2 = y + height + 2, // 下端に合わせる
            IsHitTestVisible = false
        };
        this.OverlayCanvas.Children.Add(line);
    }
}