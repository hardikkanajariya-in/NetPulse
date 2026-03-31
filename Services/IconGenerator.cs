using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SpeedoMeter.Services;

public static class IconGenerator
{
    public static Icon CreateTrayIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Draw a simple gauge/speedometer icon
        // Outer circle
        using var pen = new Pen(Color.FromArgb(220, 220, 220), 2.5f);
        g.DrawArc(pen, 4, 6, 24, 24, 180, 180);

        // Needle pointing right (fast)
        using var needlePen = new Pen(Color.FromArgb(100, 200, 255), 2.5f);
        float cx = 16, cy = 18;
        float angle = 315 * (float)(Math.PI / 180); // ~45 degrees from horizontal
        float nx = cx + 9 * (float)Math.Cos(angle);
        float ny = cy - 9 * (float)Math.Sin(angle);
        g.DrawLine(needlePen, cx, cy, nx, ny);

        // Down arrow (left side, bottom)
        using var downPen = new Pen(Color.FromArgb(80, 200, 120), 2f);
        g.DrawLine(downPen, 8, 24, 8, 30);
        g.DrawLine(downPen, 5, 27, 8, 30);
        g.DrawLine(downPen, 11, 27, 8, 30);

        // Up arrow (right side, bottom)
        using var upPen = new Pen(Color.FromArgb(255, 150, 80), 2f);
        g.DrawLine(upPen, 24, 30, 24, 24);
        g.DrawLine(upPen, 21, 27, 24, 24);
        g.DrawLine(upPen, 27, 27, 24, 24);

        // Center dot
        using var brush = new SolidBrush(Color.FromArgb(220, 220, 220));
        g.FillEllipse(brush, 14, 16, 4, 4);

        IntPtr hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }
}
