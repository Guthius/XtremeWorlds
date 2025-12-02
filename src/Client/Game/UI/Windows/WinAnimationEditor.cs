using Client.Game.UI;
using Client.Game.UI.Controls;
using Core.Globals;
using System;
using System.IO;

namespace Client.Game.UI.Windows
{
    public static class WinAnimationEditor
    {
        public static int SelectedIndex = 0;

        public static void Init()
        {
            if (!WindowManager.TryGetControl("winAnimationEditor", "lstIndex", out _))
                return;

            SelectedIndex = Math.Clamp(SelectedIndex, 0, Variables.MaxAnimations - 1);
            RefreshList();
            OnLoad(SelectedIndex);
        }

        public static void RefreshList()
        {
            if (!WindowManager.TryGetControl("winAnimationEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list)
                return;

            int prevIndex = SelectedIndex;
            int prevScroll = list.ScrollOffset;

            list.Clear();
            for (int i = 0; i < Variables.MaxAnimations; i++)
            {
                string name = Strings.Trim(Data.Animation[i].Name);
                if (string.IsNullOrWhiteSpace(name)) name = "None";
                list.AddItem($"{i + 1}: {name}");
            }

            if (prevIndex >= 0 && prevIndex < list.Items.Count)
            {
                list.SelectedIndex = prevIndex;
                list.EnsureVisible(prevIndex);
            }

            if (WindowManager.TryGetControl("winAnimationEditor", "sldList", out var sldCtrl) && sldCtrl is ScrollBar sb)
            {
                int visible = list.GetVisibleCount();
                int max = Math.Max(0, list.Items.Count - visible);
                sb.Min = 0; sb.Max = max;
                sb.Value = Math.Clamp(prevScroll, sb.Min, sb.Max);
            }
        }

        public static void OnListMouseDown()
        {
            if (!WindowManager.TryGetControl("winAnimationEditor", "lstIndex", out var ctrl) || ctrl is not ListBox list) return;
            var win = WindowManager.GetWindowByName("winAnimationEditor");
            if (win is null) return;
            int relY = GameState.CurMouseY - (win.Y + list.Y);
            int index = list.GetItemIndexAtPosition(relY);
            if (index < 0 || index >= Variables.MaxAnimations) return;

            SelectedIndex = index;
            GameState.EditorIndex = index;
            list.SelectedIndex = index;
            list.EnsureVisible(index);
            OnLoad(index);
        }

        public static void OnLoad(int index)
        {
            if (index < 0 || index >= Variables.MaxAnimations) return;
            SelectedIndex = index;
            GameState.EditorIndex = index;
            ref var a = ref Data.Animation[index];

            EnsureAnimArrays(ref a);

            // Name
            if (WindowManager.TryGetControl("winAnimationEditor", "txtName", out var nameCtrl) && nameCtrl is TextBox txtName)
                txtName.Text = a.Name ?? string.Empty;

            // Sprite 0
            if (WindowManager.TryGetControl("winAnimationEditor", "nudSprite0", out var s0Ctrl) && s0Ctrl is ScrollBar s0)
            {
                s0.Min = 0; s0.Max = Math.Max(0, GameState.NumAnimations);
                s0.Value = Math.Clamp(a.Sprite[0], s0.Min, s0.Max);
            }

            // Frames 0
            if (WindowManager.TryGetControl("winAnimationEditor", "nudFrameCount0", out var f0Ctrl) && f0Ctrl is ScrollBar f0)
            {
                f0.Min = 0; f0.Max = 64;
                f0.Value = Math.Clamp(a.Frames[0], f0.Min, f0.Max);
            }

            // Loop Time 0
            if (WindowManager.TryGetControl("winAnimationEditor", "nudLoopTime0", out var lt0Ctrl) && lt0Ctrl is ScrollBar lt0)
            {
                lt0.Min = 0; lt0.Max = 10000;
                lt0.Value = Math.Clamp(a.LoopTime[0], lt0.Min, lt0.Max);
            }

            // Loop Count 0
            if (WindowManager.TryGetControl("winAnimationEditor", "nudLoopCount0", out var lc0Ctrl) && lc0Ctrl is ScrollBar lc0)
            {
                lc0.Min = 1; lc0.Max = 64;
                lc0.Value = Math.Clamp(a.LoopCount[0], lc0.Min, lc0.Max);
            }

            // Sprite 1
            if (WindowManager.TryGetControl("winAnimationEditor", "nudSprite1", out var s1Ctrl) && s1Ctrl is ScrollBar s1)
            {
                s1.Min = 0; s1.Max = Math.Max(0, GameState.NumAnimations);
                s1.Value = Math.Clamp(a.Sprite[1], s1.Min, s1.Max);
            }

            // Frames 1
            if (WindowManager.TryGetControl("winAnimationEditor", "nudFrameCount1", out var f1Ctrl) && f1Ctrl is ScrollBar f1)
            {
                f1.Min = 0; f1.Max = 64;
                f1.Value = Math.Clamp(a.Frames[1], f1.Min, f1.Max);
            }

            // Loop Time 1
            if (WindowManager.TryGetControl("winAnimationEditor", "nudLoopTime1", out var lt1Ctrl) && lt1Ctrl is ScrollBar lt1)
            {
                lt1.Min = 0; lt1.Max = 10000;
                lt1.Value = Math.Clamp(a.LoopTime[1], lt1.Min, lt1.Max);
            }

            // Loop Count 1
            if (WindowManager.TryGetControl("winAnimationEditor", "nudLoopCount1", out var lc1Ctrl) && lc1Ctrl is ScrollBar lc1)
            {
                lc1.Min = 1; lc1.Max = 64;
                lc1.Value = Math.Clamp(a.LoopCount[1], lc1.Min, lc1.Max);
            }

            // Sound combo is populated in the skin wiring; just select current
            if (WindowManager.TryGetControl("winAnimationEditor", "cmbSound", out var soundCtrl) && soundCtrl is ComboBox cmbSound)
            {
                // Find index by text match
                int sel = 0;
                var current = a.Sound ?? string.Empty;
                for (int i = 0; i < cmbSound.Items.Count; i++)
                {
                    var item = cmbSound.Items[i] ?? string.Empty;
                    if (string.Equals(item, current, StringComparison.OrdinalIgnoreCase))
                    {
                        sel = i; break;
                    }
                }
                cmbSound.Value = Math.Clamp(sel, 0, Math.Max(0, cmbSound.Items.Count - 1));
            }

            // Previews are assigned in Crystalshire wiring via PictureBox.OnDraw closures.
        }

        public static void OnSave()
        {
            Editors.AnimationEditorOK();
            WindowManager.HideWindow("winAnimationEditor");
        }

        public static void OnCancel()
        {
            Editors.AnimationEditorCancel();
            WindowManager.HideWindow("winAnimationEditor");
        }

        public static void OnDelete()
        {
            Animation.OnClear(GameState.EditorIndex);
            GameState.AnimationChanged[SelectedIndex] = true;
            OnLoad(GameState.EditorIndex);
            RefreshList();
        }

        public static void OnCopy()
        {
            // Prompt for destination and copy current animation (deep copy arrays)
            int src = GameState.EditorIndex;
            if (src < 0 || src >= Variables.MaxAnimations) return;

            var a = Data.Animation[src];
            var n = a;
            if (a.Sprite != null) n.Sprite = (int[])a.Sprite.Clone();
            if (a.Frames != null) n.Frames = (int[])a.Frames.Clone();
            if (a.LoopCount != null) n.LoopCount = (int[])a.LoopCount.Clone();
            if (a.LoopTime != null) n.LoopTime = (int[])a.LoopTime.Clone();

            int def = src + 1;
            var oneBased = Editors.PromptIndex(null, "Paste Animation", $"Paste animation into index (1..{Variables.MaxAnimations}):", 1, Variables.MaxAnimations, def);
            if (oneBased == null) return;
            int dst = oneBased.Value - 1;

            EnsureAnimArrays(ref n);
            Data.Animation[dst] = n;
            GameState.AnimationChanged[dst] = true;

            RefreshList();
            OnLoad(dst);
        }

        private static void EnsureAnimArrays(ref Core.Globals.Type.Animation a)
        {
            a.Sprite ??= new int[2];
            a.Frames ??= new int[2];
            a.LoopCount ??= new int[2];
            a.LoopTime ??= new int[2];
            if (a.Sprite.Length < 2) Array.Resize(ref a.Sprite, 2);
            if (a.Frames.Length < 2) Array.Resize(ref a.Frames, 2);
            if (a.LoopCount.Length < 2) Array.Resize(ref a.LoopCount, 2);
            if (a.LoopTime.Length < 2) Array.Resize(ref a.LoopTime, 2);
            if (a.LoopCount[0] == 0) a.LoopCount[0] = 1;
            if (a.LoopCount[1] == 0) a.LoopCount[1] = 1;
            if (a.LoopTime[0] == 0) a.LoopTime[0] = 1;
            if (a.LoopTime[1] == 0) a.LoopTime[1] = 1;
        }
    }
}