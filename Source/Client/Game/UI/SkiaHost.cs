using System;
using System.Diagnostics;
using System.Threading;
using Eto.Forms;
using Eto.Drawing;

namespace Client.Game.UI
{
    // Lightweight Eto+Skia host that runs a UI-thread render loop via UITimer
    public sealed class SkiaHostWindow : Form
    {
        private readonly Drawable _canvas;
        private readonly UITimer _timer;
        private readonly Stopwatch _clock = new();

        public event Action<float>? UpdateFrame;
        public event Action<Graphics>? PaintSurface;

        public SkiaHostWindow()
        {
            Title = "XtremeWorlds";
            ClientSize = new Eto.Drawing.Size(1024, 576);
            _canvas = new Drawable { Size = new Eto.Drawing.Size(1024, 576) };
            _canvas.Paint += (s, pe) =>
            {
                try { PaintSurface?.Invoke(pe.Graphics); } catch { }
            };
            Content = _canvas;

            _timer = new UITimer { Interval = 1.0 / 60.0 }; // 60 FPS target
            _timer.Elapsed += (_, __) => Tick();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _clock.Restart();
            _timer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }

        private void Tick()
        {
            var dt = (float)_clock.Elapsed.TotalSeconds;
            _clock.Restart();
            try { UpdateFrame?.Invoke(dt); } catch { }
            _canvas.Invalidate();
        }
    }
}
