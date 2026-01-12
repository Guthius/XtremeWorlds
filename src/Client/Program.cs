using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Client.Game.UI;
using Client.Game.UI.Windows;
using Client.Net;
using Core.Configurations;
using Core.Globals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Core.Globals.Commands;
using Type = Core.Globals.Type;
using System.IO;
using Core.Objects;

namespace Client
{
    public class GameClient : Microsoft.Xna.Framework.Game
    {
        // Helper: compute direction rows from texture height and configured setting with fallbacks (configured -> 8 -> 4 -> 1)
        // Mirrors legacy heuristic used in DrawPaperdoll (ensuring per-row frame height >= 16 when auto-detecting 8/4).
        public static int ComputeDirectionRows(int textureHeight, int configuredDirs)
        {
            int configured = SettingsManager.Instance.SpriteDirections;
            if (configured <= 0) configured = 4;
            configured = Math.Max(1, configured);
            if (textureHeight <= 0) return 1;
            if (textureHeight % configured == 0) return configured;
            if (configured != 8 && textureHeight % 8 == 0) return 8;
            if (configured != 4 && textureHeight % 4 == 0) return 4;

            return 1;
        }

        // Helper: map logical Direction enum to row index based on supported row count.
        // New ordering requirement (top-left, top-right, down-left, down-right) interpreted for diagonals when 8 directions present.
        // For 4-row sheets we retain legacy Down(0), Right(1), Left(2), Up(3) unless expanded sheet present.
        public static int MapDirectionToRow(Direction dir, int rows)
        {
            if (rows <= 1) return 0;
            // If 8-direction sheet, assume ordering rows 0..7 in enum numeric order.
            if (rows >= 8)
            {
                return (int)dir % rows; // enum expected to align with ordering
            }
            
            // 4-direction fallback mapping: Down, Right, Left, Up
            switch (dir)
            {
                case Direction.Down:
                case Direction.DownLeft:
                case Direction.DownRight:
                    return 0;
                case Direction.Right:
                case Direction.UpRight:
                    return 1;
                case Direction.Left:
                case Direction.UpLeft:
                    return 2;
                case Direction.Up:
                    return 3;
                default:
                    return 0;
            }
        }
        public static GraphicsDeviceManager? Graphics;
        public static SpriteBatch? SpriteBatch;

        public static readonly ConcurrentDictionary<string, Texture2D> TextureCache = new();
        public static readonly ConcurrentDictionary<string, GfxInfo> GfxInfoCache = new();

        private static string? _pendingScreenshotPath;

        private static int _gameFps;
        private static readonly object FpsLock = new();

        // Safely set FPS with a lock
        public static void SetFps(int newFps)
        {
            lock (FpsLock)
                _gameFps = newFps;
        }

        // Safely get FPS with a lock
        public static int GetFps()
        {
            lock (FpsLock)
                return _gameFps;
        }

        // State tracking variables
        // Shared keyboard and mouse states for cross-thread access
        public static KeyboardState CurrentKeyboardState;
        public static KeyboardState PreviousKeyboardState;
        public static MouseState CurrentMouseState;
        public static MouseState PreviousMouseState;

        // Keep track of the key states to avoid repeated input
        public static readonly Dictionary<Keys, bool> KeyStates = new();

        // Define a dictionary to store the last time a key was processed
        public static Dictionary<Keys, DateTime> KeyRepeatTimers = new();

        // Minimum interval (in milliseconds) between repeated key inputs
        private const byte KeyRepeatInterval = 200;

        // Lock object to ensure thread safety
        public static readonly object InputLock = new();

        // Track the previous scroll value to compute delta
        private static readonly object ScrollLock = new();
        private static int _prevScrollWheelValue = 0;

        private TimeSpan _elapsedTime = TimeSpan.Zero;

        public static RenderTarget2D? RenderTarget;
        public static Texture2D? TransparentTexture;
        public static Texture2D? PixelTexture;
        private static RenderTarget2D? _guiRenderTarget; // GUI layer RT (never zoomed)
        // Smoothed camera pivot (native coords) used for composition-time zoom
        private static Vector2 _zoomPivotSmoothed = Vector2.Zero;
        private static bool _zoomPivotInitialized = false;

        // Add a timer to prevent spam
        private static DateTime _lastInputTime = DateTime.MinValue;
        private const int InputCooldown = 250;

        // Handle Escape key to toggle menus
        private static DateTime _lastMouseClickTime = DateTime.MinValue;
        private const int MouseClickCooldown = 250;
        private static DateTime _lastSearchTime = DateTime.MinValue;

        // Ensure this class exists to store graphic info
        public class GfxInfo
        {
            public int Width;
            public int Height;
        }

        public static GfxInfo? GetGfxInfo(string key)
        {
            // Check if the key does not end with ".gfxext" and append if needed
            if (!key.EndsWith(GameState.GfxExt, StringComparison.OrdinalIgnoreCase))
            {
                key += GameState.GfxExt;
            }

            // Ensure the texture is loaded so GfxInfoCache gets populated
            var texture = GetTexture(key) ?? LoadTexture(key);

            if (!GfxInfoCache.TryGetValue(key, out var result) || result is null)
            {
                return null;
            }

            return result;
        }
        
        public GameClient()
        {
            (GameState.ResolutionWidth, GameState.ResolutionHeight) = General.GetResolutionSize(SettingsManager.Instance.Resolution);

            Graphics = new GraphicsDeviceManager(this);

            // Set basic properties for GraphicsDeviceManager
            ref var instance = ref Graphics;
            instance.GraphicsProfile = GraphicsProfile.Reach;
            instance.IsFullScreen = SettingsManager.Instance.Fullscreen;
            instance.PreferredBackBufferWidth = GameState.ResolutionWidth;
            instance.PreferredBackBufferHeight = GameState.ResolutionHeight;
            instance.SynchronizeWithVerticalRetrace = SettingsManager.Instance.Vsync;
            IsFixedTimeStep = false;
            instance.PreferMultiSampling = false;

            // Allow resizing and keep backbuffer in sync with window size when windowed
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnClientSizeChanged;

            // Add handler for PreparingDeviceSettings
            Graphics.PreparingDeviceSettings += (sender, args) =>
            {
                args.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            };

            // Hide OS cursor; we'll render our own
            IsMouseVisible = false;

            Content.RootDirectory = "Content";

            // Handle Exiting: ensure subsystems (like networking) are stopped and
            // forward to Cocca.OnExit so the host window/app can react appropriately.
            Exiting += (s, e) =>
            {
                try { Network.Stop(); } catch { }
                try { Cocca.OnExit(); } catch { }
            };
        }

        private void OnClientSizeChanged(object? sender, EventArgs e)
        {
            // In windowed mode, track the window client size as the backbuffer size
            if (Graphics is null || Graphics.IsFullScreen)
                return;

            var bounds = Window.ClientBounds;

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            if (Graphics.PreferredBackBufferWidth != bounds.Width || Graphics.PreferredBackBufferHeight != bounds.Height)
            {
                Graphics.PreferredBackBufferWidth = bounds.Width;
                Graphics.PreferredBackBufferHeight = bounds.Height;
                try { Graphics.ApplyChanges(); } catch { }
            }
        }

        protected override void Initialize()
        {
            Window.Title = SettingsManager.Instance.GameName;
            
            // Create the RenderTarget2D with the same size as the screen
            RenderTarget = new RenderTarget2D(Graphics?.GraphicsDevice,
                Graphics?.GraphicsDevice.PresentationParameters.BackBufferWidth ?? 0,
                Graphics?.GraphicsDevice.PresentationParameters.BackBufferHeight ?? 0, false,
                Graphics?.GraphicsDevice.PresentationParameters.BackBufferFormat ?? SurfaceFormat.Color, DepthFormat.Depth24);

            // Apply changes to GraphicsDeviceManager
            try
            {
                Graphics?.ApplyChanges();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GraphicsDevice initialization failed: {ex.Message}");
                throw;
            }

            base.Initialize();
        }

        protected override void BeginRun()
        {
            base.BeginRun();
            // Start TCP client after the window has loaded
            try { _ = Network.Start(); }
            catch (Exception ex) { Debug.WriteLine($"Network start error: {ex.Message}"); }
        }

        static void LoadFonts()
        {
            SpriteFont? defaultFont = null;

            var fontValues = Enum.GetValues(typeof(Font));
            for (int i = 1; i < fontValues.Length; i++)
            {
                var val = fontValues.GetValue(i);
                if (val is not Font f)
                    continue;

                try
                {
                    var loaded = LoadFont(DataPath.Fonts, f);
                    TextRenderer.Fonts[f] = loaded;
                    if (defaultFont == null || f == TextRenderer.ConfiguredFont)
                        defaultFont = loaded;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load font {f}: {ex.Message}");
                }
            }

            if (defaultFont == null && TextRenderer.Fonts.Count > 0)
                defaultFont = TextRenderer.Fonts.Values.First();

            if (defaultFont != null)
            {
                for (int i = 1; i < fontValues.Length; i++)
                {
                    var val = fontValues.GetValue(i);
                    if (val is Font f && !TextRenderer.Fonts.ContainsKey(f))
                        TextRenderer.Fonts[f] = defaultFont;
                }
            }
        }

        static void LoadBitmapFonts(GraphicsDevice gd)
        {
            try
            {
                static string? ResolveFontFile(string baseDir, string nameNoExt, string ext)
                {
                    var a = Path.Combine(baseDir, nameNoExt + ext);
                    if (File.Exists(a)) return a;

                    var lower = Path.Combine(baseDir, nameNoExt.ToLowerInvariant() + ext);
                    if (File.Exists(lower)) return lower;

                    var upper = Path.Combine(baseDir, nameNoExt.ToUpperInvariant() + ext);
                    if (File.Exists(upper)) return upper;

                    return null;
                }

                foreach (var v in Enum.GetValues(typeof(Core.Globals.BitmapFont)))
                {
                    if (v is not Core.Globals.BitmapFont bf) continue;
                    var name = bf.ToString();
                    if (string.Equals(name, "None", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var datPath = ResolveFontFile(DataPath.Fonts, name, ".dat");
                    var pngPath = ResolveFontFile(DataPath.Fonts, name, ".png");
                    if (datPath == null || pngPath == null) continue;
                    if (TextRenderer.HasBitmapFont(bf)) continue;
                    try { TextRenderer.LoadLegacyBitmapFont(bf, datPath, pngPath, gd); }
                    catch (Exception ex) { Debug.WriteLine($"Bitmap font load failed {name}: {ex.Message}"); }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Bitmap font loader error: {ex.Message}"); }
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            TransparentTexture = new Texture2D(GraphicsDevice, 1, 1);
            TransparentTexture.SetData([Color.White]);
            PixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            PixelTexture.SetData([Color.White]);

            // Load SpriteFont assets
            LoadFonts();
            // Load legacy bitmap fonts (.dat/.png) via enum + configured setting
            LoadBitmapFonts(GraphicsDevice);

            // Kick off heavy startup work on a background thread
            GameState.IsLoading = true;
            _ = Task.Run(() =>
            {
                try
                {
                    General.Startup();
                }
                finally
                {
                    GameState.IsLoading = false;
                }
            });

            // Preload cursor texture into cache
            try
            {
                var preload = Path.Combine(DataPath.Misc, "Cursor");
                _ = GetTexture(preload);
            }
            catch { }
        }

        public static SpriteFont LoadFont(string path, Font font)
        {
            return General.Client.Content.Load<SpriteFont>(Path.Combine(path, ((int) font).ToString()));
        }

        public static Color ToXnaColor(System.Drawing.Color drawingColor)
        {
            return new Color(drawingColor.R, drawingColor.G, drawingColor.B, drawingColor.A);
        }

        public static System.Drawing.Color ToDrawingColor(Color xnaColor)
        {
            return System.Drawing.Color.FromArgb(xnaColor.A, xnaColor.R, xnaColor.G, xnaColor.B);
        }

        public static void RenderTexture(ref string path, int dX, int dY, int sX, int sY, int dW, int dH, int sW = 1,
            int sH = 1, float alpha = 1.0f, byte red = 255, byte green = 255, byte blue = 255)
        {
            path = DataPath.EnsureFileExtension(path);

            // Retrieve the texture
            var texture = GetTexture(path);

            if (texture is null)
            {
                return;
            }

            // Draw directly in native render-target coordinates. Global composition handles scaling
            // to the backbuffer with pillarbox/letterbox as needed. Avoid using backbuffer sizes here
            // to prevent double-scaling during window resizes.
            var destRect = new Rectangle(dX, dY, dW, dH);
            var srcRect = new Rectangle(sX, sY, sW, sH);
            var color = new Color(red, green, blue, (byte) 255) * alpha;

            SpriteBatch?.Draw(texture, destRect, srcRect, color);
        }

        public static Texture2D? GetTexture(string path)
        {
            if (!TextureCache.ContainsKey(path))
            {
                var texture = LoadTexture(path);
                return texture;
            }

            return TextureCache[path];
        }

        public static Texture2D? LoadTexture(string path)
        {
            try
            {
                // Check if the key does not end with ".gfxext" and append if needed  
                if (!path.EndsWith(GameState.GfxExt, StringComparison.OrdinalIgnoreCase))
                {
                    path += GameState.GfxExt;
                }
                
                if (!File.Exists(path))
                {
                    return null;
                }
                
                // Open the file stream with FileShare.Read to allow other processes to read the file  
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var texture = Texture2D.FromStream(Graphics?.GraphicsDevice!, stream);

                    // Cache graphics information  
                    var gfxInfo = new GfxInfo()
                    {
                        Width = texture.Width,
                        Height = texture.Height
                    };
                    GfxInfoCache.TryAdd(path, gfxInfo);

                    TextureCache[path] = texture;

                    return texture;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading texture '{path}': {ex.Message}", ex);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // If graphics or sprite batch aren’t ready yet, skip this frame safely
            if (Graphics == null || Graphics.GraphicsDevice == null || SpriteBatch == null)
            {
                base.Draw(gameTime);
                return;
            }

            var gd = Graphics.GraphicsDevice;

            gd.Clear(Color.Black);

            // Update GUI mouse position before drawing GUI (ensures correct UI hover/click)
            var mousePosGame = GetMousePosition("game");
            GameState.CurMouseXGame = mousePosGame.Item1;
            GameState.CurMouseYGame = mousePosGame.Item2;

            var ppNow = gd.PresentationParameters;
            int bbWNow = ppNow.BackBufferWidth;
            int bbHNow = ppNow.BackBufferHeight;
            bool isFullscreenNow = Graphics.IsFullScreen;
            int nativeWidth, nativeHeight;
            if (isFullscreenNow)
            {
                var sel = General.GetResolutionSize(SettingsManager.Instance.Resolution);
                int w = sel.Item1;
                int h = sel.Item2;
                float aspect = 16f / 9f;

                // Recompute height from width to enforce 16:9
                h = (int)Math.Round(w / aspect);
                nativeWidth = Math.Max(1, w);
                nativeHeight = Math.Max(1, h);
            }
            else
            {
                // In windowed mode, keep native at selected resolution and SCALE to fit window
                var sel = General.GetResolutionSize(SettingsManager.Instance.Resolution);
                nativeWidth = Math.Max(1, sel.Item1);
                nativeHeight = Math.Max(1, sel.Item2);
            }
            // Update effective native size and trigger GUI rebuild if changed
            bool guiSizeChanged = GameState.ResolutionWidth != nativeWidth || GameState.ResolutionHeight != nativeHeight;
            GameState.ResolutionWidth = nativeWidth;
            GameState.ResolutionHeight = nativeHeight;

            // Only recreate if needed
            if (RenderTarget == null || RenderTarget.Width != nativeWidth || RenderTarget.Height != nativeHeight)
            {
                if (RenderTarget != null)
                    RenderTarget.Dispose();
                RenderTarget = new RenderTarget2D(GraphicsDevice, nativeWidth, nativeHeight, false, GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            }

            // --- Render game/menu to RenderTarget (zoomed) ---
            GraphicsDevice.SetRenderTarget(RenderTarget);
            GraphicsDevice.Clear(Color.Black);

            if (GameState.IsLoading || GameState.GettingMap)
            {
                // Optional: draw a simple loading screen here if desired
            }
            else
            {
                // Draw the actual game onto the RenderTarget
                SpriteBatch?.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
                Render_Game();
                SpriteBatch?.End();
            }

            // After drawing to RenderTarget, reset to back buffer for composition
            GraphicsDevice.SetRenderTarget(null);

            // --- Render GUI to guiRenderTarget (not zoomed) ---
            // Only render UI when we're not loading/getting map; during map load we intentionally draw nothing.
            if (!GameState.GettingMap && !GameState.IsLoading)
            {
                if (_guiRenderTarget == null || _guiRenderTarget.Width != nativeWidth || _guiRenderTarget.Height != nativeHeight)
                {
                    _guiRenderTarget?.Dispose();
                    _guiRenderTarget = new RenderTarget2D(GraphicsDevice, nativeWidth, nativeHeight, false, GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
                }

                // Update GUI mouse position before drawing GUI (ensures correct UI hover/click)
                // This uses only GUI scale, not game zoom, so GUI input is always correct regardless of zoom
                var mousePosGui = GetMousePosition("gui");
                GameState.CurMouseXGui = mousePosGui.Item1;
                GameState.CurMouseYGui = mousePosGui.Item2;

                GraphicsDevice.SetRenderTarget(_guiRenderTarget);
                GraphicsDevice.Clear(Color.Transparent);
                if (SpriteBatch != null)
                {
                    SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
                    if (GameState.InMenu)
                        WindowManager.DrawMenuBackground();
                    WindowManager.Render();
                    TextRenderer.DrawMapName();

                    // Draw custom cursor on the GUI layer at GUI mouse coords
                    if (GameState.CurMouseXGui >= 0 && GameState.CurMouseYGui >= 0)
                    {
                        string cursorTex = Path.Combine(DataPath.Misc, "Cursor");
                        var info = GetGfxInfo(cursorTex);
                        if (info != null)
                        {
                            int cw = Math.Max(1, info.Width);
                            int ch = Math.Max(1, info.Height);
                            int hotspotX = 0; // adjust if your cursor hotspot is not top-left
                            int hotspotY = 0;
                            int cx = GameState.CurMouseXGui - hotspotX;
                            int cy = GameState.CurMouseYGui - hotspotY;
                            RenderTexture(ref cursorTex, cx, cy, 0, 0, cw, ch, cw, ch);
                        }
                    }

                    SpriteBatch.End();
                }

                // After drawing to _guiRenderTarget, reset to back buffer
                GraphicsDevice.SetRenderTarget(null);
            }

            // Always composite to the back buffer so we don't leave the previous frame on screen.
            // When GettingMap=true, RenderTarget is cleared to black above, so the game won't be drawn.
            int backBufferWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int backBufferHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
            bool isFullscreenNow2 = Graphics?.IsFullScreen ?? false;
            var viewportRect = ComputeViewportRect(backBufferWidth, backBufferHeight, nativeWidth, nativeHeight, isFullscreenNow2);

            // Update smoothed pivot and compute destination rects (world uses zoomedRect, GUI uses viewportRect)
            UpdateSmoothedPivot(nativeWidth, nativeHeight);
            float zoomNow = GameState.CameraZoom <= 0 ? 1.0f : GameState.CameraZoom;
            var zoomedRect = ComputeZoomedDestRect(viewportRect, nativeWidth, nativeHeight, zoomNow);

            using (var targetBatch = new SpriteBatch(GraphicsDevice))
            {
                targetBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

                // Draw the game/menu with zoom (this will be black while GettingMap/IsLoading)
                if (RenderTarget != null)
                    targetBatch.Draw(RenderTarget, zoomedRect, Color.White);

                // Draw GUI un-zoomed to the viewport only when active
                if (!GameState.GettingMap && !GameState.IsLoading && _guiRenderTarget != null)
                    targetBatch.Draw(_guiRenderTarget, viewportRect, Color.White);

                targetBatch.End();
            }

            base.Draw(gameTime);
        }

        // Compute the base destination rectangle that preserves aspect ratio (pillarbox/letterbox)
        private static Rectangle ComputeViewportRect(int backBufferWidth, int backBufferHeight, int nativeWidth, int nativeHeight, bool isFullscreen)
        {
            if (backBufferWidth <= 0 || backBufferHeight <= 0 || nativeWidth <= 0 || nativeHeight <= 0)
                return new Rectangle(0, 0, Math.Max(1, backBufferWidth), Math.Max(1, backBufferHeight));

            if (isFullscreen)
            {
                float targetAspect = 16f / 9f; // enforced in fullscreen
                int height = backBufferHeight;
                int width = (int)(height * targetAspect);
                int x = (backBufferWidth - width) / 2;
                return new Rectangle(x, 0, width, height);
            }
            else
            {
                float targetAspect = (float)nativeWidth / nativeHeight;
                float screenAspect = (float)backBufferWidth / backBufferHeight;
                if (screenAspect > targetAspect)
                {
                    // Pillarbox: full height
                    int height = backBufferHeight;
                    int width = (int)(height * targetAspect);
                    int x = (backBufferWidth - width) / 2;
                    return new Rectangle(x, 0, width, height);
                }
                else
                {
                    // Letterbox: full width
                    int width = backBufferWidth;
                    int height = (int)(width / targetAspect);
                    int y = (backBufferHeight - height) / 2;
                    return new Rectangle(0, y, width, height);
                }
            }
        }

        // Compute a zoomed destination rectangle around the player/target pivot (native space)
        private static Rectangle ComputeZoomedDestRect(Rectangle viewportRect, int nativeWidth, int nativeHeight, float zoom)
        {
            if (zoom <= 0) zoom = 1.0f;

            // Use smoothed pivot (initialized lazily)
            var pivotNative = _zoomPivotInitialized ? _zoomPivotSmoothed : GetZoomPivotNative();

            // Screen position where the pivot should remain (unzoomed mapping)
            float sx = viewportRect.X + (pivotNative.X / nativeWidth) * viewportRect.Width;
            float sy = viewportRect.Y + (pivotNative.Y / nativeHeight) * viewportRect.Height;

            // Size scales with zoom
            int dw = (int)Math.Round(viewportRect.Width * zoom);
            int dh = (int)Math.Round(viewportRect.Height * zoom);

            // Position so that pivot maps to the same screen position
            int dx = (int)Math.Round(sx - (pivotNative.X / nativeWidth) * dw);
            int dy = (int)Math.Round(sy - (pivotNative.Y / nativeHeight) * dh);

            return new Rectangle(dx, dy, dw, dh);
        }

        // Smooth the pivot toward current player/target native position
        private static void UpdateSmoothedPivot(int nativeWidth, int nativeHeight)
        {
            var desired = GetZoomPivotNative();
            if (!_zoomPivotInitialized || float.IsNaN(_zoomPivotSmoothed.X) || float.IsNaN(_zoomPivotSmoothed.Y))
            {
                _zoomPivotSmoothed = desired;
                _zoomPivotInitialized = true;
                return;
            }
            // Lerp factor: tune 0..1. Closer to 1 is snappier. Use frame-rate independent smoothing.
            // Assuming ~60 FPS here; we can do a fixed small factor.
            const float alpha = 0.15f; // gentle smoothing
            _zoomPivotSmoothed = Vector2.Lerp(_zoomPivotSmoothed, desired, alpha);
            // Clamp within native bounds (defensive)
            _zoomPivotSmoothed.X = Math.Clamp(_zoomPivotSmoothed.X, 0, nativeWidth);
            _zoomPivotSmoothed.Y = Math.Clamp(_zoomPivotSmoothed.Y, 0, nativeHeight);
        }

        protected override void Update(GameTime gameTime)
        {
            // During background loading, keep updates minimal and skip input/UI logic
            if (GameState.IsLoading)
            {
                ResetInputStates();
                base.Update(gameTime);
                return;
            }

            // Ignore input if the window is minimized or inactive
            if ((!IsActive || Window.ClientBounds.Width == 0) | Window.ClientBounds.Height == 0)
            {
                ResetInputStates();
                base.Update(gameTime);
                return;
            }

            lock (InputLock)
            {
                UpdateMouseCache();
                UpdateKeyCache();
                ProcessInputs();
            }

            if (GameState.MyEditorType == EditorType.Map)
            {
                if (IsKeyStateActive(Keys.Z))
                {
                    Editors.Undo();
                }

                if (IsKeyStateActive(Keys.Y))
                {
                    Editors.Redo();
                }
            }

            // Camera zoom with mouse wheel
            int currentWheel = Mouse.GetState().ScrollWheelValue;
            if (currentWheel != _prevScrollWheelValue)
            {
                int delta = currentWheel - _prevScrollWheelValue;
                // Block zoom while over any GUI window
                bool overGui = WindowManager.IsMouseOverAnyWindow;
                if (delta != 0 && !overGui)
                {
                    float zoomDelta = delta > 0 ? 0.1f : -0.1f;
                    GameState.CameraZoom += zoomDelta;

                    float minZoom = 0.5f;
                    float maxZoom = 2.0f;
                    int mapIndex = GetPlayerMap(GameState.MyIndex);
                    if (mapIndex >= 0 && mapIndex < Client.Map.Instance.Count)
                    {
                        minZoom = Client.Map.Instance[mapIndex].MinZoom;
                        maxZoom = Client.Map.Instance[mapIndex].MaxZoom;
                    }

                    if (minZoom <= 0) minZoom = 0.5f;
                    if (maxZoom <= 0) maxZoom = 2.0f;
                    if (maxZoom < minZoom) maxZoom = minZoom;

                    GameState.CameraZoom = Math.Clamp(GameState.CameraZoom, minZoom, maxZoom);
                }
                _prevScrollWheelValue = currentWheel;
            }

            if (IsKeyStateActive(Keys.F12))
            {
                TakeScreenshot();
            }

            if (_pendingScreenshotPath != "")
            {
                TrySaveBackbufferScreenshot(_pendingScreenshotPath);
            }

            SetFps(_gameFps + 1);
            _elapsedTime += gameTime.ElapsedGameTime;

            if (_elapsedTime.TotalSeconds >= 1d)
            {
                SetFps(0);
                
                _elapsedTime = TimeSpan.Zero;
            }

            Loop.OnStart();

            base.Update(gameTime);
        }

        // Reset keyboard and mouse states
        private static void ResetInputStates()
        {
            CurrentKeyboardState = new KeyboardState();
            PreviousKeyboardState = new KeyboardState();
            CurrentMouseState = new MouseState();
            PreviousMouseState = new MouseState();
        }

        private static void UpdateKeyCache()
        {
            // Get the current keyboard state
            var keyboardState = Keyboard.GetState();

            // Update the previous and current states
            PreviousKeyboardState = CurrentKeyboardState;
            CurrentKeyboardState = keyboardState;
        }

        private static void UpdateMouseCache()
        {
            // Get the current mouse state
            var mouseState = Mouse.GetState();

            // Update the previous and current states
            PreviousMouseState = CurrentMouseState;
            CurrentMouseState = mouseState;
        }

        public static int GetMouseScrollDelta()
        {
            lock (ScrollLock)
                // Calculate the scroll delta between the previous and current states
                return CurrentMouseState.ScrollWheelValue - PreviousMouseState.ScrollWheelValue;
        }

        public static bool IsKeyStateActive(Keys key)
        {
            if (CanProcessKey(key) == true)
            {
                // Check if the key is down in the current keyboard state
                return CurrentKeyboardState.IsKeyDown(key);
            }

            return default;
        }

        public static Tuple<int, int> GetMousePosition(string mode = "gui")
        {
            int mouseX = CurrentMouseState.X;
            int mouseY = CurrentMouseState.Y;

            int nW = GameState.ResolutionWidth;
            int nH = GameState.ResolutionHeight;
            int bbW = Graphics?.GraphicsDevice.PresentationParameters.BackBufferWidth ?? 0;
            int bbH = Graphics?.GraphicsDevice.PresentationParameters.BackBufferHeight ?? 0;
            bool isFs = Graphics?.IsFullScreen ?? false;
            if (nW <= 0 || nH <= 0 || bbW <= 0 || bbH <= 0)
                return new Tuple<int, int>(mouseX, mouseY);

            // Compute base viewport rect same as Draw
            var viewport = ComputeViewportRect(bbW, bbH, nW, nH, isFs);
            if (mouseX < viewport.X || mouseY < viewport.Y || mouseX >= viewport.Right || mouseY >= viewport.Bottom)
                return new Tuple<int, int>(-1, -1);

            if (string.Equals(mode, "game", StringComparison.OrdinalIgnoreCase))
            {
                // Inverse zoom around the same pivot used in Draw()
                float zoom = GameState.CameraZoom <= 0 ? 1.0f : GameState.CameraZoom;
                // Ensure pivot is initialized/smoothed
                UpdateSmoothedPivot(nW, nH);
                Vector2 pivotNative = _zoomPivotInitialized ? _zoomPivotSmoothed : GetZoomPivotNative();
                // pivot screen position in unzoomed viewport space
                var pivotScreen = new Vector2(
                    viewport.X + (pivotNative.X / nW) * viewport.Width,
                    viewport.Y + (pivotNative.Y / nH) * viewport.Height
                );
                // unscale screen point back to unzoomed screen around pivot
                var unzoomedScreen = new Vector2(
                    (mouseX - pivotScreen.X) / zoom + pivotScreen.X,
                    (mouseY - pivotScreen.Y) / zoom + pivotScreen.Y
                );
                float sx = (unzoomedScreen.X - viewport.X) / viewport.Width;
                float sy = (unzoomedScreen.Y - viewport.Y) / viewport.Height;
                int mx = (int)(sx * nW);
                int my = (int)(sy * nH);
                return new Tuple<int, int>(mx, my);
            }
            else
            {
                // GUI: ignore zoom, just map viewport to native
                float sx = (float)(mouseX - viewport.X) / viewport.Width;
                float sy = (float)(mouseY - viewport.Y) / viewport.Height;
                int mx = (int)(sx * nW);
                int my = (int)(sy * nH);
                return new Tuple<int, int>(mx, my);
            }
        }

        // Compute the native-space pivot for zooming: target center if valid, else player center
        private static Vector2 GetZoomPivotNative()
        {
            if (Player.Instance.Count < GameState.MyIndex)
                return new Vector2(GameState.ResolutionWidth / 2, GameState.ResolutionHeight / 2);
            if (!GameState.InGame) return new Vector2(GameState.ResolutionWidth / 2, GameState.ResolutionHeight / 2);
            
            int worldX = GetPlayerRawX(GameState.MyIndex) + Constants.TileSize / 2;
            int worldY = GetPlayerRawY(GameState.MyIndex) + Constants.TileSize / 2;

            if (GameState.MyTarget >= 0)
            {
                if (GameState.MyTargetType == (int)TargetType.Player)
                {
                    int t = GameState.MyTarget;
                    if (IsPlaying(t))
                    {
                        // Same map check
                        if (Player.Instance[t].Map == Player.Instance[GameState.MyIndex].Map)
                        {
                            worldX = GetPlayerRawX(t) + Constants.TileSize / 2;
                            worldY = GetPlayerRawY(t) + Constants.TileSize / 2;
                        }
                    }
                }
                else if (GameState.MyTargetType == (int)TargetType.Npc)
                {
                    int n = GameState.MyTarget;
                    if (n >= 0 && n < MapNpc.Instance.Length && MapNpc.Instance[n].Num >= 0)
                    {
                        worldX = MapNpc.Instance[n].X + Constants.TileSize / 2;
                        worldY = MapNpc.Instance[n].Y + Constants.TileSize / 2;
                    }
                }
            }

            int px = GameLogic.ConvertMapX(worldX);
            int py = GameLogic.ConvertMapY(worldY);
            return new Vector2(px, py);
        }

        public static bool IsMouseButtonDown(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                {
                    return CurrentMouseState.LeftButton == ButtonState.Pressed;
                }
                case MouseButton.Right:
                {
                    return CurrentMouseState.RightButton == ButtonState.Pressed;
                }
                case MouseButton.Middle:
                {
                    return CurrentMouseState.MiddleButton == ButtonState.Pressed;
                }

                default:
                {
                    return false;
                }
            }
        }

        public static bool IsMouseButtonUp(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                {
                    return CurrentMouseState.LeftButton == ButtonState.Released;
                }
                case MouseButton.Right:
                {
                    return CurrentMouseState.RightButton == ButtonState.Released;
                }
                case MouseButton.Middle:
                {
                    return CurrentMouseState.MiddleButton == ButtonState.Released;
                }

                default:
                {
                    return false;
                }
            }
        }

        public static void ProcessInputs()
        {
            // Game context
            var mousePosGame = GetMousePosition("game");
            int mouseXGame = mousePosGame.Item1;
            int mouseYGame = mousePosGame.Item2;
            GameState.CurMouseXGame = mouseXGame;
            GameState.CurMouseYGame = mouseYGame;

            if (mouseXGame >= 0 && mouseYGame >= 0)
            {
                // Absolute world tile under the mouse: floor((cameraOffsetPx + mousePx) / tileSize)
                GameState.CurXGame = (int)Math.Floor((GameState.Camera.Left + mouseXGame) / (double)Constants.TileSize);
                GameState.CurYGame = (int)Math.Floor((GameState.Camera.Top + mouseYGame) / (double)Constants.TileSize);
            }
            else
            {
                GameState.CurXGame = -1;
                GameState.CurYGame = -1;
            }

            // GUI context
            var mousePosGui = GetMousePosition("gui");
            int mouseXGui = mousePosGui.Item1;
            int mouseYGui = mousePosGui.Item2;
            GameState.CurMouseXGui = mouseXGui;
            GameState.CurMouseYGui = mouseYGui;
            if (mouseXGui >= 0 && mouseYGui >= 0)
            {
                // GUI maps to native space; still convert to absolute world tile using camera offsets
                GameState.CurXGui = (int)Math.Floor((GameState.Camera.Left + mouseXGui) / (double)Constants.TileSize);
                GameState.CurYGui = (int)Math.Floor((GameState.Camera.Top + mouseYGui) / (double)Constants.TileSize);
            }
            else
            {
                GameState.CurXGui = -1;
                GameState.CurYGui = -1;
            }

            // For compatibility, set legacy variables to GAME context by default (for targeting, etc)
            GameState.CurX = GameState.CurXGame;
            GameState.CurY = GameState.CurYGame;
            GameState.CurMouseX = GameState.CurMouseXGame;
            GameState.CurMouseY = GameState.CurMouseYGame;

            // Check for action keys
            GameState.VbKeyControl = CurrentKeyboardState.IsKeyDown(Keys.LeftControl);
            GameState.VbKeyShift = CurrentKeyboardState.IsKeyDown(Keys.LeftShift);

            if (IsKeyStateActive(Keys.F8))
            {
                var uiPath = Path.Combine(DataPath.Skins, SettingsManager.Instance.Skin + ".cs");

                if (!File.Exists(uiPath))
                {
                    Console.WriteLine($"File not found: {uiPath}");
                }
                else
                {
                    // Open with default text editor
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = uiPath,
                        UseShellExecute = true
                    });
                }
            }

            if (IsKeyStateActive(Keys.F5))
            {
                UIScript.Load();
                WindowManager.Init();
            }

            // Handle Escape key to toggle menus or cancel casts
            if (IsKeyStateActive(Keys.Escape))
            {
                // If an event prompt is open, cancel/abort it.
                if (IsWindowVisible("winEventChat"))
                {
                    WinEventChat.OnCancel();
                    return;
                }

                // If we're casting/buffering a skill, cancel it first
                if (GameState.SkillBuffer >= 0)
                {
                    GameState.SkillBuffer = -1;
                    GameState.SkillBufferTimer = 0;
                    Sender.SendCancelCast();
                    return;
                }

                // First: clear target with server if one is selected
                int prevTarget = GameState.MyTarget;
                int prevTargetType = GameState.MyTargetType;
                if (prevTarget >= 0 && prevTargetType >= 0)
                {
                    int? clearTileX = null;
                    int? clearTileY = null;
                    if (prevTargetType == (int)TargetType.Player)
                    {
                        if (IsPlaying(prevTarget) && GetPlayerMap(prevTarget) == GetPlayerMap(GameState.MyIndex))
                        {
                            clearTileX = GetPlayerX(prevTarget);
                            clearTileY = GetPlayerY(prevTarget);
                        }
                    }
                    else if (prevTargetType == (int)TargetType.Npc)
                    {
                        if (prevTarget >= 0 && prevTarget < MapNpc.Instance.Length && MapNpc.Instance[prevTarget].Num >= 0)
                        {
                            clearTileX = (int)Math.Floor(MapNpc.Instance[prevTarget].X / (double)Constants.TileSize);
                            clearTileY = (int)Math.Floor(MapNpc.Instance[prevTarget].Y / (double)Constants.TileSize);
                        }
                    }

                    // Clear locally first per requirement
                    GameState.MyTarget = -1;
                    GameState.MyTargetType = 0;

                    // Notify server to toggle/clear the same tile
                    if (clearTileX.HasValue && clearTileY.HasValue)
                    {
                        Sender.SendPlayerSearch(clearTileX.Value, clearTileY.Value, 0);
                    }

                    // If we just cleared a target, stop here (don’t open/close menus this press)
                    return;
                }

                if (GameState.InMenu == true)
                    return;

                // Hide options screen
                if (IsWindowVisible("winOptions"))
                {
                    WindowManager.HideWindow("winOptions");
                    WinComboMenu.Close();
                    return;
                }

                // hide/show chat window
                if (IsWindowVisible("winChat"))
                {
                    if (WindowManager.TryGetControl("winChat", "txtChat", out var chatCtrl))
                    {
                        chatCtrl!.Text = "";
                    }
                    WinChat.Hide();
                    return;
                }

                if (IsWindowVisible("winEscMenu"))
                {
                    WindowManager.HideWindow("winEscMenu");
                    return;
                }

                if (IsWindowVisible("winShop"))
                {
                    Shop.OnClose();
                    return;
                }

                if (IsWindowVisible("winBank"))
                {
                    Sender.SendCloseBank();
                    return;
                }

                if (IsWindowVisible("winTrade"))
                {
                    Sender.SendDeclineTrade();
                    return;
                }

                if (IsWindowVisible("winInventory"))
                {
                    WindowManager.HideWindow("winInventory");
                    return;
                }

                if (IsWindowVisible("winCharacter"))
                {
                    WindowManager.HideWindow("winCharacter");
                    return;
                }

                if (IsWindowVisible("winSkills"))
                {
                    WindowManager.HideWindow("winSkills");
                    return;
                }

                // show them
                if (!IsWindowVisible("winChat"))
                {
                    WindowManager.ShowWindow("winEscMenu", true);
                    return;
                }
            }

            if (GameState.InGame)
            {
                if (Player.Instance.Count >= GameState.MyIndex)
                {
                    if (CurrentKeyboardState.IsKeyDown(Keys.Space) || (IsMouseButtonDown(MouseButton.Left) && GameState.CurX == GetPlayerX(GameState.MyIndex) && GameState.CurY == GetPlayerY(GameState.MyIndex)))
                    {
                        GameLogic.CheckMapGetItem();
                    }
                }
            }

            if (CurrentKeyboardState.IsKeyDown(Keys.Insert))
            {
                Sender.SendRequestAdmin();
            }

            HandleMouseInputs();
            HandleActiveWindowInput();
            HandleTextInput();

            if (GameState.InGame)
            {
                // Check for movement keys
                UpdateMovementKeys();
                HandleHotbarInput();

                // Exit if escape menu is open
                if (IsWindowVisible("winEscMenu"))
                    return;

                // Check for input cooldown
                if (!IsInputCooldownElapsed())
                    return;

                // Process toggle actions
                HandleWindowToggle(Keys.I, "winInventory", WinMenu.OnInventoryClick);
                HandleWindowToggle(Keys.C, "winCharacter", WinMenu.OnCharacterClick);
                HandleWindowToggle(Keys.K, "winSkills", WinMenu.OnSkillsClick);

                // Handle chat input (only once per key press)
                if (IsKeyPressedOnce(Keys.Enter))
                {
                    if (IsWindowVisible("winChatSmall"))
                    {
                        WinChat.Show();
                        GameState.InSmallChat = false;
                    }
                    else
                    {
                        GameLogic.HandlePressEnter();
                    }

                    UpdateLastInputTime();
                }
            }
        }

        // Helper methods
        private static void UpdateMovementKeys()
        {
            if (IsWindowVisible("winChat"))
            {
                GameState.DirUp = false;
                GameState.DirDown = false;
                GameState.DirLeft = false;
                GameState.DirRight = false;
                return;
            }

            GameState.DirUp = CurrentKeyboardState.IsKeyDown(Keys.W) | CurrentKeyboardState.IsKeyDown(Keys.Up);
            GameState.DirDown = CurrentKeyboardState.IsKeyDown(Keys.S) | CurrentKeyboardState.IsKeyDown(Keys.Down);
            GameState.DirLeft = CurrentKeyboardState.IsKeyDown(Keys.A) | CurrentKeyboardState.IsKeyDown(Keys.Left);
            GameState.DirRight = CurrentKeyboardState.IsKeyDown(Keys.D) | CurrentKeyboardState.IsKeyDown(Keys.Right);
        }

        public static bool IsWindowVisible(string windowName)
        {
            return WindowManager.TryGetWindow(windowName, out var window) && window!.Visible;
        }

        private static bool IsInputCooldownElapsed()
        {
            return (DateTime.Now - _lastInputTime).TotalMilliseconds >= InputCooldown;
        }

        private static bool IsSearchCooldownElapsed()
        {
            return (DateTime.Now - _lastSearchTime).TotalMilliseconds >= InputCooldown;
        }

        private static void UpdateLastInputTime()
        {
            _lastInputTime = DateTime.Now;
        }

        private static void HandleWindowToggle(Keys key, string windowName, Action toggleAction)
        {
            if (CurrentKeyboardState.IsKeyDown(key) && !IsWindowVisible("winChat"))
            {
                toggleAction.Invoke();
                UpdateLastInputTime();
            }
        }

        private static void HandleActiveWindowInput()
        {
            // Check if there is an active window and that it is visible.
            if (WindowManager.ActiveWindow is not null && WindowManager.ActiveWindow.Visible)
            {
                // Check if an active control exists.
                if (WindowManager.ActiveWindow.ActiveControl is not null)
                {
                    // Get the active control.
                    var activeControl = WindowManager.ActiveWindow.ActiveControl;

                    // Check if the Enter key is active and can be processed.
                    if (IsKeyStateActive(Keys.Enter))
                    {
                        // Handle Enter: Call the control's callback or activate a new control.
                        activeControl.CallBack[(int) ControlState.FocusEnter]?.Invoke();
                    }

                    // Check if the Tab key is active and can be processed
                    if (IsKeyStateActive(Keys.Tab))
                    {
                        WindowManager.FocusNextControl();
                    }
                }
            }
        }

        // Handles the hotbar key presses using KeyboardState
        private static void HandleHotbarInput()
        {
            if (GameState.InSmallChat)
            {
                // Iterate through hotbar slots and check for corresponding keys
                for (int i = 0; i < Variables.MaxHotbar; i++)
                {
                    // Check if the corresponding hotbar key is pressed
                    if (CurrentKeyboardState.IsKeyDown((Keys) ((int) Keys.D0 + (i + 1))))
                    {
                        Sender.SendUseHotbarSlot(i);
                        return; // Exit once the matching slot is used
                    }
                }
            }
        }

        private static void HandleTextInput()
        {
            // Iterate over all pressed keys  
            foreach (Keys key in CurrentKeyboardState.GetPressedKeys())
            {
                // Check for special keys and skip processing
                if (key == Keys.Tab || key == Keys.LeftShift || key == Keys.RightShift || key == Keys.LeftControl ||
                    key == Keys.RightControl || key == Keys.LeftAlt || key == Keys.RightAlt)
                {
                    continue;
                }

                if (IsKeyStateActive(key))
                {
                    // Handle Backspace key separately  
                    if (key == Keys.Back)
                    {
                        var activeControl = WindowManager.GetActiveControl();

                        if (activeControl is not null && activeControl.Visible && activeControl.Text.Length > 0)
                        {
                            // Modify the text and update it back in the window  
                            activeControl.Text = activeControl.Text.Substring(0, activeControl.Text.Length - 1);
                            WindowManager.UpdateActiveControl(activeControl);
                        }

                        continue; // Move to the next key  
                    }

                    // Convert key to a character, considering Shift key  
                    char? character = ConvertKeyToChar(key, CurrentKeyboardState.IsKeyDown(Keys.LeftShift));

                    // If the character is valid, update the active control's text  
                    if (character.HasValue)
                    {
                        var activeControl = WindowManager.GetActiveControl();

                        if (activeControl is not null && activeControl.Visible && activeControl.Enabled)
                        {
                            string text = activeControl.Text + character.Value;
                            if (TextRenderer.GetTextWidth(text) < activeControl.Width)
                            {
                                // Append character to the control's text  
                                activeControl.Text += character.Value;
                                WindowManager.UpdateActiveControl(activeControl);
                                continue; // Move to the next key  
                            }
                        }
                    }

                    KeyStates.Remove(key);
                    KeyRepeatTimers.Remove(key);
                }
            }
        }

        // Check if the key can be processed (with interval-based repeat logic)
        private static bool CanProcessKey(Keys key)
        {
            var now = DateTime.Now;
            if (CurrentKeyboardState.IsKeyDown(key))
            {
                if (IsKeyPressedOnce(key) || !KeyRepeatTimers.ContainsKey(key) ||
                    (now - KeyRepeatTimers[key]).TotalMilliseconds >= KeyRepeatInterval)
                {
                    // If the key is released, remove it from KeyStates and reset the timer
                    KeyStates.Remove(key);
                    KeyRepeatTimers.Remove(key);
                    KeyRepeatTimers[key] = now; // Update the timer for the key
                    return true;
                }
            }

            return false;
        }

        private static bool IsKeyPressedOnce(Keys key)
        {
            return CurrentKeyboardState.IsKeyDown(key) && PreviousKeyboardState.IsKeyUp(key);
        }

        // Convert a key to a character (if possible)
        private static char? ConvertKeyToChar(Keys key, bool shiftPressed)
        {
            // Handle alphabetic keys
            if (key >= Keys.A && key <= Keys.Z)
            {
                char baseChar = (char)('A' + ((int)key - (int)Keys.A));
                return shiftPressed ? baseChar : char.ToLower(baseChar);
            }

            // Handle numeric keys (0-9)
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                char digit = (char)('0' + ((int)key - (int)Keys.D0));
                return shiftPressed ? General.GetShiftedDigit(digit) : digit;
            }

            // Handle space key
            if (key == Keys.Space)
                return ' ';

            // Handle the minus/hyphen key
            if (key == Keys.OemMinus)
            {
                return shiftPressed ? '_' : '-';
            }

            // Handle the "/" character (typically mapped to OemQuestion)
            if (key == Keys.OemQuestion)
            {
                return shiftPressed ? '?' : '/';
            }

            // Handle period/decimal point
            if (key == Keys.OemPeriod)
            {
                return shiftPressed ? '>' : '.';
            }

            // Ignore unsupported keys (e.g., function keys, control keys)
            return null;
        }

        private static void HandleMouseInputs()
        {
            HandleMouseClick();
            HandleScrollWheel();
        }

        // Ensure GUI event handlers receive GUI-scaled coordinates
        private static void HandleGuiEvent(ControlState state)
        {
            // Save legacy game-context values
            int prevMouseX = GameState.CurMouseX;
            int prevMouseY = GameState.CurMouseY;
            int prevCurX = GameState.CurX;
            int prevCurY = GameState.CurY;

            // Swap to GUI-space values
            GameState.CurMouseX = GameState.CurMouseXGui;
            GameState.CurMouseY = GameState.CurMouseYGui;
            GameState.CurX = GameState.CurXGui;
            GameState.CurY = GameState.CurYGui;

            // Dispatch the GUI event
            WindowManager.OnUpdate(state);
            
            GameState.CurMouseX = prevMouseX;
            GameState.CurMouseY = prevMouseY;
            GameState.CurX = prevCurX;
            GameState.CurY = prevCurY;
        }

        private static void HandleScrollWheel()
        {
            // Dispatch wheel events to GUI and chat for both directions
            int scrollValue = GetMouseScrollDelta();
            if (scrollValue == 0)
            {
                return;
            }

            // First, let GUI controls react (e.g., combo menus, custom lists)
            HandleGuiEvent(ControlState.MouseScroll);

            // Then apply default chat scrolling behavior
            if (scrollValue > 0)
            {
                GameLogic.ScrollChatBox(0); // up
            }
            else
            {
                GameLogic.ScrollChatBox(1); // down
            }
        }

        private static void HandleMouseClick()
        {
            int currentTime = Environment.TickCount;

            // Handle MouseMove event when the mouse moves
            if (CurrentMouseState.X != PreviousMouseState.X || CurrentMouseState.Y != PreviousMouseState.Y)
            {
                HandleGuiEvent(ControlState.MouseMove);
            }

            // Check for MouseDown event (button pressed)
            if (IsMouseButtonDown(MouseButton.Left))
            {
                if ((DateTime.Now - _lastMouseClickTime).TotalMilliseconds >= MouseClickCooldown)
                {
                    HandleGuiEvent(ControlState.MouseDown);
                    _lastMouseClickTime = DateTime.Now; // Update last mouse click time
                    GameState.LastLeftClickTime = currentTime; // Track time for double-click detection
                    GameState.ClickCount++;
                }

                if (GameState.ClickCount >= 2)
                {
                    HandleGuiEvent(ControlState.DoubleClick);
                }
            }

            // Double-click detection for left button
            if ((DateTime.Now - _lastMouseClickTime).TotalMilliseconds >= GameState.DoubleClickTImer)
            {
                GameState.ClickCount = 0;
                GameState.Info = false;
            }

            // Check for MouseUp event (button released)
            if (IsMouseButtonUp(MouseButton.Left))
            {
                HandleGuiEvent(ControlState.MouseUp);
            }

            for (int i = 1; i < WindowManager.Windows.Count; i++)
            {
                // Check if active control is hovered (GUI context)
                if (WindowManager.Windows[i]?.Controls != null)
                {
                    for (int j = 0; j < WindowManager.Windows[i].Controls.Count; j++)
                    {
                        if (GameState.CurMouseXGui >= WindowManager.Windows[i].X &&
                            GameState.CurMouseXGui <= WindowManager.Windows[i].Width + WindowManager.Windows[i].X &&
                            GameState.CurMouseYGui >= WindowManager.Windows[i].Y &&
                            GameState.CurMouseYGui <= WindowManager.Windows[i].Height + WindowManager.Windows[i].Y)
                        {
                            if (WindowManager.Windows[i].Controls[j].State != ControlState.Normal)
                            {
                                return;
                            }
                        }
                    }
                }
            }

            // In-game interactions for left click
            if (GameState.InGame == true)
            {
                if (GameState.MyEditorType == EditorType.Map)
                {
                    // Guard: do not edit map while mouse is over any GUI window/control
                    bool overGui = false;
                    foreach (var w in WindowManager.Windows.Values)
                    {
                        if (w is null || !w.Visible) continue;
                        if (GameState.CurMouseXGui >= w.X && GameState.CurMouseXGui <= w.X + w.Width &&
                            GameState.CurMouseYGui >= w.Y && GameState.CurMouseYGui <= w.Y + w.Height)
                        {
                            overGui = true;
                            break;
                        }
                    }
                    
                    if (!overGui)
                    {
                        Editors.MouseDown(GameState.CurXGame, GameState.CurYGame, false);
                    }
                }

                if (IsSearchCooldownElapsed())
                {
                    if (IsMouseButtonDown(MouseButton.Left))
                    {
                        Sender.SendPlayerSearch(GameState.CurXGame, GameState.CurYGame, 0);
                        _lastSearchTime = DateTime.Now;
                    }
                }

                // Right-click interactions
                if (IsMouseButtonDown(MouseButton.Right))
                {
                    if (GameState.PlayerData)
                    {
                        int slot = -1;
                        if (WindowManager.TryGetWindow("winHotbar", out var winHotbar))
                        {
                            slot = GameLogic.IsHotbar(winHotbar!.X, winHotbar!.Y);
                        }

                        if (slot >= 0L)
                        {
                            Sender.SendDeleteHotbar(slot);
                        }

                        if (GameState.VbKeyShift == true)
                        {
                            // Admin warp if Shift is held and the player has moderator access
                            if (GetPlayerAccess(GameState.MyIndex) >= (int) AccessLevel.Moderator)
                            {
                                Sender.SendAdminWarp(GameState.CurXGame, GameState.CurYGame);
                            }
                        }
                        else
                        {
                            // Handle right-click menu
                            HandleRightClickMenu();
                        }
                    }
                }
            }
        }

        private static void HandleRightClickMenu()
        {
            // Use game-space mouse position for world interactions (target/admin warp/player search)
            var mousePosGame = GetMousePosition("game");
            int mouseXGame = mousePosGame.Item1;
            int mouseYGame = mousePosGame.Item2;

            // Use gui-space mouse position for UI
            var mousePosGui = GetMousePosition("gui");
            int mouseXGui = mousePosGui.Item1;
            int mouseYGui = mousePosGui.Item2;

            for (int i = 0; i < Player.Instance.Count; i++)
            {
                if (IsPlaying(i) && GetPlayerMap(i) == GetPlayerMap(GameState.MyIndex))
                {
                    if (GetPlayerX(i) == GameState.CurXGame && GetPlayerY(i) == GameState.CurYGame)
                    {
                        // Show player menu at GUI mouse position (for UI popups)
                        GameLogic.ShowPlayerMenu(i, mouseXGui, mouseYGui);
                    }
                }
            }

            // Perform player search at the current cursor position (game-space)
            Sender.SendPlayerSearch(GameState.CurXGame, GameState.CurYGame, 1);
        }


        public static void TakeScreenshot()
        {
            try
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "Screenshots");
                if (!Path.Exists(dir))
                    Directory.CreateDirectory(dir);
                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _pendingScreenshotPath = Path.Combine(dir, $"screenshot_{ts}.png");
            }
            catch
            {
                _pendingScreenshotPath = null;
            }
        }

        private void TrySaveBackbufferScreenshot(string? path)
        {
            try
            {
                var gd = GraphicsDevice;
                var pp = gd.PresentationParameters;
                int w = pp.BackBufferWidth;
                int h = pp.BackBufferHeight;

                var data = new Color[w * h];
                gd.GetBackBufferData(data); // works with MSAA off

                using var tex = new Texture2D(gd, w, h, false, SurfaceFormat.Color);
                tex.SetData(data);

                if (string.IsNullOrEmpty(path)) return;
                using var fs = File.Create(path);
                tex.SaveAsPng(fs, w, h);

                _pendingScreenshotPath = "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Screenshot failed: {ex}");
            }
        }

        // Draw a filled rectangle with an optional outline
        public static void DrawRectangle(Vector2 position, Vector2 size, Color fillColor, Color outlineColor, float outlineThickness)
        {
            if (SpriteBatch == null || PixelTexture == null) return;

            // Draw the filled rectangle
            SpriteBatch.Draw(PixelTexture, new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y), fillColor);

            // Draw the outline if thickness > 0
            if (outlineThickness > 0f)
            {
                int t = (int)Math.Max(1, Math.Round(outlineThickness));

                var left = new Rectangle((int)position.X, (int)position.Y, t, (int)size.Y);
                var top = new Rectangle((int)position.X, (int)position.Y, (int)size.X, t);
                var right = new Rectangle((int)(position.X + size.X - t), (int)position.Y, t, (int)size.Y);
                var bottom = new Rectangle((int)position.X, (int)(position.Y + size.Y - t), (int)size.X, t);

                SpriteBatch.Draw(PixelTexture, left, outlineColor);
                SpriteBatch.Draw(PixelTexture, top, outlineColor);
                SpriteBatch.Draw(PixelTexture, right, outlineColor);
                SpriteBatch.Draw(PixelTexture, bottom, outlineColor);
            }
        }

        public static void DrawOutlineRectangle(int x, int y, int width, int height, Color color, float thickness)
        {
            if (SpriteBatch == null || PixelTexture == null) return; // safety
            if (width <= 0 || height <= 0) return;

            int t = (int)Math.Max(1, Math.Round(thickness));
            if (t > width) t = width;
            if (t > height) t = height;

            // Define four rectangles for the outline
            var left = new Rectangle(x, y, t, height);
            var top = new Rectangle(x, y, width, t);
            var right = new Rectangle(x + width - t, y, t, height);
            var bottom = new Rectangle(x, y + height - t, width, t);

            // Draw the outline using cached PixelTexture
            SpriteBatch.Draw(PixelTexture, left, color);
            SpriteBatch.Draw(PixelTexture, top, color);
            SpriteBatch.Draw(PixelTexture, right, color);
            SpriteBatch.Draw(PixelTexture, bottom, color);
        }

        public static Color QbColorToXnaColor(int qbColor)
        {
            switch (qbColor)
            {
                case (int) ColorName.Black:
                {
                    return Color.Black;
                }
                case (int) ColorName.Blue:
                {
                    return Color.Blue;
                }
                case (int) ColorName.Green:
                {
                    return Color.Green;
                }
                case (int) ColorName.Cyan:
                {
                    return Color.Cyan;
                }
                case (int) ColorName.Red:
                {
                    return Color.Red;
                }
                case (int) ColorName.Magenta:
                {
                    return Color.Magenta;
                }
                case (int) ColorName.Brown:
                {
                    return Color.Brown;
                }
                case (int) ColorName.Gray:
                {
                    return Color.LightGray;
                }
                case (int) ColorName.DarkGray:
                {
                    return Color.Gray;
                }
                case (int) ColorName.BrightBlue:
                {
                    return Color.LightBlue;
                }
                case (int) ColorName.BrightGreen:
                {
                    return Color.LightGreen;
                }
                case (int) ColorName.BrightCyan:
                {
                    return Color.LightCyan;
                }
                case (int) ColorName.BrightRed:
                {
                    return Color.LightCoral;
                }
                case (int) ColorName.Pink:
                {
                    return Color.Orchid;
                }
                case (int) ColorName.Yellow:
                {
                    return Color.Yellow;
                }
                case (int) ColorName.White:
                {
                    return Color.White;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(qbColor), "Invalid QbColor value.");
                }
            }
        }

        public static void DrawEmote(int x2, int y2, int sprite)
        {
            Rectangle rec;
            int x;
            int y;
            int anim;

            if (sprite < 1 | sprite > GameState.NumEmotes)
                return;

            if (GameState.ShowAnimLayers)
            {
                anim = 1;
            }
            else
            {
                anim = 0;
            }

            rec.Y = 0;
            rec.Height = Constants.TileSize;
            var emoteInfo = GetGfxInfo(Path.Combine(DataPath.Emotes, sprite.ToString()));
            if (emoteInfo == null) return;
            rec.X = (int)Math.Round(anim * (emoteInfo.Width / 2d));
            rec.Width = (int)Math.Round(emoteInfo.Width / 2d);
                                         
            x = GameLogic.ConvertMapX(x2);
            y = GameLogic.ConvertMapY(y2) - (Constants.TileSize + 16);

            string argPath = Path.Combine(DataPath.Emotes, sprite.ToString());
            RenderTexture(ref argPath, x, y, rec.X, rec.Y, rec.Width, rec.Height, rec.Width, rec.Height);
        }

        public static void DrawDirections(int x, int y)
        {
            Rectangle rec;
            int i;

            // render grid
            rec.Y = 24;
            rec.X = 0;
            rec.Width = 32;
            rec.Height = 32;

            string argPath = Path.Combine(DataPath.Misc, "Direction");
            RenderTexture(ref argPath, GameLogic.ConvertMapX(x * Constants.TileSize),
                GameLogic.ConvertMapY(y * Constants.TileSize),
                rec.X, rec.Y, rec.Width, rec.Height, rec.Width, rec.Height);

            // render dir blobs
            for (i = 0; i < 4; i++)
            {
                rec.X = i * 8;
                rec.Width = 8;

                // find out whether render blocked or not
                bool LocalIsDirBlocked()
                {
                    byte argdir = (byte) i;
                    var n = GameLogic.IsDirBlocked(ref Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].DirBlock, ref argdir);
                    return n;
                }

                if (!LocalIsDirBlocked())
                {
                    rec.Y = 8;
                }
                else
                {
                    rec.Y = 16;
                }

                rec.Height = 8;

                string argPath1 = Path.Combine(DataPath.Misc, "Direction");
                RenderTexture(ref argPath1, GameLogic.ConvertMapX(x * Constants.TileSize) + GameState.DirArrowX[i],
                    GameLogic.ConvertMapY(y * Constants.TileSize) + GameState.DirArrowY[i], rec.X, rec.Y, rec.Width,
                    rec.Height,
                    rec.Width, rec.Height);
            }
        }

        public static void DrawPaperdoll(int x, int y, int sprite, int anim, int spritetop, bool isMoving = false, bool isAttacking = false)
        {
            // Paperdoll rendering now mirrors DrawPlayer segmented logic (Idle|Run|Attack) so equipment layers
            // stay frame-synchronized with the base character during run/attack animations.
            if (sprite < 1 || sprite > GameState.NumPaperdolls) return;

            string gfxPath = Path.Combine(DataPath.Paperdolls, sprite.ToString());
            var info = GetGfxInfo(gfxPath);
            if (info == null || info.Width <= 0 || info.Height <= 0) return;
            // Determine directional rows using helper (configured -> 8 -> 4 -> 1 heuristic)
            int configuredDirs = Math.Max(1, SettingsManager.Instance.SpriteDirections);
            int directionRows = ComputeDirectionRows(info.Height, configuredDirs);
            bool looksDirectional = directionRows > 1;

            int frameHeight = info.Height / directionRows;
            if (frameHeight <= 0) frameHeight = info.Height; // safety

            // Segment frame counts (match player logic) but only meaningful if segmented sheet.
            int idleFrames = Math.Max(1, SettingsManager.Instance.IdleFrames);
            int runFrames = Math.Max(1, SettingsManager.Instance.RunFrames);
            int attackFrames = Math.Max(1, SettingsManager.Instance.AttackFrames);
            int expectedTotalColumns = idleFrames + runFrames + attackFrames;

            // Legacy: derive columns by square-ish assumption
            int autoColsBySquare = frameHeight > 0 ? info.Width / frameHeight : 1;
            if (autoColsBySquare <= 0) autoColsBySquare = 1;
            // Can we treat as segmented? Require width divisibility AND at least expectedTotalColumns frames by square heuristic.
            bool canSegment = expectedTotalColumns > 0 && info.Width % expectedTotalColumns == 0 && autoColsBySquare >= expectedTotalColumns;
            // Force non-segment if sheet effectively only has 1 column (common for static paperdolls)
            if (autoColsBySquare <= 1) canSegment = false;
            int frameColumnsForWidth = canSegment ? expectedTotalColumns : autoColsBySquare;
            if (frameColumnsForWidth <= 0) frameColumnsForWidth = 1;
            int frameWidth = Math.Max(1, info.Width / frameColumnsForWidth);

            // Offsets for segments
            int idleOffset = 0, runOffset = 0, attackOffset = 0;
            if (canSegment)
            {
                string orderCsv = SettingsManager.Instance.SpriteSegmentOrder ?? "idle,run,attack";
                var tokens = orderCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 3) tokens = new[] { "idle", "run", "attack" };
                for (int i = 0; i < tokens.Length; i++) tokens[i] = tokens[i].Trim().ToLowerInvariant();
                if (!(tokens.Contains("idle") && tokens.Contains("run") && tokens.Contains("attack")))
                    tokens = new[] { "idle", "run", "attack" };
                int runningOffset = 0;
                for (int i = 0; i < tokens.Length; i++)
                {
                    string t = tokens[i];
                    if (t == "idle") idleOffset = runningOffset;
                    else if (t == "run") runOffset = runningOffset;
                    else if (t == "attack") attackOffset = runningOffset;
                    if (t == "idle") runningOffset += idleFrames;
                    else if (t == "run") runningOffset += runFrames;
                    else if (t == "attack") runningOffset += attackFrames;
                }
            }

            // Choose segment (paperdolls track with base character state via isMoving/isAttacking flags)
            int segmentOffset = idleOffset;
            int segmentFrames = idleFrames;
            if (canSegment)
            {
                if (isAttacking) { segmentOffset = attackOffset; segmentFrames = attackFrames; }
                else if (isMoving) { segmentOffset = runOffset; segmentFrames = runFrames; }
            }

            int frameInSegment = anim % Math.Max(1, segmentFrames);
            int frameColumn = Math.Min(frameColumnsForWidth - 1, segmentOffset + frameInSegment);

            // Row: if no directional rows, clamp to 0 ignoring spritetop
            if (!looksDirectional) spritetop = 0;
            if (spritetop < 0 || spritetop >= directionRows) spritetop = 0;

            // Match player rectangle math exactly (double precision + Math.Round)
            double frameWidthD = info.Width / (double)frameColumnsForWidth;
            double frameHeightD2 = frameHeight; // already derived
            var rec = new Rectangle(
                (int)Math.Round(frameColumn * frameWidthD),
                (int)Math.Round(spritetop * frameHeightD2),
                (int)Math.Round(frameWidthD),
                (int)Math.Round(frameHeightD2));

            // Convert to screen coordinates exactly once here for the equipment layer.
            int sx = GameLogic.ConvertMapX(x);
            int sy = GameLogic.ConvertMapY(y);
            RenderTexture(ref gfxPath, sx, sy, rec.X, rec.Y, rec.Width, rec.Height, rec.Width, rec.Height);
        }


        public static void DrawCharacterSprite(int sprite, int x2, int y2, Rectangle sRect)
        {
            int x;
            int y;

            if (sprite < 1 | sprite > GameState.NumCharacters)
                return;

            x = GameLogic.ConvertMapX(x2);
            y = GameLogic.ConvertMapY(y2);

            string argPath = Path.Combine(DataPath.Characters, sprite.ToString());
            RenderTexture(ref argPath, x, y, sRect.X, sRect.Y, sRect.Width, sRect.Height, sRect.Width, sRect.Height);
        }

        public static void DrawBars()
        {
            long left;
            long top;
            long width;
            long height;
            long tmpX;
            long tmpY;
            var barWidth = default(long);
            long i;
            long npc;

            // dynamic bar calculations (defensively handle missing texture)
            var barsInfo = GetGfxInfo(Path.Combine(DataPath.Misc, "Bars"));
            if (barsInfo == null)
                return;

            // dynamic bar calculations
            width = barsInfo.Width;
            height = (long) Math.Round(barsInfo.Height / 4d);

            if (MapNpc.Instance == null)
                return;

            // render Npc health bars
            for (i = 0L; i < Variables.MaxMapNpcs; i++)
            {
                npc = (long) MapNpc.Instance[(int) i].Num;
                // exists?
                if (npc >= 0L && npc < Npc.Instance.Count)
                {
                    // alive?
                    if (MapNpc.Instance[(int) i].Vital[(int) Core.Globals.Vital.Health] > 0 &
                        MapNpc.Instance[(int) i].Vital[(int) Core.Globals.Vital.Health] < Npc.Instance[(int) npc].Hp)
                    {
                        // lock to Npc
                        tmpX = (long) Math.Round(MapNpc.Instance[(int) i].X + 16 - width / 2d);
                        tmpY = MapNpc.Instance[(int) i].Y + 35;

                        // calculate the width to fill
                        if (width > 0)
                            GameState.BarWidthNpcHPMax[(int) i] = (int) Math.Round(
                                MapNpc.Instance[(int) i].Vital[(int) Core.Globals.Vital.Health] / (double) width /
                                (Npc.Instance[(int) npc].Hp / (double) width) * width);

                        // draw bar background
                        top = height * 3L; // HP bar background
                        left = 0L;
                        string argPath = Path.Combine(DataPath.Misc, "Bars");
                        RenderTexture(ref argPath, GameLogic.ConvertMapX((int) tmpX), GameLogic.ConvertMapY((int) tmpY),
                            (int) left, (int) top, (int) width, (int) height, (int) width, (int) height);

                        // draw the bar proper
                        top = 0L; // HP bar
                        left = 0L;
                        string argPath1 = Path.Combine(DataPath.Misc, "Bars");
                        RenderTexture(ref argPath1, GameLogic.ConvertMapX((int) tmpX), GameLogic.ConvertMapY((int) tmpY),
                            (int) left, (int) top, (int) GameState.BarWidthNpcHP[(int) i], (int) height,
                            (int) GameState.BarWidthNpcHP[(int) i], (int) height);
                    }
                }
            }

            for (i = 0L; i < PlayerBase.Instance.Count; i++)
            {
                if (IsPlaying((int) i))
                {
                    if (GetPlayerMap((int) i) == GetPlayerMap((int) i))
                    {
                        if (GetPlayerVital((int) i, Core.Globals.Vital.Health) > 0 &
                            GetPlayerVital((int) i, Core.Globals.Vital.Health) < GetPlayerMaxVital((int) i, Core.Globals.Vital.Health))
                        {
                            // lock to Player
                            tmpX = (long) Math.Round(GetPlayerRawX((int) i) +
                                16 - width / 2d);
                            tmpY = GetPlayerRawY((int) i) + 35;

                            // calculate the width to fill
                            if (width > 0L)
                                GameState.BarWidthPlayerHPMax[(int) i] = (int) Math.Round(
                                    GetPlayerVital((int) i, Core.Globals.Vital.Health) / (double) width /
                                    (GetPlayerMaxVital((int) i, Core.Globals.Vital.Health) / (double) width) * width);

                            // draw bar background
                            top = height * 3L; // HP bar background
                            left = 0L;
                            string argPath2 = Path.Combine(DataPath.Misc, "Bars");
                            RenderTexture(ref argPath2, GameLogic.ConvertMapX((int) tmpX), GameLogic.ConvertMapY((int) tmpY),
                                (int) left, (int) top, (int) width, (int) height, (int) width, (int) height);

                            // draw the bar proper
                            top = 0L; // HP bar
                            left = 0L;
                            string argPath3 = Path.Combine(DataPath.Misc, "Bars");
                            RenderTexture(ref argPath3, GameLogic.ConvertMapX((int) tmpX), GameLogic.ConvertMapY((int) tmpY),
                                (int) left, (int) top, (int) GameState.BarWidthPlayerHP[(int) i], (int) height,
                                (int) GameState.BarWidthPlayerHP[(int) i], (int) height);
                        }

                        if (GetPlayerVital((int) i, Core.Globals.Vital.Stamina) > 0 &
                            GetPlayerVital((int) i, Core.Globals.Vital.Stamina) < GetPlayerMaxVital((int) i, Core.Globals.Vital.Stamina))
                        {
                            // lock to Player
                            tmpX = (long)Math.Round(GetPlayerRawX((int)i) +
                                16 - width / 2d);
                            tmpY = GetPlayerRawY((int)i) + 35 + height;

                            // calculate the width to fill
                            if (width > 0)
                                GameState.BarWidthPlayerMPMax[(int) i] = (int) Math.Round(
                                    GetPlayerVital((int) i, Core.Globals.Vital.Mana) / (double) width /
                                    (GetPlayerMaxVital((int) i, Core.Globals.Vital.Mana) / (double) width) * width);

                            // draw bar background
                            top = height * 3L; // SP bar background
                            left = 0L;
                            string argPath4 = Path.Combine(DataPath.Misc, "Bars");
                            RenderTexture(ref argPath4, GameLogic.ConvertMapX((int) tmpX), GameLogic.ConvertMapY((int) tmpY),
                                (int) left, (int) top, (int) width, (int) height, (int) width, (int) height);

                            // draw the bar proper
                            top = height * 1L; // MP bar
                            left = 0L;
                            string argPath5 = Path.Combine(DataPath.Misc, "Bars");
                            RenderTexture(ref argPath5, GameLogic.ConvertMapX((int) tmpX), GameLogic.ConvertMapY((int) tmpY),
                                (int) left, (int) top, (int) GameState.BarWidthPlayerMP[(int) i], (int) height,
                                (int) GameState.BarWidthPlayerMP[(int) i], (int) height);
                        }

                        if (GameState.SkillBuffer >= 0)
                        {
                            if (Player.Instance[(int) i].Skill[GameState.SkillBuffer].Num >= 0)
                            {
                                if (Skill.Instance.Count > Player.Instance[(int) i].Skill[GameState.SkillBuffer].Num)
                                {
                                    if (Skill.Instance[Player.Instance[(int) i].Skill[GameState.SkillBuffer].Num]
                                            .CastTime > 0)
                                    {
                                        // lock to player
                                        tmpX = (long)Math.Round(GetPlayerRawX((int)i) + 16 - width / 2d);

                                        tmpY = GetPlayerRawY((int)i) + 35 + height;

                                        // calculate the width to fill
                                        if (width > 0L)
                                            barWidth = (long) Math.Round((General.GetTickCount() - GameState.SkillBufferTimer) /
                                                (double) (Client.Skill.Instance[(int) Player.Instance[(int) i].Skill[GameState.SkillBuffer].Num]
                                                    .CastTime * 1000) * width);

                                        // draw bar background
                                        top = height * 3L; // cooldown bar background
                                        left = 0L;
                                        string argPath6 = Path.Combine(DataPath.Misc, "Bars");
                                        RenderTexture(ref argPath6, GameLogic.ConvertMapX((int) tmpX),
                                            GameLogic.ConvertMapY((int) tmpY), (int) left, (int) top, (int) width, (int) height,
                                            (int) width, (int) height);

                                        // draw the bar proper
                                        top = height * 2L; // cooldown bar
                                        left = 0L;
                                        string argPath7 = Path.Combine(DataPath.Misc, "Bars");
                                        RenderTexture(ref argPath7, GameLogic.ConvertMapX((int) tmpX),
                                            GameLogic.ConvertMapY((int) tmpY), (int) left, (int) top, (int) barWidth, (int) height,
                                            (int) barWidth, (int) height);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void DrawEyeDropper()
        {
            if (SpriteBatch == null) return;

            // Define rectangle parameters.
            int x = GameState.CurXGame;
            int y = GameState.CurYGame;
            int w = 1;
            int h = 1;

            // Show selection outline only while drag-selecting.
            if (GameState.EyeDropperSelecting)
            {
                int minX = Math.Min(GameState.EyeDropperSelStart.X, GameState.EyeDropperSelEnd.X);
                int minY = Math.Min(GameState.EyeDropperSelStart.Y, GameState.EyeDropperSelEnd.Y);
                int maxX = Math.Max(GameState.EyeDropperSelStart.X, GameState.EyeDropperSelEnd.X);
                int maxY = Math.Max(GameState.EyeDropperSelStart.Y, GameState.EyeDropperSelEnd.Y);
                x = minX;
                y = minY;
                w = (maxX - minX) + 1;
                h = (maxY - minY) + 1;
            }

            var position = new Vector2(
                GameLogic.ConvertMapX(x * Constants.TileSize),
                GameLogic.ConvertMapY(y * Constants.TileSize));
            var size = new Vector2(Constants.TileSize * w, Constants.TileSize * h);
            var fillColor = Color.Transparent; // No fill
            var outlineColor = Color.Red; // Red outline
            int outlineThickness = 1; // Thickness of outline

            // Draw the rectangle with an outline.
            DrawRectangle(position, size, fillColor, outlineColor, outlineThickness);
        }

        public static void DrawGrid()
        {
            // Draw tile grid outlines using the existing batch-safe helpers
            int tileW = Constants.TileSize;
            int tileH = Constants.TileSize;
            for (int x = (int)GameState.TileView.Left; x <= (int)GameState.TileView.Right; x++)
            {
                for (int y = (int)GameState.TileView.Top; y <= (int)GameState.TileView.Bottom; y++)
                {
                    if (!GameLogic.IsValidMapPoint(x, y)) continue;
                    int px = GameLogic.ConvertMapX(x * tileW);
                    int py = GameLogic.ConvertMapY(y * tileH);
                    GameClient.DrawOutlineRectangle(px, py, tileW, tileH, Color.Red, 1f);
                }
            }
        }

        // Visual hover effect reflects the current selection size in the Map editor
        private static void DrawMapEditorHoverTileHighlight()
        {
            if (SpriteBatch == null) return;

            int map = GetPlayerMap(GameState.MyIndex);
            int x = GameState.CurXGame;
            int y = GameState.CurYGame;

            if (map < 0 || map >= Client.Map.Instance.Count) return;
            if (x < 0 || y < 0) return;

            var mi = Client.Map.Instance[map];
            if (x >= mi.MaxX || y >= mi.MaxY) return;

            // Determine selection dimensions: prefer captured stamp, otherwise tileset selection/autotile size.
            int selW = 1;
            int selH = 1;

            if (GameState.EditorStampActive)
            {
                selW = Math.Max(1, GameState.EditorStampWidth);
                selH = Math.Max(1, GameState.EditorStampHeight);
            }
            else
            {
                selW = Math.Max(1, GameState.EditorTileWidth);
                selH = Math.Max(1, GameState.EditorTileHeight);
            }

            // Clamp selection to map bounds from hover origin.
            selW = Math.Min(selW, mi.MaxX - x);
            selH = Math.Min(selH, mi.MaxY - y);
            if (selW <= 0 || selH <= 0) return;

            int px = GameLogic.ConvertMapX(x * Constants.TileSize);
            int py = GameLogic.ConvertMapY(y * Constants.TileSize);

            var pos = new Vector2(px, py);
            var size = new Vector2(Constants.TileSize * selW, Constants.TileSize * selH);

            // Semi-transparent fill with a subtle outline covering the selection area
            var fill = new Color(255, 255, 255, 40);
            var outline = Color.Yellow;
            DrawRectangle(pos, size, fill, outline, 1f);

            // Also render a ghost preview of the selected tiles at the hover location.
            // Stamp preview supports mixed tilesets; contiguous selection preview uses current tileset.
            float previewAlpha = 0.6f;

            if (GameState.EditorStampActive && GameState.EditorStampTileset is not null && GameState.EditorStampX is not null && GameState.EditorStampY is not null)
            {
                for (int dy = 0; dy < selH; dy++)
                {
                    for (int dx = 0; dx < selW; dx++)
                    {
                        int tx = GameState.EditorStampTileset[dx, dy];
                        int sx = GameState.EditorStampX[dx, dy] * Constants.TileSize;
                        int sy = GameState.EditorStampY[dx, dy] * Constants.TileSize;

                        if (tx <= 0 || tx > GameState.NumTileSets) continue;

                        string path = Path.Combine(DataPath.Tilesets, tx.ToString());
                        int destX = px + dx * Constants.TileSize;
                        int destY = py + dy * Constants.TileSize;
                        RenderTexture(ref path, destX, destY, sx, sy, Constants.TileSize, Constants.TileSize, Constants.TileSize, Constants.TileSize, previewAlpha);
                    }
                }
            }
            else if (GameState.CurAutotileType == 0 && GameState.CurTileset > 0 && GameState.CurTileset <= GameState.NumTileSets)
            {
                string path = Path.Combine(DataPath.Tilesets, GameState.CurTileset.ToString());
                for (int dy = 0; dy < selH; dy++)
                {
                    for (int dx = 0; dx < selW; dx++)
                    {
                        int sx = (GameState.EditorTileX + dx) * Constants.TileSize;
                        int sy = (GameState.EditorTileY + dy) * Constants.TileSize;
                        int destX = px + dx * Constants.TileSize;
                        int destY = py + dy * Constants.TileSize;
                        RenderTexture(ref path, destX, destY, sx, sy, Constants.TileSize, Constants.TileSize, Constants.TileSize, Constants.TileSize, previewAlpha);
                    }
                }
            }
        }

        public static void DrawTarget(int x2, int y2)
        {
            Rectangle rec;
            int x;
            int y;
            int width;
            int height;

            rec.Y = 0;
            var targetInfo = GetGfxInfo(Path.Combine(DataPath.Misc, "Target"));
            if (targetInfo == null) return;
            rec.Height = targetInfo.Height;
            rec.X = 0;
            rec.Width = (int)Math.Round(targetInfo.Width / 2d);
            x = GameLogic.ConvertMapX(x2 + 4);
            y = GameLogic.ConvertMapY(y2 - 32);
            width = rec.Right - rec.Left;
            height = rec.Bottom - rec.Top;

            string argPath = Path.Combine(DataPath.Misc, "Target");
            RenderTexture(ref argPath, x, y, rec.X, rec.Y, rec.Width, rec.Height, rec.Width, rec.Height);
        }

        public static Color ToMonoGameColor(System.Drawing.Color drawingColor)
        {
            return new Color(drawingColor.R, drawingColor.G, drawingColor.B, drawingColor.A);
        }

        public static void DrawHover(int x2, int y2)
        {
            Rectangle rec;
            int x;
            int y;
            int width;
            int height;

            rec.Y = 0;
            var targetInfo2 = GetGfxInfo(Path.Combine(DataPath.Misc, "Target"));
            if (targetInfo2 == null) return;
            rec.Height = targetInfo2.Height;
            rec.X = (int)Math.Round(targetInfo2.Width / 2d);
            rec.Width = (int)Math.Round((double)targetInfo2.Width);

            x = GameLogic.ConvertMapX(x2 + 4);
            y = GameLogic.ConvertMapY(y2 - 32);
            width = rec.Right - rec.Left;
            height = rec.Bottom - rec.Top;

            string argPath = Path.Combine(DataPath.Misc, "Target");
            RenderTexture(ref argPath, x, y, rec.X, rec.Y, rec.Width, rec.Height, rec.Width, rec.Height);
        }

        public static void RenderCharacterGraphic(Type.Event eventData, int x, int y)
        {
            // Get the graphic index from the event's first page
            int gfxIndex = eventData.Pages[0].Graphic;
            if (gfxIndex <= 0 || gfxIndex > GameState.NumCharacters)
                return;

            var gfxInfo = GetGfxInfo(Path.Combine(DataPath.Characters, gfxIndex.ToString()));
            if (gfxInfo == null)
                return;

            // Direction rows dynamic (configured sprite directions with fallback heuristics)
            int directionRows = ComputeDirectionRows(gfxInfo.Height, Math.Max(1, SettingsManager.Instance.SpriteDirections));

            // Frame counts from settings (segment lengths)
            int idleFrames = Math.Max(1, SettingsManager.Instance.IdleFrames);
            int runFrames = Math.Max(1, SettingsManager.Instance.RunFrames);
            int attackFrames = Math.Max(1, SettingsManager.Instance.AttackFrames);
            int expectedTotalColumns = idleFrames + runFrames + attackFrames;

            // Height of one directional row
            int frameRowHeight = gfxInfo.Height / directionRows;
            if (frameRowHeight <= 0)
                frameRowHeight = gfxInfo.Height; // safety fallback

            // Legacy heuristic: square frames => columns inferred by height
            int autoColsBySquare = frameRowHeight > 0 ? gfxInfo.Width / frameRowHeight : 0;
            if (autoColsBySquare <= 0)
                autoColsBySquare = 1;

            bool widthDivisible = expectedTotalColumns > 0 && gfxInfo.Width % expectedTotalColumns == 0;
            bool canSegment = widthDivisible; // relaxed rule (match Player/NPC logic)
            int frameColumnsForWidth = canSegment ? expectedTotalColumns : autoColsBySquare;
            double frameWidthD = gfxInfo.Width / (double)frameColumnsForWidth;
            double frameHeightD = frameRowHeight;

            // Parse ordering (e.g. "idle,run,attack")
            string orderCsv = SettingsManager.Instance.SpriteSegmentOrder ?? "idle,run,attack";
            var tokens = orderCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 3)
                tokens = new[] { "idle", "run", "attack" };
            for (int i = 0; i < tokens.Length; i++)
                tokens[i] = tokens[i].Trim().ToLowerInvariant();
            if (!(tokens.Contains("idle") && tokens.Contains("run") && tokens.Contains("attack")))
                tokens = new[] { "idle", "run", "attack" };

            int runningOffset = 0;
            int idleOffset = 0, runOffset = 0, attackOffset = 0; // run/attack retained for symmetry/debug
            for (int i = 0; i < tokens.Length; i++)
            {
                string t = tokens[i];
                if (t == "idle") idleOffset = runningOffset;
                else if (t == "run") runOffset = runningOffset;
                else if (t == "attack") attackOffset = runningOffset;

                if (t == "idle") runningOffset += idleFrames;
                else if (t == "run") runningOffset += runFrames;
                else if (t == "attack") runningOffset += attackFrames;
            }

            // Determine animation frame (idle-only animation for events)
            int animFrame;
            // Derive a pseudo steps counter from global tick (150ms per frame)
            const int IdleFrameDurationMs = 150;
            long tick = General.GetTickCount();
            int pseudoSteps = (int)(tick / IdleFrameDurationMs);
            if (canSegment)
            {
                animFrame = idleOffset + (pseudoSteps % idleFrames);
            }
            else
            {
                // Legacy: animate across all columns. If user explicitly set GraphicX>0, honor it as a base offset.
                int baseCol = Math.Max(0, Math.Min(frameColumnsForWidth - 1, eventData.Pages[0].GraphicX));
                animFrame = (baseCol + pseudoSteps) % frameColumnsForWidth;
            }

            // Row selection: event editor stores desired facing row in GraphicY.
            int row = Math.Max(0, Math.Min(directionRows - 1, eventData.Pages[0].GraphicY));

            // Build rectangle
            var sourceRect = new Rectangle(
                (int)Math.Round(animFrame * frameWidthD),
                (int)Math.Round(row * frameHeightD),
                (int)Math.Round(frameWidthD),
                (int)Math.Round(frameHeightD));

            // Anchor adjustment for tall sprites (>32px)
            int drawY = y;
            if (frameRowHeight > 32)
                drawY = y - (frameRowHeight - 32); // lift so feet align to tile

            int drawX = x - Constants.TileSize - 8;

            string argPath = Path.Combine(DataPath.Characters, gfxIndex.ToString());
            RenderTexture(ref argPath, drawX, drawY, sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height, sourceRect.Width, sourceRect.Height);
        }

        public static void RenderTilesetGraphic(Type.Event eventData, int x, int y)
        {
            int gfxIndex = eventData.Pages[0].Graphic;

            if (gfxIndex > 0 && gfxIndex <= GameState.NumTileSets)
            {
                // Define source rectangle from tileset graphics
                int width = Math.Max(1, eventData.Pages[0].GraphicX2) * Constants.TileSize;
                int height = Math.Max(1, eventData.Pages[0].GraphicY2) * Constants.TileSize;
                var srcRect = new Rectangle(eventData.Pages[0].GraphicX * Constants.TileSize,
                    eventData.Pages[0].GraphicY * Constants.TileSize, width, height);

                // Draw at the tile's top-left in screen space for editor consistency
                int destX = x;
                int destY = y;

                string argPath = Path.Combine(DataPath.Tilesets, gfxIndex.ToString());
                RenderTexture(ref argPath, destX, destY, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    srcRect.Width, srcRect.Height);
            }
            else
            {
                // Draw fallback outline if the tileset graphic is invalid
                DrawOutlineRectangle(x, y, Constants.TileSize, Constants.TileSize, Color.Blue, 0.6f);
            }
        }

        public static void Render_Game()
        {
            int x;
            int y;
            int i;

            if (GameState.GettingMap || !GameState.InGame || !GameState.PlayerData)
                return;

            GameLogic.UpdateCamera();

            // Auto-cancel target if player is off the current camera viewport (native world rect)
            CancelTargetIfOffCamera();

            var map = GetPlayerMap(GameState.MyIndex);
            if (map < 0 || map >= Client.Map.Instance.Count)
                return;

            if (GameState.NumPanoramas > 0 & Map.Instance[map].Panorama > 0)
            {
                Map.DrawPanorama(Map.Instance[map].Panorama);
            }

            if (GameState.NumParallax > 0 & Map.Instance[map].Parallax > 0)
            {
                Map.DrawParallax(Map.Instance[map].Parallax);
            }

            // Draw lower tiles
            if (GameState.NumTileSets > 0)
            {
                var count = (int) Math.Round(GameState.TileView.Right + 1d);
                for (x = (int) Math.Round(GameState.TileView.Left - 1d); x < count; x++)
                {
                    var count2 = (int) Math.Round(GameState.TileView.Bottom + 1d);
                    for (y = (int) Math.Round(GameState.TileView.Top - 1d); y < count2; y++)
                    {
                        if (GameLogic.IsValidMapPoint(x, y))
                        {
                            Map.DrawMapGroundTile(x, y);
                        }
                    }
                }
            }

            // events
            if (GameState.MyEditorType != EditorType.Map)
            {
                if (GameState.CurrentEvents > 0 & GameState.CurrentEvents <= Client.Map.Instance[map].EventCount)
                {
                    var count2 = Information.UBound(Data.MapEvents);
                    for (i = 0; i <= count2; i++)
                    {
                        if (Data.MapEvents?[i].Position == 0)
                        {
                            Event.OnDraw(i);
                        }
                    }
                }
            }

            // blood
            for (i = 0; i < Data.Blood.Length; i++)
                Blood.OnDraw(i);

            // Draw out the items
            if (GameState.NumItems > 0)
            {
                for (i = 0; i < Variables.MaxMapItems; i++)
                {
                    MapItem.OnDraw(i);
                }
            }

            // draw animations
            if (GameState.NumAnimations > 0)
            {
                for (i = 0; i < MapAnimation.Instance?.Length; i++)
                {
                    if (MapAnimation.Instance?[i].Used?[0] == true)
                    {
                        MapAnimation.OnDraw(i, 0);
                    }
                }
            }

            // Y-based render. Renders Players, Npcs and Resources based on Y-axis.
            var count3 = (int) Client.Map.Instance[map].MaxY;
            for (y = 0; y < count3; y++)
            {
                if (GameState.NumCharacters > 0)
                {
                    // Npcs
                    for (i = 0; i < Variables.MaxMapNpcs; i++)
                    {
                        if (Math.Floor((decimal) MapNpc.Instance[i].Y / Constants.TileSize) == y)
                        {
                            MapNpc.OnDraw(i);
                        }
                    }

                    // Events (Same as Player): draw in the Y-pass so they sort with the world.
                    // Draw before players so events on the same row don't always appear above the player.
                    if (GameState.MyEditorType != EditorType.Map)
                    {
                        if (GameState.CurrentEvents > 0 & GameState.CurrentEvents <= Client.Map.Instance[map].EventCount)
                        {
                            var count4 = Information.UBound(Data.MapEvents);
                            for (i = 0; i <= count4; i++)
                            {
                                if (Data.MapEvents?[i].Position == 1)
                                {
                                    if (Math.Floor((decimal) Data.MapEvents[i].Y / Constants.TileSize) == y)
                                    {
                                        Event.OnDraw(i);
                                    }
                                }
                            }
                        }
                    }

                    // Players
                    for (i = 0; i < PlayerBase.Instance.Count; i++)
                    {
                        if (IsPlaying(i) & GetPlayerMap(i) == map)
                        {
                            if (GetPlayerY(i) == y)
                            {
                                Player.OnDraw(i);
                            }
                        }
                    }

                    // Draw the target icon
                    if (GameState.MyTarget >= 0)
                    {
                        switch (GameState.MyTargetType)
                        {
                            case (int) TargetType.Player:
                                if (IsPlaying(GameState.MyTarget))
                                {
                                    if (Player.Instance[GameState.MyTarget].Map ==
                                        Player.Instance[GameState.MyIndex].Map)
                                    {
                                        if (Player.Instance[GameState.MyTarget].Sprite > 0)
                                        {
                                            // Draw the target icon for the player
                                            DrawTarget(
                                                Player.Instance[GameState.MyTarget].X - 16,
                                                Player.Instance[GameState.MyTarget].Y);
                                        }
                                    }
                                }

                                break;

                            case (int) TargetType.Npc:
                                DrawTarget(
                                    MapNpc.Instance[GameState.MyTarget].X - 16,
                                    MapNpc.Instance[GameState.MyTarget].Y);
                                break;
                        }
                    }

                    for (i = 0; i < Player.Instance.Count; i++)
                    {
                        if (IsPlaying(i))
                        {
                            if (Player.Instance[i].Map == Player.Instance[GameState.MyIndex].Map)
                            {
                                if (Player.Instance[i].Sprite == 0)
                                    continue;

                                if (GameState.CurXGame == Player.Instance[i].X & GameState.CurYGame == Player.Instance[i].Y)
                                {
                                    if (GameState.MyTargetType == (int) TargetType.Player & GameState.MyTarget == i)
                                    {
                                    }
                                    else
                                    {
                                        DrawHover(Player.Instance[i].X * 32 - 16,
                                            Player.Instance[i].Y * 32 + Player.Instance[i].Y);
                                    }
                                }
                            }
                        }
                    }
                }

                // Resources
                if (GameState.NumResources > 0)
                {
                    if (GameState.ResourcesInit)
                    {
                        if (GameState.ResourceIndex > 0)
                        {
                            var count5 = GameState.ResourceIndex;
                            for (i = 0; i < count5; i++)                               
                                if (MapResource.Instance[i].Y == y)
                                {
                                    MapResource.OnDraw(i);
                                }
                            }
                        }
                                   
                }
            }

            // animations
            if (GameState.NumAnimations > 0)
            {
                for (i = 0; i < MapAnimation.Instance?.Length; i++)
                {
                    var used = MapAnimation.Instance?[i].Used;
                    if (used != null && used.Length > 1 && used[1])
                    {
                        MapAnimation.OnDraw(i, 1);
                    }
                }
            }

            if (GameState.NumProjectiles > 0)
            {
                if (map >= 0 && map < Core.Globals.Data.MapProjectile.GetLength(0))
                {
                    for (i = 0; i < Variables.MaxProjectiles; i++)
                    {
                        if (Core.Globals.Data.MapProjectile[map, i].Index >= 0)
                        {
                            MapProjectile.OnDraw(i);
                        }
                    }
                }
            }

            if (Data.MapEvents != null && GameState.CurrentEvents > 0 & GameState.CurrentEvents <= Client.Map.Instance[map].EventCount)
            {
                var count6 = GameState.CurrentEvents;
                for (i = 0; i < count6; i++)
                {
                    if (i < Data.MapEvents.Length && Data.MapEvents[i].Position == 2)
                    {
                        Event.OnDraw(i);
                    }
                }
            }

            if (GameState.NumTileSets > 0)
            {
                var count7 = (int) Math.Round(GameState.TileView.Right + 1d);
                for (x = (int) Math.Round(GameState.TileView.Left - 1d); x < count7; x++)
                {
                    var count8 = (int) Math.Round(GameState.TileView.Bottom + 1d);
                    for (y = (int) Math.Round(GameState.TileView.Top - 1d); y < count8; y++)
                    {
                        if (GameLogic.IsValidMapPoint(x, y))
                        {
                            Map.DrawMapRoofTile(x, y);
                        }
                    }
                }
            }

            Weather.OnDraw();
            Map.DrawMapTint();

            // Draw tile grid when enabled in the Map editor
            if (GameState.MapGrid && GameState.MyEditorType == EditorType.Map)
            {
                DrawGrid();
            }

            // Draw eyedropper selection outline only while actively selecting.
            if (GameState.MyEditorType == EditorType.Map && GameState.EyeDropperSelecting)
            {
                DrawEyeDropper();
            }
            // Hover tile highlight in Map editor
            if (GameState.MyEditorType == EditorType.Map)
            {
                DrawMapEditorHoverTileHighlight();
            }

            for (i = 0; i < Player.Instance.Count; i++)
            {
                if (IsPlaying(i) & GetPlayerMap(i) == map)
                {
                    Player.OnDrawName(i);
                }
            }

            if (GameState.MyEditorType != EditorType.Map)
            {
                if (GameState.CurrentEvents > 0 && Data.MapEvents != null)
                {
                    // CurrentEvents can temporarily exceed the local array length while packets are in-flight.
                    var count9 = Math.Min(GameState.CurrentEvents, Data.MapEvents.Length);
                    if (count9 > 0 && Client.Map.Instance[map].EventCount >= count9)
                    {
                        for (i = 0; i < count9; i++)
                        {
                            if (Data.MapEvents[i].Visible)
                            {
                                if (Data.MapEvents[i].ShowName == 1)
                                {
                                    Event.OnDrawName(i);
                                }
                            }
                        }
                    }
                }
            }

            for (i = 0; i < Variables.MaxMapNpcs; i++)
            {
                MapNpc.OnDrawName(i);
            }

            Map.DrawFog();
            Map.DrawPicture();

            for (i = 0; i < byte.MaxValue; i++)
                TextRenderer.DrawActionMessage(i);

            if (GameState.MyEditorType == EditorType.Map)
            {
                UpdateDirBlock();
                UpdateMapAttributes();
            }

            for (i = 0; i < byte.MaxValue; i++)
            {
                if (Data.ChatBubble[i].Active)
                {
                    ChatBubble.OnDraw(i);
                }
            }

            if (GameState.Bfps)
            {
                string fps = "FPS: " + GetFps();
                TextRenderer.Render(fps, (int) Math.Round(GameState.Camera.Left - 24d),
                    (int) Math.Round(GameState.Camera.Top + 60d), Color.Yellow, Color.Black);
            }

            // draw cursor, player X and Y locations
            if (GameState.BLoc)
            {
                string cur = "Cur X: " + GameState.CurXGame + " Y: " + GameState.CurYGame;
                string loc = "Loc X: " + GetPlayerX(GameState.MyIndex) + " Y: " + GetPlayerY(GameState.MyIndex);
                string pos = " (Map #" + map + ")";
                string curMouse = "Mouse X: " + (int)GameState.CurMouseXGame + " Y: " + (int)GameState.CurMouseYGame;

                TextRenderer.Render(cur, (int)GameState.CurMouseXGame, (int)Math.Round(GameState.CurMouseYGame + 15f),
                    Color.Yellow, Color.Black);

                TextRenderer.Render(curMouse, (int)GameState.CurMouseXGame, (int)Math.Round(GameState.CurMouseYGame + 30f),
                    Color.Yellow, Color.Black);

                TextRenderer.Render(loc, (int)GameState.CurMouseXGame, (int)Math.Round(GameState.CurMouseYGame + 45f),
                    Color.Yellow, Color.Black);

                TextRenderer.Render(pos, (int)GameState.CurMouseXGame, (int)Math.Round(GameState.CurMouseYGame + 60f),
                    Color.Yellow, Color.Black);
            }
            
            if (GameState.MyEditorType == EditorType.Map)
            {
                if (GameState.MapEditorTab == (int)MapEditorTab.Events)
                {
                    Event.OnDraw();
                }
            }

            DrawBars();
            Map.DrawMapFade();
        }

        // Cancels the current target if the distance between PLAYER and TARGET exceeds the visible camera view.
        private static void CancelTargetIfOffCamera()
        {
            // Only handle Player and NPC targets
            if (GameState.MyTargetType == (int)TargetType.None)
                return;

            int t = GameState.MyTarget;
            if (t < 0)
                return;

            // Compute the actually visible world rect, factoring in zoom.
            // Camera rectangle is in world pixels for the full render target size.
            // When zoomed in (>1), the visible world area is smaller by 1/zoom.
            int camLeftBase = (int)Math.Floor(GameState.Camera.Left);
            int camTopBase = (int)Math.Floor(GameState.Camera.Top);
            int camWidthBase = GameState.ResolutionWidth;
            int camHeightBase = GameState.ResolutionHeight;

            float zoom = GameState.CameraZoom <= 0 ? 1.0f : GameState.CameraZoom;
            int visWidth = (int)Math.Round(camWidthBase / zoom);
            int visHeight = (int)Math.Round(camHeightBase / zoom);

            // Center the visible rect around the camera center
            int camCenterX = camLeftBase + camWidthBase / 2;
            int camCenterY = camTopBase + camHeightBase / 2;
            int camLeft = camCenterX - visWidth / 2;
            int camTop = camCenterY - visHeight / 2;
            int camRight = camLeft + visWidth;
            int camBottom = camTop + visHeight;

            // Compute max allowed deltas based on visible size
            int maxDx = visWidth / 2;
            int maxDy = visHeight / 2;

            bool shouldClear = false;
            int tileX = -1;
            int tileY = -1;

            if (GameState.MyTargetType == (int)TargetType.Player)
            {
                if (!IsPlaying(t) || GetPlayerMap(t) != GetPlayerMap(GameState.MyIndex))
                {
                    shouldClear = true;
                }
                else
                {
                    // Compare distance between player and target against the view half-size (zoom-aware)
                    int px = GetPlayerRawX(GameState.MyIndex) + Constants.TileSize / 2;
                    int py = GetPlayerRawY(GameState.MyIndex) + Constants.TileSize / 2;
                    int tx = GetPlayerRawX(t) + Constants.TileSize / 2;
                    int ty = GetPlayerRawY(t) + Constants.TileSize / 2;
                    if (Math.Abs(tx - px) >= maxDx || Math.Abs(ty - py) >= maxDy)
                    {
                        shouldClear = true;
                        tileX = GetPlayerX(t);
                        tileY = GetPlayerY(t);
                    }
                }
            }
            else if (GameState.MyTargetType == (int)TargetType.Npc)
            {
                int n = t;
                if (n < 0 || n >= MapNpc.Instance.Length || MapNpc.Instance[n].Num < 0)
                {
                    shouldClear = true;
                }
                else
                {
                    int px = GetPlayerRawX(GameState.MyIndex) + Constants.TileSize / 2;
                    int py = GetPlayerRawY(GameState.MyIndex) + Constants.TileSize / 2;
                    int tx = MapNpc.Instance[n].X + Constants.TileSize / 2;
                    int ty = MapNpc.Instance[n].Y + Constants.TileSize / 2;
                    if (Math.Abs(tx - px) >= maxDx || Math.Abs(ty - py) >= maxDy)
                    {
                        shouldClear = true;
                        tileX = (int)Math.Floor(MapNpc.Instance[n].X / (double)Constants.TileSize);
                        tileY = (int)Math.Floor(MapNpc.Instance[n].Y / (double)Constants.TileSize);
                    }
                }
            }
            else
            {
                // Unsupported target types: ignore
                return;
            }

            if (!shouldClear)
                return;

            // Clear locally first
            GameState.MyTarget = -1;
            GameState.MyTargetType = 0;

            // Notify server if we have the tile
            if (tileX >= 0 && tileY >= 0)
            {
                Sender.SendPlayerSearch(tileX, tileY, 0);
            }
        }

        public static void UpdateMapAttributes()
        {
            if (GameState.MapEditorTab == (int) MapEditorTab.Attributes)
            {
                TextRenderer.DrawMapAttributes();
            }
        }

        public static void UpdateDirBlock()
        {
            int x;
            int y;

            if (GameState.MapEditorTab == (int) MapEditorTab.Directions)
            {
                var count10 = (int) Math.Round(GameState.TileView.Right + 1d);
                for (x = (int) Math.Round(GameState.TileView.Left - 1d); x < count10; x++)
                {
                    var count11 = (int) Math.Round(GameState.TileView.Bottom + 1d);
                    for (y = (int) Math.Round(GameState.TileView.Top - 1d); y < count11; y++)
                    {
                        if (GameLogic.IsValidMapPoint(x, y))
                        {
                            DrawDirections(x, y);
                        }
                    }
                }
            }
        }
    }
}