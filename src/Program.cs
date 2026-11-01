using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CrosshairOverlay
{
    sealed class Config
    {
        public Color[] Colors = new Color[] { Color.Lime };
        public string Shape = "cross";
        public Color OutlineColor = Color.Black;
        public int Alpha = 255;
        public int ScreenIndex = 0;
        public bool PixelSnap = true;
        public int AlphaMode = 0;   // 0 = color key (most compatible), 1 = per-pixel alpha
        public string Language = "";   // "", zh, en, ru, pt, es, ja, ko, tr
        public bool SyncArms = true; // keep horizontal/vertical length, thickness and gap identical
        public int OutlineMode = 1;
        public bool SoftEdge = false;
        public int Upscale = 0;
        public int Glow = 30;      // 0..100 halo strength around the crosshair (alpha mode only)   // 0 = nearest (crisp blocks), 1 = bilinear (smoother/softer)  // 0 = outline on all four sides, 1 = sides only (no caps, gap stays clear)

        public int CanvasWidth = 800;
        public int CanvasHeight = 600;
        public string CanvasFit = "stretch";
        public List<Point> CanvasPresets = new List<Point>();
        public double AnchorX = 50.0;
        public double AnchorY = 50.0;

        public double SizeX = 6, SizeY = 10;
        public double ThicknessX = 2, ThicknessY = 2;
        public double GapX = 4, GapY = 4;
        public int Outline = 1;

        public Keys KeyToggle = Keys.F8;
        public Keys KeyColor = Keys.F9;
        public Keys KeyReload = Keys.F10;
        public Keys KeyCanvas = Keys.F7;
        public Keys KeySettings = Keys.F6;
        public Keys KeyExit = Keys.Q;
        public bool ExitCtrl = true;
        public bool ExitAlt = true;

        public string Path = "crosshair.ini";

        public static Color ParseColor(string s, Color dflt)
        {
            if (string.IsNullOrEmpty(s)) return dflt;
            s = s.Trim();
            if (s.StartsWith("#")) s = s.Substring(1);
            try
            {
                if (s.Length == 6)
                {
                    int v = int.Parse(s, NumberStyles.HexNumber);
                    return Color.FromArgb(255, (v >> 16) & 0xFF, (v >> 8) & 0xFF, v & 0xFF);
                }
                if (s.Length == 8)
                {
                    uint v = uint.Parse(s, NumberStyles.HexNumber);
                    return Color.FromArgb((int)((v >> 24) & 0xFF), (int)((v >> 16) & 0xFF), (int)((v >> 8) & 0xFF), (int)(v & 0xFF));
                }
            }
            catch { }
            return dflt;
        }

        public static string Hex(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        public static Keys ParseKey(string s)
        {
            if (string.IsNullOrEmpty(s)) return Keys.None;
            string t = s.Trim().ToUpperInvariant();
            if (t.StartsWith("F") && t.Length <= 3)
            {
                int n;
                if (int.TryParse(t.Substring(1), out n) && n >= 1 && n <= 24) return (Keys)((int)Keys.F1 + n - 1);
            }
            if (t.Length == 1)
            {
                char c = t[0];
                if (c >= 'A' && c <= 'Z') return (Keys)c;
                if (c >= '0' && c <= '9') return (Keys)c;
            }
            try { return (Keys)Enum.Parse(typeof(Keys), t, true); }
            catch { return Keys.None; }
        }

        static void ParseHotkey(string s, out Keys key, out bool ctrl, out bool alt)
        {
            key = Keys.None; ctrl = false; alt = false;
            if (string.IsNullOrEmpty(s)) return;
            string[] parts = s.Split('+');
            foreach (string raw in parts)
            {
                string p = raw.Trim();
                if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || p.Equals("Control", StringComparison.OrdinalIgnoreCase)) ctrl = true;
                else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) alt = true;
                else key = ParseKey(p);
            }
        }

        static double Num(string s, double dflt)
        {
            double d;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
            return dflt;
        }

        static List<Point> ParsePresets(string v)
        {
            List<Point> list = new List<Point>();
            foreach (string part in v.Split(','))
            {
                string p = part.Trim().ToLowerInvariant();
                if (p.Length == 0) continue;
                if (p == "native" || p == "screen" || p == "0x0") { list.Add(new Point(0, 0)); continue; }
                string[] wh = p.Split('x');
                if (wh.Length != 2) continue;
                int w, h;
                if (int.TryParse(wh[0], out w) && int.TryParse(wh[1], out h) && w > 0 && h > 0)
                    list.Add(new Point(w, h));
            }
            return list;
        }

        public static Config Load(string path)
        {
            Config c = new Config();
            c.Path = path;
            if (!File.Exists(path))
            {
                c.Save(path);
                return c;
            }

            string toggle = "F8", colorKey = "F9", reloadKey = "F10", canvasKey = "F7", settingsKey = "F6", exitKey = "Ctrl+Alt+Q";
            List<Color> colors = new List<Color>();

            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string k = line.Substring(0, eq).Trim().ToLowerInvariant();
                string v = line.Substring(eq + 1).Trim();

                switch (k)
                {
                    case "colors":
                        colors.Clear();
                        foreach (string part in v.Split(','))
                        {
                            Color col = ParseColor(part, Color.Empty);
                            if (col != Color.Empty) colors.Add(col);
                        }
                        if (colors.Count == 0) colors.Add(Color.Lime);
                        c.Colors = colors.ToArray();
                        break;
                    case "shape": c.Shape = v.ToLowerInvariant(); break;
                    case "outlinecolor": c.OutlineColor = ParseColor(v, Color.Black); break;
                    case "alpha": c.Alpha = (int)Math.Max(0, Math.Min(255, Num(v, 255))); break;
                    case "screen": c.ScreenIndex = (int)Num(v, 0); break;
                    case "pixelsnap": c.PixelSnap = v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase); break;
                    case "alphamode": c.AlphaMode = (int)Num(v, 0); break;
                    case "syncarms": c.SyncArms = v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase); break;
                    case "outlinemode": c.OutlineMode = (int)Num(v, 1); break;
                    case "language": c.Language = v; break;
                    case "softedge": c.SoftEdge = v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase); break;
                    case "upscale": c.Upscale = (int)Num(v, 0); break;
                    case "glow": c.Glow = (int)Math.Max(0, Math.Min(100, Num(v, 30))); break;

                    case "canvaswidth": c.CanvasWidth = (int)Num(v, c.CanvasWidth); break;
                    case "canvasheight": c.CanvasHeight = (int)Num(v, c.CanvasHeight); break;
                    case "canvasfit": c.CanvasFit = v.ToLowerInvariant(); break;
                    case "canvaspresets": c.CanvasPresets = ParsePresets(v); break;
                    case "anchorx": c.AnchorX = Num(v, 50); break;
                    case "anchory": c.AnchorY = Num(v, 50); break;

                    case "sizex": c.SizeX = Num(v, c.SizeX); break;
                    case "sizey": c.SizeY = Num(v, c.SizeY); break;
                    case "thicknessx": c.ThicknessX = Num(v, c.ThicknessX); break;
                    case "thicknessy": c.ThicknessY = Num(v, c.ThicknessY); break;
                    case "gapx": c.GapX = Num(v, c.GapX); break;
                    case "gapy": c.GapY = Num(v, c.GapY); break;
                    case "outline": c.Outline = (int)Num(v, 1); break;

                    case "keytoggle": toggle = v; break;
                    case "keycolor": colorKey = v; break;
                    case "keyreload": reloadKey = v; break;
                    case "keycanvas": canvasKey = v; break;
                    case "keysettings": settingsKey = v; break;
                    case "keyexit": exitKey = v; break;
                }
            }

            bool ctrl, alt;
            ParseHotkey(toggle, out c.KeyToggle, out ctrl, out alt);
            ParseHotkey(colorKey, out c.KeyColor, out ctrl, out alt);
            ParseHotkey(reloadKey, out c.KeyReload, out ctrl, out alt);
            ParseHotkey(canvasKey, out c.KeyCanvas, out ctrl, out alt);
            ParseHotkey(settingsKey, out c.KeySettings, out ctrl, out alt);
            ParseHotkey(exitKey, out c.KeyExit, out c.ExitCtrl, out c.ExitAlt);

            if (c.SizeX < 0.5) c.SizeX = 0.5;
            if (c.SizeY < 0.5) c.SizeY = 0.5;
            if (c.ThicknessX < 0.5) c.ThicknessX = 0.5;
            if (c.ThicknessY < 0.5) c.ThicknessY = 0.5;
            if (c.GapX < 0) c.GapX = 0;
            if (c.GapY < 0) c.GapY = 0;
            if (c.Outline < 0) c.Outline = 0;
            c.AnchorX = Math.Max(0, Math.Min(100, c.AnchorX));
            c.AnchorY = Math.Max(0, Math.Min(100, c.AnchorY));
            return c;
        }

        public void CopyFrom(Config o)
        {
            Colors = (Color[])o.Colors.Clone();
            Shape = o.Shape;
            OutlineColor = o.OutlineColor;
            Alpha = o.Alpha;
            ScreenIndex = o.ScreenIndex;
            PixelSnap = o.PixelSnap;
            AlphaMode = o.AlphaMode;
            SyncArms = o.SyncArms;
            OutlineMode = o.OutlineMode;
            CanvasWidth = o.CanvasWidth;
            CanvasHeight = o.CanvasHeight;
            CanvasFit = o.CanvasFit;
            CanvasPresets = new List<Point>(o.CanvasPresets);
            AnchorX = o.AnchorX;
            AnchorY = o.AnchorY;
            SizeX = o.SizeX; SizeY = o.SizeY;
            ThicknessX = o.ThicknessX; ThicknessY = o.ThicknessY;
            GapX = o.GapX; GapY = o.GapY;
            Outline = o.Outline;
        }

        public static string ProfileLine(Config c)
        {
            return c.CanvasWidth + "x" + c.CanvasHeight + "|" + c.CanvasFit + "|" +
                   F(c.SizeX) + "," + F(c.SizeY) + "|" +
                   F(c.ThicknessX) + "," + F(c.ThicknessY) + "|" +
                   F(c.GapX) + "," + F(c.GapY);
        }

        static string F(double d)
        {
            return d.ToString("0.##", CultureInfo.InvariantCulture);
        }

        static double[] Pair(string s)
        {
            string[] p = s.Split(',');
            if (p.Length != 2) return null;
            double a, b;
            if (!double.TryParse(p[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out a)) return null;
            if (!double.TryParse(p[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out b)) return null;
            return new double[] { a, b };
        }

        public static bool ApplyProfileLine(Config c, string v)
        {
            string[] p = v.Split('|');
            if (p.Length < 5) return false;
            string[] wh = p[0].Trim().ToLowerInvariant().Split('x');
            int w, h;
            if (wh.Length != 2 || !int.TryParse(wh[0], out w) || !int.TryParse(wh[1], out h) || w < 8 || h < 8) return false;
            string fit = p[1].Trim().ToLowerInvariant();
            if (fit != "fit" && fit != "native") fit = "stretch";
            c.CanvasWidth = w; c.CanvasHeight = h; c.CanvasFit = fit;
            double[] s = Pair(p[2]); if (s != null) { c.SizeX = Math.Max(0.5, s[0]); c.SizeY = Math.Max(0.5, s[1]); }
            double[] t = Pair(p[3]); if (t != null) { c.ThicknessX = Math.Max(0.5, t[0]); c.ThicknessY = Math.Max(0.5, t[1]); }
            double[] g = Pair(p[4]); if (g != null) { c.GapX = Math.Max(0, g[0]); c.GapY = Math.Max(0, g[1]); }
            return true;
        }

        public void Save(string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("; ===== Crosshair Overlay =====");
            sb.AppendLine("; F6 settings / F7 canvas / F8 show-hide / F9 color / F10 reload / Ctrl+Alt+Q quit");
            sb.AppendLine("");
            sb.AppendLine("Colors=" + JoinColors());
            sb.AppendLine("Shape=" + Shape);
            sb.AppendLine("OutlineColor=" + Hex(OutlineColor));
            sb.AppendLine("Outline=" + Outline);
            sb.AppendLine("Alpha=" + Alpha);
            sb.AppendLine("Screen=" + ScreenIndex);
            sb.AppendLine("PixelSnap=" + (PixelSnap ? 1 : 0));
            sb.AppendLine("AlphaMode=" + AlphaMode);
            sb.AppendLine("Language=" + Language);
            sb.AppendLine("SyncArms=" + (SyncArms ? 1 : 0));
            sb.AppendLine("OutlineMode=" + OutlineMode);
            sb.AppendLine("SoftEdge=" + (SoftEdge ? 1 : 0));
            sb.AppendLine("Upscale=" + Upscale);
            sb.AppendLine("Glow=" + Glow);
            sb.AppendLine("");
            sb.AppendLine("CanvasWidth=" + CanvasWidth);
            sb.AppendLine("CanvasHeight=" + CanvasHeight);
            sb.AppendLine("CanvasFit=" + CanvasFit);
            StringBuilder ps = new StringBuilder();
            foreach (Point p in CanvasPresets)
            {
                if (ps.Length > 0) ps.Append(",");
                ps.Append(p.X == 0 ? "native" : p.X + "x" + p.Y);
            }
            sb.AppendLine("CanvasPresets=" + ps);
            sb.AppendLine("AnchorX=" + AnchorX.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("AnchorY=" + AnchorY.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("");
            sb.AppendLine("SizeX=" + SizeX.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("SizeY=" + SizeY.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("ThicknessX=" + ThicknessX.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("ThicknessY=" + ThicknessY.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("GapX=" + GapX.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("GapY=" + GapY.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("");
            sb.AppendLine("KeyToggle=" + KeyToggle);
            sb.AppendLine("KeyColor=" + KeyColor);
            sb.AppendLine("KeyCanvas=" + KeyCanvas);
            sb.AppendLine("KeySettings=" + KeySettings);
            sb.AppendLine("KeyReload=" + KeyReload);
            sb.AppendLine("KeyExit=" + (ExitCtrl ? "Ctrl+" : "") + (ExitAlt ? "Alt+" : "") + KeyExit);
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        string JoinColors()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Color c in Colors)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append(Hex(c));
            }
            return sb.ToString();
        }
    }

    static class PresetStore
    {
        public static string PathFor(string cfgPath)
        {
            string dir = Path.GetDirectoryName(cfgPath);
            if (string.IsNullOrEmpty(dir)) dir = ".";
            return Path.Combine(dir, "presets.ini");
        }

        public static string CleanName(string name)
        {
            if (name == null) return "";
            string t = name.Trim().Replace("[", "").Replace("]", "").Replace("=", "");
            if (t.Length > 40) t = t.Substring(0, 40);
            return t;
        }

        public static List<string> Names(string path)
        {
            List<string> list = new List<string>();
            if (!File.Exists(path)) return list;
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string t = raw.Trim();
                if (t.Length > 2 && t.StartsWith("[") && t.EndsWith("]"))
                    list.Add(t.Substring(1, t.Length - 2));
            }
            return list;
        }

        static List<string> StripSection(List<string> src, string name)
        {
            List<string> outp = new List<string>();
            bool skipping = false;
            foreach (string raw in src)
            {
                string t = raw.Trim();
                if (t.Length > 2 && t.StartsWith("[") && t.EndsWith("]"))
                {
                    string n = t.Substring(1, t.Length - 2);
                    skipping = string.Equals(n, name, StringComparison.OrdinalIgnoreCase);
                    if (skipping) continue;
                }
                if (!skipping) outp.Add(raw);
            }
            return outp;
        }

        public static void Save(string path, string name, Config c)
        {
            List<string> lines = new List<string>();
            if (File.Exists(path)) lines.AddRange(File.ReadAllLines(path, Encoding.UTF8));
            lines = StripSection(lines, name);
            if (lines.Count > 0 && lines[lines.Count - 1].Trim().Length != 0) lines.Add("");
            lines.Add("[" + name + "]");
            lines.Add("Canvas=" + c.CanvasWidth + "x" + c.CanvasHeight);
            lines.Add("Fit=" + c.CanvasFit);
            lines.Add("Anchor=" + F(c.AnchorX) + "," + F(c.AnchorY));
            lines.Add("Size=" + F(c.SizeX) + "," + F(c.SizeY));
            lines.Add("Thickness=" + F(c.ThicknessX) + "," + F(c.ThicknessY));
            lines.Add("Gap=" + F(c.GapX) + "," + F(c.GapY));
            lines.Add("Color=" + Config.Hex(c.Colors[0]));
            lines.Add("Shape=" + c.Shape);
            lines.Add("Outline=" + c.Outline);
            lines.Add("OutlineColor=" + Config.Hex(c.OutlineColor));
            lines.Add("OutlineMode=" + c.OutlineMode);
            lines.Add("SoftEdge=" + (c.SoftEdge ? 1 : 0));
            lines.Add("Upscale=" + c.Upscale);
            lines.Add("Glow=" + c.Glow);
            lines.Add("SyncArms=" + (c.SyncArms ? 1 : 0));
            lines.Add("Alpha=" + c.Alpha);
            File.WriteAllLines(path, lines.ToArray(), new UTF8Encoding(false));
        }

        static string F(double d)
        {
            return d.ToString("0.##", CultureInfo.InvariantCulture);
        }

        static double D(string s, double dflt)
        {
            double d;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
            return dflt;
        }

        static void Pair(string s, ref double a, ref double b)
        {
            string[] p = (s ?? "").Split(',');
            if (p.Length == 2)
            {
                a = D(p[0], a);
                b = D(p[1], b);
            }
        }

        public static bool Apply(string path, string name, Config c)
        {
            if (!File.Exists(path)) return false;
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool inSection = false;
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string t = raw.Trim();
                if (t.Length > 2 && t.StartsWith("[") && t.EndsWith("]"))
                {
                    inSection = string.Equals(t.Substring(1, t.Length - 2), name, StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!inSection) continue;
                int eq = t.IndexOf('=');
                if (eq <= 0) continue;
                map[t.Substring(0, eq).Trim()] = t.Substring(eq + 1).Trim();
            }
            if (map.Count == 0) return false;

            string v;
            if (map.TryGetValue("Canvas", out v))
            {
                string[] wh = v.ToLowerInvariant().Split('x');
                int w, h;
                if (wh.Length == 2 && int.TryParse(wh[0], out w) && int.TryParse(wh[1], out h) && w >= 8 && h >= 8)
                {
                    c.CanvasWidth = w;
                    c.CanvasHeight = h;
                }
            }
            if (map.TryGetValue("Fit", out v)) c.CanvasFit = v.ToLowerInvariant();
            if (map.TryGetValue("Anchor", out v)) Pair(v, ref c.AnchorX, ref c.AnchorY);
            if (map.TryGetValue("Size", out v)) Pair(v, ref c.SizeX, ref c.SizeY);
            if (map.TryGetValue("Thickness", out v)) Pair(v, ref c.ThicknessX, ref c.ThicknessY);
            if (map.TryGetValue("Gap", out v)) Pair(v, ref c.GapX, ref c.GapY);
            if (map.TryGetValue("Color", out v))
            {
                Color col = Config.ParseColor(v, Color.Empty);
                if (col != Color.Empty) c.Colors[0] = col;
            }
            if (map.TryGetValue("Shape", out v)) c.Shape = v.ToLowerInvariant();
            if (map.TryGetValue("Outline", out v)) c.Outline = (int)D(v, c.Outline);
            if (map.TryGetValue("OutlineColor", out v))
            {
                Color col = Config.ParseColor(v, Color.Empty);
                if (col != Color.Empty) c.OutlineColor = col;
            }
            if (map.TryGetValue("OutlineMode", out v)) c.OutlineMode = (int)D(v, c.OutlineMode);
            if (map.TryGetValue("SoftEdge", out v)) c.SoftEdge = v == "1";
            if (map.TryGetValue("Upscale", out v)) c.Upscale = (int)D(v, 0);
            if (map.TryGetValue("Glow", out v)) c.Glow = (int)Math.Max(0, Math.Min(100, D(v, 30)));
            if (map.TryGetValue("SyncArms", out v)) c.SyncArms = v == "1";
            if (map.TryGetValue("Alpha", out v)) c.Alpha = (int)Math.Max(0, Math.Min(255, D(v, c.Alpha)));
            return true;
        }

        public static void Delete(string path, string name)
        {
            if (!File.Exists(path)) return;
            List<string> lines = StripSection(new List<string>(File.ReadAllLines(path, Encoding.UTF8)), name);
            File.WriteAllLines(path, lines.ToArray(), new UTF8Encoding(false));
        }
    }

    static class CrosshairDraw
    {
        public static void Render(Graphics g, Config cfg, double cxp, double cyp, double sx, double sy, int alpha, Color col, Color outlineCol)
        {
            if (cfg.PixelSnap) { cxp = Math.Round(cxp); cyp = Math.Round(cyp); }
            if (cfg.Outline > 0) Pass(g, cfg, cxp, cyp, sx, sy, alpha, outlineCol, true);
            Pass(g, cfg, cxp, cyp, sx, sy, alpha, col, false);
        }

        static void Pass(Graphics g, Config cfg, double cxp, double cyp, double sx, double sy, int alpha, Color col, bool outlinePass)
        {
            Color c = Color.FromArgb(alpha, col);
            double gx = cfg.GapX * sx, gy = cfg.GapY * sy;
            double lx = cfg.SizeX * sx, ly = cfg.SizeY * sy;
            double tx = cfg.ThicknessX * sx, ty = cfg.ThicknessY * sy;
            double o = outlinePass ? cfg.Outline : 0;
            double ox2 = o * sx, oy2 = o * sy;

            if (cfg.PixelSnap)
            {
                gx = Math.Round(gx); gy = Math.Round(gy);
                lx = Math.Max(1, Math.Round(lx)); ly = Math.Max(1, Math.Round(ly));
                tx = Math.Max(1, Math.Round(tx)); ty = Math.Max(1, Math.Round(ty));
                ox2 = Math.Round(ox2); oy2 = Math.Round(oy2);
            }
            else if (o > 0)
            {
                ox2 = Math.Max(ox2, 1.0);
                oy2 = Math.Max(oy2, 1.0);
            }

            double txa = Math.Max(1.0, tx), tya = Math.Max(1.0, ty);
            double bx = cxp - Math.Floor(txa / 2.0);
            double by = cyp - Math.Floor(tya / 2.0);

            using (SolidBrush br = new SolidBrush(c))
            using (Pen pen = new Pen(c, (float)Math.Max(1.0, (txa + tya) / 2.0)))
            {
                switch (cfg.Shape)
                {
                    case "dot":
                        g.FillEllipse(br, R(cxp - lx / 2 - ox2, cyp - ly / 2 - oy2, lx + ox2 * 2, ly + oy2 * 2));
                        break;
                    case "circle":
                        {
                            double rx = gx + lx + ox2, ry = gy + ly + oy2;
                            g.DrawEllipse(pen, R(cxp - rx, cyp - ry, rx * 2, ry * 2));
                            break;
                        }
                    case "x":
                        {
                            double d = (gx + lx) * 0.7071, gd = gx * 0.7071;
                            Seg(g, pen, cxp - d, cyp - d, cxp - gd, cyp - gd);
                            Seg(g, pen, cxp + d, cyp - d, cxp + gd, cyp - gd);
                            Seg(g, pen, cxp - d, cyp + d, cxp - gd, cyp + gd);
                            Seg(g, pen, cxp + d, cyp + d, cxp + gd, cyp + gd);
                            break;
                        }
                    case "t":
                        {
                            Bar(g, cfg, br, cxp - gx - lx, by, lx, tya, ox2, oy2, true);
                            Bar(g, cfg, br, cxp + gx, by, lx, tya, ox2, oy2, true);
                            Bar(g, cfg, br, bx, cyp + gy, txa, ly, ox2, oy2, false);
                            break;
                        }
                    default:
                        {
                            Bar(g, cfg, br, cxp - gx - lx, by, lx, tya, ox2, oy2, true);
                            Bar(g, cfg, br, cxp + gx, by, lx, tya, ox2, oy2, true);
                            Bar(g, cfg, br, bx, cyp - gy - ly, txa, ly, ox2, oy2, false);
                            Bar(g, cfg, br, bx, cyp + gy, txa, ly, ox2, oy2, false);
                            break;
                        }
                }
            }
        }

        static RectangleF R(double x, double y, double w, double h)
        {
            return new RectangleF((float)x, (float)y, (float)Math.Max(1.0, w), (float)Math.Max(1.0, h));
        }

        static void Bar(Graphics g, Config cfg, SolidBrush br, double x, double y, double w, double h, double ox2, double oy2, bool horizontal)
        {
            if (cfg.OutlineMode == 0)
            {
                g.FillRectangle(br, R(x - ox2, y - oy2, w + ox2 * 2, h + oy2 * 2));
                return;
            }
            if (horizontal)
                g.FillRectangle(br, R(x, y - oy2, w, h + oy2 * 2));
            else
                g.FillRectangle(br, R(x - ox2, y, w + ox2 * 2, h));
        }

        static void Seg(Graphics g, Pen pen, double x1, double y1, double x2, double y2)
        {
            g.DrawLine(pen, (float)x1, (float)y1, (float)x2, (float)y2);
        }
    }

    sealed class OverlayForm : Form
    {
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_TOOLWINDOW = 0x80;
        const int WM_HOTKEY = 0x0312;
        const int MOD_ALT = 1;
        const int MOD_CONTROL = 2;
        const uint SWP_NOSIZE = 0x1;
        const uint SWP_NOMOVE = 0x2;
        const uint SWP_NOACTIVATE = 0x10;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly Color KEY_COLOR = Color.FromArgb(255, 0, 254);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        internal static extern bool SetProcessDPIAware();

        const int WS_EX_LAYERED = 0x80000;

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        struct SIZE { public int cx, cy; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObj);
        [DllImport("user32.dll")]
        static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst,
            ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFOHEADER bmi, int usage, out IntPtr bits, IntPtr hSection, int offset);

        const int ID_TOGGLE = 1;
        const int ID_COLOR = 2;
        const int ID_RELOAD = 3;
        const int ID_EXIT = 4;
        const int ID_CANVAS = 5;
        const int ID_SETTINGS = 6;

        Config cfg;
        int colorIndex;
        int canvasIndex;
        bool shown = true;
        bool alphaMode;
        bool topmostPaused;
        NotifyIcon tray;
        Timer topmostTimer;
        SettingsForm settings;
        PresetGalleryForm gallery;
        ContextMenuStrip trayMenu;
        Config baseCfg;
        Dictionary<string, string> profiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string activeProfile = "";
        string lastForeground = "";
        Timer profileTimer;

        Rectangle screenRect;
        double oxAbs, oyAbs, sx, sy, cw, ch;
        double ox, oy;

        public event EventHandler CanvasChanged;

        public Config Cfg { get { return cfg; } }

        public bool UsingAlphaMode { get { return alphaMode; } }

        public OverlayForm(Config c)
        {
            cfg = c;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            ApplyTransparency();
            UpdateGeometry();
            BuildTray();
            Shown += delegate { Redraw(); };
            baseCfg = new Config();
            baseCfg.CopyFrom(cfg);
            LoadProfiles();
            profileTimer = new Timer();
            profileTimer.Interval = 800;
            profileTimer.Tick += ProfileTick;
            profileTimer.Start();
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
            topmostTimer = new Timer();
            topmostTimer.Interval = 1500;
            topmostTimer.Tick += delegate
            {
                if (shown && !topmostPaused && IsHandleCreated)
                    SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            };
            topmostTimer.Start();
        }

        void ApplyTransparency()
        {
            alphaMode = cfg.AlphaMode == 1;
            if (alphaMode) return;   // manual UpdateLayeredWindow path, no WinForms transparency
            AllowTransparency = false;
            BackColor = KEY_COLOR;
            TransparencyKey = KEY_COLOR;
        }

        public void PauseTopMost(bool pause)
        {
            topmostPaused = pause;
        }

        void Redraw()
        {
            Invalidate();
            if (alphaMode && IsHandleCreated) RenderLayered();
        }

        public void RefreshOverlay()
        {
            UpdateGeometry();
            Redraw();
        }

        public void QuitApp()
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
            if (topmostTimer != null) topmostTimer.Stop();
            if (profileTimer != null) profileTimer.Stop();
            if (settings != null && !settings.IsDisposed) { settings.Dispose(); settings = null; }
            if (gallery != null && !gallery.IsDisposed) { gallery.Dispose(); gallery = null; }
            if (tray != null) { tray.Visible = false; tray.Dispose(); tray = null; }
            Application.Exit();
        }

        public void ShowSettings()
        {
            if (settings == null || settings.IsDisposed) settings = new SettingsForm(this);
            settings.Show();
            settings.WindowState = FormWindowState.Normal;
            settings.BringToFront();
            settings.Activate();
        }

        public void RestyleTray()
        {
            if (trayMenu == null || trayMenu.Items.Count < 8) return;
            trayMenu.Items[0].Text = Loc.T("tray_settings") + " (" + cfg.KeySettings + ")";
            trayMenu.Items[1].Text = Loc.T("tray_gallery");
            trayMenu.Items[2].Text = Loc.T("tray_show") + " (" + cfg.KeyToggle + ")";
            trayMenu.Items[3].Text = Loc.T("tray_color") + " (" + cfg.KeyColor + ")";
            trayMenu.Items[4].Text = Loc.T("tray_canvas") + " (" + cfg.KeyCanvas + ")";
            trayMenu.Items[5].Text = Loc.T("tray_reload") + " (" + cfg.KeyReload + ")";
            trayMenu.Items[7].Text = Loc.T("tray_exit");
        }

        public void RestyleSettings()
        {
            if (settings != null && !settings.IsDisposed) settings.Relocalize();
            if (gallery != null && !gallery.IsDisposed) { gallery.Dispose(); gallery = null; }
        }

        public void ShowGallery()
        {
            if (gallery == null || gallery.IsDisposed) gallery = new PresetGalleryForm(this);
            gallery.Show();
            gallery.BringToFront();
            gallery.Activate();
        }

        public void ReloadSettingsUi()
        {
            if (settings != null && !settings.IsDisposed) settings.ReloadUi();
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

        static string ForegroundProcess()
        {
            try
            {
                IntPtr h = GetForegroundWindow();
                if (h == IntPtr.Zero) return "";
                uint pid;
                GetWindowThreadProcessId(h, out pid);
                if (pid == 0) return "";
                using (Process p = Process.GetProcessById((int)pid))
                    return (p.ProcessName ?? "").ToLowerInvariant();
            }
            catch { return ""; }
        }

        string ProfilesPath
        {
            get { return Path.Combine(Path.GetDirectoryName(cfg.Path), "profiles.ini"); }
        }

        void LoadProfiles()
        {
            profiles.Clear();
            string path = ProfilesPath;
            if (!File.Exists(path)) return;
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string t = raw.Trim();
                if (t.Length == 0 || t.StartsWith(";") || t.StartsWith("[")) continue;
                int eq = t.IndexOf('=');
                if (eq <= 0) continue;
                string k = t.Substring(0, eq).Trim().ToLowerInvariant();
                if (k.EndsWith(".exe")) k = k.Substring(0, k.Length - 4);
                profiles[k] = t.Substring(eq + 1).Trim();
            }
        }

        void ProfileTick(object sender, EventArgs e)
        {
            string proc = ForegroundProcess();
            if (proc.Length == 0) return;
            if (proc.StartsWith("crosshairoverlay")) return;
            if (proc == lastForeground) return;
            lastForeground = proc;

            string line;
            if (profiles.TryGetValue(proc, out line) && Config.ApplyProfileLine(cfg, line))
            {
                activeProfile = proc;
                if (settings != null && !settings.IsDisposed) settings.ReloadUi();
                RefreshOverlay();
            }
            else if (activeProfile.Length > 0)
            {
                cfg.CopyFrom(baseCfg);
                activeProfile = "";
                if (settings != null && !settings.IsDisposed) settings.ReloadUi();
                RefreshOverlay();
            }
        }

        public string SaveProfileForCurrentGame()
        {
            string proc = lastForeground;
            if (proc.Length == 0) return "";
            string line = Config.ProfileLine(cfg);
            string path = ProfilesPath;
            List<string> lines = new List<string>();
            bool replaced = false;
            if (File.Exists(path))
            {
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string t = raw.Trim();
                    int eq = t.IndexOf('=');
                    string k = eq > 0 ? t.Substring(0, eq).Trim().ToLowerInvariant() : "";
                    if (k.EndsWith(".exe")) k = k.Substring(0, k.Length - 4);
                    if (k.Length > 0 && k == proc) { lines.Add(proc + ".exe = " + line); replaced = true; }
                    else lines.Add(raw);
                }
            }
            if (!replaced) lines.Add(proc + ".exe = " + line);
            File.WriteAllLines(path, lines.ToArray(), new UTF8Encoding(false));
            profiles[proc] = line;
            activeProfile = proc;
            return proc;
        }

        void OnDisplayChanged(object sender, EventArgs e)
        {
            UpdateGeometry();
            Redraw();
        }

        void UpdateGeometry()
        {
            Screen[] all = Screen.AllScreens;
            Screen target = all[0];
            if (cfg.ScreenIndex >= 0 && cfg.ScreenIndex < all.Length) target = all[cfg.ScreenIndex];
            screenRect = target.Bounds;

            double sw = screenRect.Width, sh = screenRect.Height;
            double cwv = cfg.CanvasWidth, chv = cfg.CanvasHeight;
            if (cwv < 8 || chv < 8 || cfg.CanvasFit == "native") { cwv = sw; chv = sh; }

            if (cfg.CanvasFit == "fit")
            {
                double s = Math.Min(sw / cwv, sh / chv);
                sx = s; sy = s;
                oxAbs = screenRect.X + (sw - cwv * s) / 2.0;
                oyAbs = screenRect.Y + (sh - chv * s) / 2.0;
            }
            else if (cfg.CanvasFit == "native")
            {
                sx = 1; sy = 1; oxAbs = screenRect.X; oyAbs = screenRect.Y;
            }
            else
            {
                sx = sw / cwv; sy = sh / chv;
                oxAbs = screenRect.X; oyAbs = screenRect.Y;
            }
            cw = cwv; ch = chv;

            double px = oxAbs + cw * cfg.AnchorX / 100.0 * sx;
            double py = oyAbs + ch * cfg.AnchorY / 100.0 * sy;

            double hx = (cfg.GapX + cfg.SizeX + cfg.Outline + 3) * sx;
            double hy = (cfg.GapY + cfg.SizeY + cfg.Outline + 3) * sy;
            int w = (int)Math.Ceiling(hx * 2) + 40;
            int h = (int)Math.Ceiling(hy * 2) + 40;
            if (w < 48) w = 48;
            if (h < 48) h = 48;

            Bounds = new Rectangle((int)Math.Round(px) - w / 2, (int)Math.Round(py) - h / 2, w, h);
            ox = oxAbs - Bounds.X;
            oy = oyAbs - Bounds.Y;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW;
                if (alphaMode) cp.ExStyle |= WS_EX_LAYERED;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        void BuildTray()
        {
            tray = new NotifyIcon();
            tray.Icon = SystemIcons.Application;
            tray.Text = "Crosshair Overlay";
            tray.Visible = true;
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(Loc.T("tray_settings") + " (" + cfg.KeySettings + ")", null, delegate { ShowSettings(); });
            menu.Items.Add(Loc.T("tray_gallery"), null, delegate { ShowGallery(); });
            menu.Items.Add(Loc.T("tray_show") + " (" + cfg.KeyToggle + ")", null, delegate { Toggle(); });
            menu.Items.Add(Loc.T("tray_color") + " (" + cfg.KeyColor + ")", null, delegate { NextColor(); });
            menu.Items.Add(Loc.T("tray_canvas") + " (" + cfg.KeyCanvas + ")", null, delegate { NextCanvas(); });
            menu.Items.Add(Loc.T("tray_reload") + " (" + cfg.KeyReload + ")", null, delegate { Reload(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Loc.T("tray_exit"), null, delegate { QuitApp(); });
            trayMenu = menu;
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { ShowSettings(); };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            uint modExit = (cfg.ExitCtrl ? (uint)MOD_CONTROL : 0) | (cfg.ExitAlt ? (uint)MOD_ALT : 0);
            if (!RegisterHotKey(Handle, ID_TOGGLE, 0, (uint)cfg.KeyToggle)) Warn("toggle hotkey busy");
            if (!RegisterHotKey(Handle, ID_COLOR, 0, (uint)cfg.KeyColor)) Warn("color hotkey busy");
            if (!RegisterHotKey(Handle, ID_RELOAD, 0, (uint)cfg.KeyReload)) Warn("reload hotkey busy");
            if (!RegisterHotKey(Handle, ID_CANVAS, 0, (uint)cfg.KeyCanvas)) Warn("canvas hotkey busy");
            if (!RegisterHotKey(Handle, ID_SETTINGS, 0, (uint)cfg.KeySettings)) Warn("settings hotkey busy");
            if (!RegisterHotKey(Handle, ID_EXIT, modExit, (uint)cfg.KeyExit)) Warn("exit hotkey busy");
        }

        void Warn(string msg)
        {
            if (tray != null) tray.ShowBalloonTip(4000, "Crosshair Overlay", msg, ToolTipIcon.Warning);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            UnregisterHotKey(Handle, ID_TOGGLE);
            UnregisterHotKey(Handle, ID_COLOR);
            UnregisterHotKey(Handle, ID_RELOAD);
            UnregisterHotKey(Handle, ID_CANVAS);
            UnregisterHotKey(Handle, ID_SETTINGS);
            UnregisterHotKey(Handle, ID_EXIT);
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == ID_TOGGLE) Toggle();
                else if (id == ID_COLOR) NextColor();
                else if (id == ID_RELOAD) Reload();
                else if (id == ID_CANVAS) NextCanvas();
                else if (id == ID_SETTINGS) ShowSettings();
                else if (id == ID_EXIT) QuitApp();
            }
            base.WndProc(ref m);
        }

        void Toggle() { shown = !shown; Invalidate(); }

        void NextColor()
        {
            colorIndex = (colorIndex + 1) % cfg.Colors.Length;
            Redraw();
        }

        void NextCanvas()
        {
            if (cfg.CanvasPresets.Count == 0) return;
            canvasIndex = (canvasIndex + 1) % cfg.CanvasPresets.Count;
            Point p = cfg.CanvasPresets[canvasIndex];
            if (p.X == 0)
            {
                Screen[] all = Screen.AllScreens;
                Rectangle b = all[Math.Max(0, Math.Min(cfg.ScreenIndex, all.Length - 1))].Bounds;
                cfg.CanvasWidth = b.Width;
                cfg.CanvasHeight = b.Height;
            }
            else
            {
                cfg.CanvasWidth = p.X;
                cfg.CanvasHeight = p.Y;
            }
            if (tray != null) tray.Text = "Crosshair Overlay - canvas " + cfg.CanvasWidth + "x" + cfg.CanvasHeight;
            if (CanvasChanged != null) CanvasChanged(this, EventArgs.Empty);
            RefreshOverlay();
        }

        void Reload()
        {
            cfg = Config.Load(cfg.Path);
            baseCfg = new Config();
            baseCfg.CopyFrom(cfg);
            LoadProfiles();
            colorIndex = 0;
            canvasIndex = 0;
            if (CanvasChanged != null) CanvasChanged(this, EventArgs.Empty);
            RefreshOverlay();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (alphaMode) return;
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (alphaMode) { RenderLayered(); return; }
            Graphics g = e.Graphics;
            g.Clear(KEY_COLOR);
            if (!shown) return;
            RenderTo(g, ClientSize.Width, ClientSize.Height);
        }

        public Bitmap RenderLayeredImage()
        {
            int lw = ClientSize.Width, lh = ClientSize.Height;
            if (lw <= 0 || lh <= 0) { lw = Bounds.Width; lh = Bounds.Height; }
            Bitmap bmp = new Bitmap(lw, lh, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics lg = Graphics.FromImage(bmp))
            {
                lg.Clear(Color.Transparent);
                if (shown) RenderTo(lg, lw, lh);
            }
            return bmp;
        }

        Bitmap MakeGlow(Bitmap src, int radius)
        {
            int w = src.Width, h = src.Height;
            System.Drawing.Imaging.BitmapData bd = src.LockBits(new Rectangle(0, 0, w, h),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int len = w * h * 4;
            byte[] px = new byte[len];
            for (int y = 0; y < h; y++)
                System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(bd.Scan0, y * bd.Stride), px, y * w * 4, w * 4);
            src.UnlockBits(bd);

            int[] a = new int[w * h];
            for (int i = 0; i < w * h; i++) a[i] = px[i * 4 + 3];

            int[] pass = new int[w * h];
            int[] blur = new int[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int sum = 0, n = 0;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int xx = x + dx;
                        if (xx < 0 || xx >= w) continue;
                        sum += a[y * w + xx]; n++;
                    }
                    pass[y * w + x] = sum / n;
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int sum = 0, n = 0;
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int yy = y + dy;
                        if (yy < 0 || yy >= h) continue;
                        sum += pass[yy * w + x]; n++;
                    }
                    blur[y * w + x] = sum / n;
                }

            Bitmap dst = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Drawing.Imaging.BitmapData dd = dst.LockBits(new Rectangle(0, 0, w, h),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] outp = new byte[len];
            for (int i = 0; i < w * h; i++)
            {
                outp[i * 4] = cfg.Colors[0].B;
                outp[i * 4 + 1] = cfg.Colors[0].G;
                outp[i * 4 + 2] = cfg.Colors[0].R;
                outp[i * 4 + 3] = (byte)Math.Min(255, blur[i] * cfg.Glow / 100);
            }
            for (int y = 0; y < h; y++)
                System.Runtime.InteropServices.Marshal.Copy(outp, y * w * 4, IntPtr.Add(dd.Scan0, y * dd.Stride), w * 4);
            dst.UnlockBits(dd);
            return dst;
        }

        void RenderLayered()
        {
            int lw = ClientSize.Width, lh = ClientSize.Height;
            if (lw <= 0 || lh <= 0) return;
            using (Bitmap bmp = new Bitmap(lw, lh, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics lg = Graphics.FromImage(bmp))
                {
                    lg.Clear(Color.Transparent);
                    if (shown) RenderTo(lg, lw, lh);
                }
                if (shown && cfg.Glow > 0)
                {
                    using (Bitmap glow = MakeGlow(bmp, 2))
                    using (Bitmap fin = new Bitmap(lw, lh, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using (Graphics fg = Graphics.FromImage(fin))
                        {
                            fg.Clear(Color.Transparent);
                            fg.DrawImage(glow, 0, 0);
                            fg.DrawImage(bmp, 0, 0);
                        }
                        Premultiply(fin);
                        ApplyLayered(fin);
                    }
                    return;
                }
                Premultiply(bmp);
                ApplyLayered(bmp);
            }
        }

        static void Premultiply(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int len = Math.Abs(data.Stride) * bmp.Height;
            byte[] buf = new byte[len];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, len);
            for (int i = 0; i + 3 < len; i += 4)
            {
                int al = buf[i + 3];
                if (al == 0) { buf[i] = 0; buf[i + 1] = 0; buf[i + 2] = 0; }
                else if (al < 255)
                {
                    buf[i] = (byte)(buf[i] * al / 255);
                    buf[i + 1] = (byte)(buf[i + 1] * al / 255);
                    buf[i + 2] = (byte)(buf[i + 2] * al / 255);
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(buf, 0, data.Scan0, len);
            bmp.UnlockBits(data);
        }

        void ApplyLayered(Bitmap bmp)
        {
            int bw = bmp.Width, bh = bmp.Height;
            System.Drawing.Imaging.BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bw, bh),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int len = bw * bh * 4;
            byte[] px = new byte[len];
            try
            {
                for (int y = 0; y < bh; y++)
                {
                    IntPtr rowPtr = IntPtr.Add(bd.Scan0, y * bd.Stride);
                    System.Runtime.InteropServices.Marshal.Copy(rowPtr, px, y * bw * 4, bw * 4);
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }

            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);

            BITMAPINFOHEADER bmi = new BITMAPINFOHEADER();
            bmi.biSize = 40;
            bmi.biWidth = bw;
            bmi.biHeight = -bh;
            bmi.biPlanes = 1;
            bmi.biBitCount = 32;
            bmi.biCompression = 0;

            IntPtr bits;
            IntPtr hBmp = CreateDIBSection(memDc, ref bmi, 0, out bits, IntPtr.Zero, 0);
            if (hBmp == IntPtr.Zero || bits == IntPtr.Zero)
            {
                DeleteDC(memDc);
                ReleaseDC(IntPtr.Zero, screenDc);
                return;
            }
            System.Runtime.InteropServices.Marshal.Copy(px, 0, bits, len);

            IntPtr old = SelectObject(memDc, hBmp);
            SIZE size = new SIZE();
            size.cx = bw; size.cy = bh;
            POINT src = new POINT();
            POINT dst = new POINT();
            dst.X = Bounds.X; dst.Y = Bounds.Y;
            BLENDFUNCTION bf = new BLENDFUNCTION();
            bf.BlendOp = 0; bf.BlendFlags = 0; bf.SourceConstantAlpha = 255; bf.AlphaFormat = 1;
            UpdateLayeredWindow(Handle, screenDc, ref dst, ref size, memDc, ref src, 0, ref bf, 2);

            SelectObject(memDc, old);
            DeleteObject(hBmp);
            DeleteDC(memDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }

        static readonly int[] BAYER4 = new int[]
        {
            0, 8, 2, 10,
            12, 4, 14, 6,
            3, 11, 1, 9,
            15, 7, 13, 5
        };

        void DrawDithered(Graphics g, Bitmap src, int w, int h, double fracX, double fracY)
        {
            using (Bitmap tmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics gt = Graphics.FromImage(tmp))
                {
                    gt.Clear(Color.Transparent);
                    gt.InterpolationMode = cfg.Upscale == 1 ? InterpolationMode.HighQualityBilinear : InterpolationMode.NearestNeighbor;
                    gt.PixelOffsetMode = cfg.PixelSnap ? PixelOffsetMode.Half : PixelOffsetMode.HighQuality;
                    gt.DrawImage(src, new Rectangle(
                        (int)Math.Round(-fracX * sx), (int)Math.Round(-fracY * sy),
                        (int)Math.Round(src.Width * sx), (int)Math.Round(src.Height * sy)));
                }

                System.Drawing.Imaging.BitmapData bd = tmp.LockBits(new Rectangle(0, 0, w, h),
                    System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                int stride = bd.Stride;
                int len = Math.Abs(stride) * h;
                byte[] buf = new byte[len];
                try
                {
                    System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, buf, 0, len);
                    for (int y = 0; y < h; y++)
                    {
                        int row = stride > 0 ? y * stride : (h - 1 - y) * (-stride);
                        for (int x = 0; x < w; x++)
                        {
                            int i = row + x * 4;
                            int a = buf[i + 3];
                            bool keep;
                            if (a >= 200) keep = true;                       // solid body
                            else if (a >= 16) keep = ((x + y) & 1) == 0;     // antialiased rim -> 50% dots
                            else keep = false;                                // background
                            if (keep)
                            {
                                buf[i + 3] = 255;
                            }
                            else
                            {
                                buf[i] = KEY_COLOR.B;
                                buf[i + 1] = KEY_COLOR.G;
                                buf[i + 2] = KEY_COLOR.R;
                                buf[i + 3] = 255;
                            }
                        }
                    }
                    System.Runtime.InteropServices.Marshal.Copy(buf, 0, bd.Scan0, len);
                }
                finally
                {
                    tmp.UnlockBits(bd);
                }
                g.DrawImage(tmp, 0, 0);
            }
        }

        void RenderTo(Graphics g, int w, int h)
        {
            g.SmoothingMode = cfg.PixelSnap ? SmoothingMode.None : SmoothingMode.AntiAlias;

            int a = cfg.Alpha;
            Color col = cfg.Colors[colorIndex % cfg.Colors.Length];
            Color outCol = cfg.OutlineColor;

            double ccx = cw * cfg.AnchorX / 100.0;
            double ccy = ch * cfg.AnchorY / 100.0;

            // Render like a game would: draw the crosshair into a canvas-sized buffer
            // (1 unit = 1 canvas pixel), then upscale that buffer to the monitor.
            // This keeps the crosshair quantised to the canvas pixel grid instead of
            // being re-scaled analytically per shape.
            if (sx <= 0 || sy <= 0) return;
            double srcXf = (Bounds.X - oxAbs) / sx;
            double srcYf = (Bounds.Y - oyAbs) / sy;
            // snap the source window to whole canvas pixels so the upscaled blocks
            // stay aligned with the canvas grid (otherwise the phase drifts)
            int srcX = (int)Math.Floor(srcXf);
            int srcY = (int)Math.Floor(srcYf);
            double fracX = srcXf - srcX, fracY = srcYf - srcY;
            int sw = (int)Math.Ceiling(w / sx + fracX) + 1;
            int sh = (int)Math.Ceiling(h / sy + fracY) + 1;

            using (Bitmap bmp = new Bitmap(sw, sh, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics gb = Graphics.FromImage(bmp))
                {
                    gb.Clear(alphaMode ? Color.Transparent : KEY_COLOR);
                    gb.SmoothingMode = cfg.PixelSnap ? SmoothingMode.None : SmoothingMode.AntiAlias;
                    gb.PixelOffsetMode = PixelOffsetMode.Half;
                    CrosshairDraw.Render(gb, cfg, ccx - srcX, ccy - srcY, 1.0, 1.0, a, col, outCol);
                }
                if (cfg.SoftEdge && !alphaMode)
                {
                    DrawDithered(g, bmp, w, h, fracX, fracY);
                }
                else
                {
                    g.InterpolationMode = cfg.Upscale == 1 ? InterpolationMode.HighQualityBilinear : InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = cfg.PixelSnap ? PixelOffsetMode.Half : PixelOffsetMode.HighQuality;
                    g.DrawImage(bmp, new Rectangle(
                        (int)Math.Round(-fracX * sx), (int)Math.Round(-fracY * sy),
                        (int)Math.Round(sw * sx), (int)Math.Round(sh * sy)));
                }
            }
        }

    }

    static class Loc
    {
        public static readonly string[] Keys =
        {
            "title", "canvas_grp", "preset", "w", "h", "mode", "snap", "upscale", "nearest", "bilinear",
            "anchor", "alpha", "cross_grp", "shape", "color", "pick", "armx", "army", "gapx",
            "thickx", "thicky", "gapy", "outline", "sides", "opacity", "sync", "softedge", "glow",
            "presets_grp", "name", "save", "gallery", "saved", "load", "del",
            "savecfg", "savegame", "close", "exit", "status", "language",
            "tray_settings", "tray_gallery", "tray_show", "tray_color", "tray_canvas", "tray_reload", "tray_exit",
            "gallery_title", "gallery_save", "gallery_close", "gallery_hint", "gallery_new"
        };

        public static readonly string[] Names =
        {
            "简体中文", "English", "Русский", "Português (BR)", "Español", "日本語", "한국어", "Türkçe",
            "Polski", "Deutsch", "Français", "Tiếng Việt"
        };

        // order must match Names / Keys
        static readonly string[][] Table =
        {
            // zh-CN
            new string[]
            {
                "准星设置", "画布（准星的坐标系）", "预设", "宽", "高", "模式", "像素对齐", "放大", "最近邻", "双线性",
                "锚点", "逐像素透明（重启生效；出现黑块就取消勾选）", "准星（单位：画布像素）", "形状", "颜色", "选择...",
                "横臂", "竖臂", "间隙X", "线宽X", "线宽Y", "间隙Y", "描边", "只描两侧", "不透明度", "横竖等长",
                "柔和边缘（抖动模拟半透明）", "光晕", "准星存档（自定义名字保存/载入）", "名称", "保存", "存档库...",
                "已存", "载入", "删除", "保存配置 (Enter)", "存为当前游戏", "关闭 (Esc)", "退出程序",
                "F6 打开本窗口 / F7 切画布 / F8 开关准星 / Ctrl+Alt+Q 退出", "语言",
                "设置", "准星存档库", "显示/隐藏", "换颜色", "换画布", "重载配置", "退出程序",
                "准星存档库", "保存当前准星为存档", "关闭", "点卡片载入 / 右键卡片删除", "新准星"
            },
            // English
            new string[]
            {
                "Crosshair Settings", "Canvas (coordinate space)", "Preset", "W", "H", "Mode", "Pixel snap", "Upscale",
                "Nearest", "Bilinear", "Anchor", "Per-pixel alpha (restart; uncheck on a black box)",
                "Crosshair (canvas px)", "Shape", "Color", "Pick...", "Arm X", "Arm Y", "Gap X",
                "Line X", "Line Y", "Gap Y", "Outline", "Sides only", "Opacity", "Sync arms",
                "Soft edge (dither)", "Glow", "Crosshair presets (save / load by name)", "Name", "Save", "Library...",
                "Saved", "Load", "Delete", "Save config (Enter)", "Save for this game", "Close (Esc)", "Quit",
                "F6 settings / F7 canvas / F8 show-hide / Ctrl+Alt+Q quit", "Language",
                "Settings", "Preset library", "Show / hide", "Next color", "Next canvas", "Reload config", "Quit",
                "Crosshair presets", "Save current crosshair as preset", "Close", "Click a card to load / right-click to delete", "New crosshair"
            },
            // Русский
            new string[]
            {
                "Настройки прицела", "Холст (координаты)", "Пресет", "Ш", "В", "Режим", "Пиксели", "Масш.",
                "Соседние", "Билинейный", "Якорь", "Попиксельная прозрачность (перезапуск; снимите при чёрном квадрате)",
                "Прицел (пикс. холста)", "Форма", "Цвет", "Выбрать...", "Плечо X", "Плечо Y", "Зазор X",
                "Толщ. X", "Толщ. Y", "Зазор Y", "Обводка", "Бока", "Альфа", "Равные",
                "Мягкий", "Свет", "Пресеты прицела (сохранить / загрузить)", "Имя", "Сохранить", "Библиотека...",
                "Сохранено", "Загрузить", "Удалить", "Сохранить (Enter)", "Для игры", "Закрыть", "Выход",
                "F6 настройки / F7 холст / F8 показать / Ctrl+Alt+Q выход", "Язык",
                "Настройки", "Библиотека пресетов", "Показать / скрыть", "Следующий цвет", "Следующий холст", "Перечитать конфиг", "Выход",
                "Пресеты прицела", "Сохранить текущий прицел", "Закрыть", "Клик — загрузить / правый клик — удалить", "Новый прицел"
            },
            // Português (BR)
            new string[]
            {
                "Configurações da mira", "Tela (espaço de coordenadas)", "Predefinição", "L", "A", "Modo", "Alinhar pixels", "Ampliação",
                "Vizinho", "Bilinear", "Âncora", "Alfa por pixel (reinicie; desmarque se ficar um quadrado preto)",
                "Mira (px da tela)", "Forma", "Cor", "Escolher...", "Braço X", "Braço Y", "Vão X",
                "Linha X", "Linha Y", "Vão Y", "Contorno", "Só as laterais", "Opacidade", "Braços iguais",
                "Borda suave (dither)", "Brilho", "Predefinições de mira (salvar / carregar)", "Nome", "Salvar", "Biblioteca...",
                "Salvo", "Carregar", "Excluir", "Salvar (Enter)", "Para este jogo", "Fechar (Esc)", "Sair",
                "F6 config / F7 tela / F8 mostrar / Ctrl+Alt+Q sair", "Idioma",
                "Configurações", "Biblioteca de predefinições", "Mostrar / ocultar", "Próxima cor", "Próxima tela", "Recarregar config", "Sair",
                "Predefinições de mira", "Salvar mira atual como predefinição", "Fechar", "Clique para carregar / botão direito para excluir", "Nova mira"
            },
            // Español
            new string[]
            {
                "Ajustes de mira", "Lienzo (espacio de coordenadas)", "Preajuste", "An", "Al", "Modo", "Alinear píxeles", "Escalado",
                "Vecino", "Bilineal", "Ancla", "Alfa por píxel (reinicia; desmarca si sale un cuadro negro)",
                "Mira (px del lienzo)", "Forma", "Color", "Elegir...", "Brazo X", "Brazo Y", "Hueco X",
                "Grosor X", "Grosor Y", "Hueco Y", "Contorno", "Solo lados", "Opacidad", "Brazos iguales",
                "Borde suave (dither)", "Resplandor", "Preajustes de mira (guardar / cargar)", "Nombre", "Guardar", "Biblioteca...",
                "Guardado", "Cargar", "Borrar", "Guardar (Enter)", "Para este juego", "Cerrar (Esc)", "Salir",
                "F6 ajustes / F7 lienzo / F8 mostrar / Ctrl+Alt+Q salir", "Idioma",
                "Ajustes", "Biblioteca de preajustes", "Mostrar / ocultar", "Siguiente color", "Siguiente lienzo", "Recargar config", "Salir",
                "Preajustes de mira", "Guardar mira actual", "Cerrar", "Clic para cargar / clic derecho para borrar", "Nueva mira"
            },
            // 日本語
            new string[]
            {
                "クロスヘア設定", "キャンバス（座標系）", "プリセット", "幅", "高", "モード", "ピクセル整列", "拡大",
                "最近傍", "バイリニア", "アンカー", "ピクセル透過（再起動が必要／黒い四角が出たら解除）",
                "クロスヘア（キャンバス px）", "形状", "色", "選択...", "アーム X", "アーム Y", "隙間 X",
                "線幅 X", "線幅 Y", "隙間 Y", "縁取り", "左右のみ", "不透明度", "縦横同じ",
                "ソフトエッジ（ディザ）", "グロー", "クロスヘア保存（名前を付けて保存／読込）", "名前", "保存", "ライブラリ...",
                "保存済み", "読込", "削除", "設定を保存 (Enter)", "このゲーム用に保存", "閉じる (Esc)", "終了",
                "F6 設定 / F7 キャンバス / F8 表示切替 / Ctrl+Alt+Q 終了", "言語",
                "設定", "プリセットライブラリ", "表示／非表示", "色を切替", "キャンバスを切替", "設定を再読込", "終了",
                "クロスヘア プリセット", "現在のクロスヘアを保存", "閉じる", "クリックで読込／右クリックで削除", "新しいクロスヘア"
            },
            // 한국어
            new string[]
            {
                "크로스헤어 설정", "캔버스 (좌표계)", "프리셋", "너비", "높이", "모드", "픽셀 정렬", "확대",
                "최근접", "양선형", "앵커", "픽셀 투명(재시작 필요, 검은 사각형이 보이면 해제)",
                "크로스헤어 (캔버스 px)", "모양", "색상", "선택...", "가로팔", "세로팔", "간격 X",
                "선폭 X", "선폭 Y", "간격 Y", "테두리", "양옆만", "불투명도", "가로세로 동일",
                "부드러운 가장자리(디더)", "글로우", "크로스헤어 저장 (이름으로 저장/불러오기)", "이름", "저장", "라이브러리...",
                "저장됨", "불러오기", "삭제", "설정 저장 (Enter)", "이 게임용으로 저장", "닫기 (Esc)", "종료",
                "F6 설정 / F7 캔버스 / F8 표시 전환 / Ctrl+Alt+Q 종료", "언어",
                "설정", "프리셋 라이브러리", "표시/숨기기", "색상 변경", "캔버스 변경", "설정 다시 읽기", "종료",
                "크로스헤어 프리셋", "현재 크로스헤어 저장", "닫기", "클릭하여 불러오기 / 우클릭하여 삭제", "새 크로스헤어"
            },
            // Türkçe
            new string[]
            {
                "Nişangah ayarları", "Tuval (koordinat alanı)", "Hazır ayar", "G", "Y", "Mod", "Piksel hizalama", "Ölçekleme",
                "En yakın", "Çift doğrusal", "Çapa", "Piksel saydamlığı (yeniden başlat; siyah kare olursa kapat)",
                "Nişangah (tuval px)", "Şekil", "Renk", "Seç...", "Kol X", "Kol Y", "Boşluk X",
                "Kalınlık X", "Kalınlık Y", "Boşluk Y", "Kenar çizgisi", "Sadece yanlar", "Opaklık", "Kollar eşit",
                "Yumuşak kenar (dither)", "Parıltı", "Nişangah kayıtları (adla kaydet / yükle)", "Ad", "Kaydet", "Kitaplık...",
                "Kayıtlı", "Yükle", "Sil", "Kaydet (Enter)", "Bu oyun için", "Kapat (Esc)", "Çıkış",
                "F6 ayarlar / F7 tuval / F8 göster / Ctrl+Alt+Q çıkış", "Dil",
                "Ayarlar", "Hazır ayar kitaplığı", "Göster / gizle", "Sonraki renk", "Sonraki tuval", "Ayarı yenile", "Çıkış",
                "Nişangah kayıtları", "Mevcut nişangahı kaydet", "Kapat", "Yüklemek için tıkla / silmek için sağ tıkla", "Yeni nişangah"
            },
            // Polski
            new string[]
            {
                "Ustawienia celownika", "Płótno (układ współrzędnych)", "Preset", "Szer.", "Wys.", "Tryb", "Wyrównanie pikseli", "Skalowanie",
                "Najbliższy", "Dwuliniowe", "Kotwica", "Przezroczystość alfa (restart; odznacz przy czarnym kwadracie)",
                "Celownik (px płótna)", "Kształt", "Kolor", "Wybierz...", "Ramię X", "Ramię Y", "Odstęp X",
                "Grubość X", "Grubość Y", "Odstęp Y", "Obrys", "Tylko boki", "Krycie", "Równe ramiona",
                "Miękka krawędź (dither)", "Poświata", "Presety celownika (zapisz / wczytaj)", "Nazwa", "Zapisz", "Biblioteka...",
                "Zapisane", "Wczytaj", "Usuń", "Zapisz (Enter)", "Dla tej gry", "Zamknij (Esc)", "Zakończ",
                "F6 ustawienia / F7 płótno / F8 pokaż / Ctrl+Alt+Q wyjście", "Język",
                "Ustawienia", "Biblioteka presetów", "Pokaż / ukryj", "Następny kolor", "Następne płótno", "Wczytaj konfigurację", "Zakończ",
                "Presety celownika", "Zapisz bieżący celownik", "Zamknij", "Kliknij, aby wczytać / prawy przycisk usuwa", "Nowy celownik"
            },
            // Deutsch
            new string[]
            {
                "Fadenkreuz-Einstellungen", "Leinwand (Koordinaten)", "Preset", "B", "H", "Modus", "Pixel", "Skal.",
                "Nächster", "Bilinear", "Anker", "Pixel-Alpha (Neustart; bei schwarzem Kasten abwählen)",
                "Fadenkreuz (Leinwand-px)", "Form", "Farbe", "Wählen...", "Arm X", "Arm Y", "Gap X",
                "Dicke X", "Dicke Y", "Gap Y", "Kontur", "Seiten", "Alpha", "Gleich",
                "Weich (Dither)", "Glow", "Fadenkreuz-Presets (speichern / laden)", "Name", "Speichern", "Bibliothek...",
                "Gespeichert", "Laden", "Löschen", "Speichern (Enter)", "Für Spiel speichern", "Schließen (Esc)", "Beenden",
                "F6 Einstellungen / F7 Leinwand / F8 zeigen / Ctrl+Alt+Q beenden", "Sprache",
                "Einstellungen", "Preset-Bibliothek", "Zeigen / verbergen", "Nächste Farbe", "Nächste Leinwand", "Konfiguration neu laden", "Beenden",
                "Fadenkreuz-Presets", "Aktuelles Fadenkreuz speichern", "Schließen", "Klicken zum Laden / Rechtsklick zum Löschen", "Neues Fadenkreuz"
            },
            // Français
            new string[]
            {
                "Réglages du réticule", "Canevas (repère)", "Préréglage", "L", "H", "Mode", "Alignement pixel", "Agrandissement",
                "Voisin", "Bilinéaire", "Ancrage", "Alpha par pixel (redémarrage ; décocher si carré noir)",
                "Réticule (px canevas)", "Forme", "Couleur", "Choisir...", "Bras X", "Bras Y", "Écart X",
                "Épaisseur X", "Épaisseur Y", "Écart Y", "Contour", "Côtés seuls", "Opacité", "Bras égaux",
                "Bord doux (dither)", "Halo", "Préréglages de réticule (enregistrer / charger)", "Nom", "Enregistrer", "Bibliothèque...",
                "Enregistré", "Charger", "Supprimer", "Enregistrer (Entrée)", "Pour ce jeu", "Fermer (Échap)", "Quitter",
                "F6 réglages / F7 canevas / F8 afficher / Ctrl+Alt+Q quitter", "Langue",
                "Réglages", "Bibliothèque de préréglages", "Afficher / masquer", "Couleur suivante", "Canevas suivant", "Recharger la config", "Quitter",
                "Préréglages de réticule", "Enregistrer le réticule actuel", "Fermer", "Clic pour charger / clic droit pour supprimer", "Nouveau réticule"
            },
            // Tiếng Việt
            new string[]
            {
                "Cài đặt tâm ngắm", "Khung vẽ (hệ tọa độ)", "Mẫu", "Rộng", "Cao", "Chế độ", "Căn pixel", "Phóng to",
                "Gần nhất", "Song tuyến", "Điểm neo", "Độ trong suốt theo điểm ảnh (khởi động lại; bỏ chọn nếu có ô đen)",
                "Tâm ngắm (px khung vẽ)", "Hình dạng", "Màu", "Chọn...", "Cánh X", "Cánh Y", "Khe X",
                "Độ dày X", "Độ dày Y", "Khe Y", "Viền", "Chỉ hai bên", "Độ mờ", "Cánh bằng nhau",
                "Cạnh mềm (dither)", "Quầng sáng", "Mẫu tâm ngắm (lưu / tải theo tên)", "Tên", "Lưu", "Thư viện...",
                "Đã lưu", "Tải", "Xóa", "Lưu (Enter)", "Cho game này", "Đóng (Esc)", "Thoát",
                "F6 cài đặt / F7 khung vẽ / F8 ẩn hiện / Ctrl+Alt+Q thoát", "Ngôn ngữ",
                "Cài đặt", "Thư viện mẫu", "Hiện / ẩn", "Màu tiếp theo", "Khung vẽ tiếp theo", "Tải lại cấu hình", "Thoát",
                "Mẫu tâm ngắm", "Lưu tâm ngắm hiện tại", "Đóng", "Nhấn để tải / chuột phải để xóa", "Tâm ngắm mới"
            }
        };

        public static int Current = 0;

        public static bool TableOk()
        {
            for (int l = 0; l < Table.Length; l++)
            {
                if (Table[l].Length != Keys.Length) return false;
                for (int k = 0; k < Keys.Length; k++)
                    if (string.IsNullOrEmpty(Table[l][k])) return false;
            }
            return true;
        }

        public static string T(string key)
        {
            int i = Array.IndexOf(Keys, key);
            if (i < 0) return key;
            string[] row = Table[Current];
            if (i < row.Length) return row[i];
            return Table[1][i];
        }

        public static int KeyIndex(string zhText)
        {
            if (string.IsNullOrEmpty(zhText)) return -1;
            return Array.IndexOf(Table[0], zhText);
        }

        public static int LangFromCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return -1;
            code = code.ToLowerInvariant();
            if (code.StartsWith("zh")) return 0;
            if (code.StartsWith("en")) return 1;
            if (code.StartsWith("ru")) return 2;
            if (code.StartsWith("pt")) return 3;
            if (code.StartsWith("es")) return 4;
            if (code.StartsWith("ja")) return 5;
            if (code.StartsWith("ko")) return 6;
            if (code.StartsWith("tr")) return 7;
            if (code.StartsWith("pl")) return 8;
            if (code.StartsWith("de")) return 9;
            if (code.StartsWith("fr")) return 10;
            if (code.StartsWith("vi")) return 11;
            return -1;
        }

        public static string CodeFor(int lang)
        {
            string[] codes = { "zh", "en", "ru", "pt", "es", "ja", "ko", "tr", "pl", "de", "fr", "vi" };
            if (lang < 0 || lang >= codes.Length) return "en";
            return codes[lang];
        }

        public static int AutoDetect()
        {
            int i = LangFromCode(System.Globalization.CultureInfo.CurrentUICulture.Name);
            return i < 0 ? 1 : i;
        }
    }

    sealed class SettingsForm : Form
    {
        readonly OverlayForm owner;
        EventHandler canvasHandler;
        bool loading;

        ComboBox cbPreset, cbFit, cbShape, cbUpscale, cbLang;
        NumericUpDown nW, nH, nAx, nAy, nSx, nSy, nTx, nTy, nGx, nGy, nOut, nAlpha, nGlow;
        CheckBox chkSnap, chkAlphaMode, chkSync, chkOutlineSides, chkSoftEdge;
        Button btnColor, btnApply, btnClose, btnExit, btnProfile, btnSavePreset, btnLoadPreset, btnDeletePreset, btnGallery;
        ComboBox cbPresetList;
        TextBox txtName;
        Panel pColor;
        TextBox txtHex;
        Label lblStatus;

        public SettingsForm(OverlayForm o)
        {
            owner = o;
            Text = "准星设置 - Crosshair Overlay";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ClientSize = new Size(700, 596);
            Rectangle scr = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(scr.X + 40, scr.Y + (scr.Height - Height) / 2);

            BuildUi();
            LoadFromConfig();
            RefreshPresetList();
            LocalizeTree(this);
            Text = Loc.T("title");
            cbLang.SelectedIndex = Loc.Current;

            canvasHandler = delegate { LoadCanvasFields(); };
            owner.CanvasChanged += canvasHandler;
            FormClosed += delegate { owner.CanvasChanged -= canvasHandler; };
        }

        public void ReloadUi()
        {
            if (IsDisposed) return;
            LoadFromConfig();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            owner.PauseTopMost(Visible);
        }

        void BuildUi()
        {
            GroupBox g1 = new GroupBox();
            g1.Text = "画布（准星的坐标系）";
            g1.SetBounds(10, 8, 680, 152);
            Controls.Add(g1);

            Lab(g1, 10, 28, "预设");
            cbPreset = Combo(g1, 120, 24, 150);
            Lab(g1, 290, 28, "宽");
            nW = Num(g1, 400, 24, 60, 8, 16384, 0, 1);
            Lab(g1, 480, 28, "高");
            nH = Num(g1, 590, 24, 60, 8, 16384, 0, 1);

            Lab(g1, 10, 60, "模式");
            cbFit = Combo(g1, 120, 56, 110);
            cbFit.Items.AddRange(new object[] { "stretch", "fit", "native" });
            Lab(g1, 250, 60, "像素对齐");
            chkSnap = new CheckBox();
            chkSnap.SetBounds(360, 58, 60, 22);
            chkSnap.CheckedChanged += OnUiChanged;
            g1.Controls.Add(chkSnap);
            Lab(g1, 450, 60, "放大");
            cbUpscale = Combo(g1, 560, 56, 90);
            cbUpscale.Items.AddRange(new object[] { "最近邻", "双线性" });

            Lab(g1, 10, 92, "锚点");
            nAx = Num(g1, 120, 88, 55, 0, 100, 1, 1);
            Lab(g1, 190, 92, "X%");
            nAy = Num(g1, 240, 88, 55, 0, 100, 1, 1);
            Lab(g1, 310, 92, "Y%");

            chkAlphaMode = new CheckBox();
            chkAlphaMode.Text = "逐像素透明（重启生效；出现黑块就取消勾选）";
            chkAlphaMode.SetBounds(10, 120, 660, 22);
            chkAlphaMode.CheckedChanged += OnUiChanged;
            g1.Controls.Add(chkAlphaMode);

            GroupBox g2 = new GroupBox();
            g2.Text = "准星（单位：画布像素）";
            g2.SetBounds(10, 168, 680, 192);
            Controls.Add(g2);

            Lab(g2, 10, 28, "形状");
            cbShape = Combo(g2, 120, 24, 110);
            cbShape.Items.AddRange(new object[] { "cross", "dot", "circle", "x", "t" });

            Lab(g2, 250, 28, "颜色");
            btnColor = new Button();
            btnColor.Text = "选择...";
            btnColor.SetBounds(360, 23, 80, 26);
            btnColor.Click += delegate
            {
                using (ColorDialog dlg = new ColorDialog())
                {
                    dlg.FullOpen = true;
                    dlg.Color = owner.Cfg.Colors[0];
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        txtHex.Text = Config.Hex(dlg.Color);
                        OnUiChanged(this, EventArgs.Empty);
                    }
                }
            };
            g2.Controls.Add(btnColor);
            pColor = new Panel();
            pColor.SetBounds(450, 24, 24, 24);
            pColor.BorderStyle = BorderStyle.FixedSingle;
            g2.Controls.Add(pColor);
            txtHex = new TextBox();
            txtHex.SetBounds(480, 25, 90, 22);
            txtHex.TextChanged += OnUiChanged;
            g2.Controls.Add(txtHex);

            Lab(g2, 10, 62, "横臂");
            nSx = Num(g2, 120, 58, 56, 0.5, 200, 1, 0.5m);
            Lab(g2, 200, 62, "竖臂");
            nSy = Num(g2, 310, 58, 56, 0.5, 200, 1, 0.5m);
            Lab(g2, 390, 62, "间隙X");
            nGx = Num(g2, 500, 58, 56, 0, 200, 1, 0.5m);

            Lab(g2, 10, 94, "线宽X");
            nTx = Num(g2, 120, 90, 56, 0.5, 100, 1, 0.5m);
            Lab(g2, 200, 94, "线宽Y");
            nTy = Num(g2, 310, 90, 56, 0.5, 100, 1, 0.5m);
            Lab(g2, 390, 94, "间隙Y");
            nGy = Num(g2, 500, 90, 56, 0, 200, 1, 0.5m);

            Lab(g2, 10, 126, "描边");
            nOut = Num(g2, 120, 122, 56, 0, 20, 0, 1);
            chkOutlineSides = new CheckBox();
            chkOutlineSides.Text = "只描两侧";
            chkOutlineSides.SetBounds(200, 120, 110, 22);
            chkOutlineSides.CheckedChanged += OnUiChanged;
            g2.Controls.Add(chkOutlineSides);
            Lab(g2, 330, 126, "不透明度");
            nAlpha = Num(g2, 460, 122, 56, 0, 255, 0, 5);

            chkSync = new CheckBox();
            chkSync.Text = "横竖等长";
            chkSync.SetBounds(10, 154, 120, 22);
            chkSync.CheckedChanged += OnUiChanged;
            g2.Controls.Add(chkSync);

            chkSoftEdge = new CheckBox();
            chkSoftEdge.Text = "柔和边缘（抖动模拟半透明）";
            chkSoftEdge.SetBounds(140, 154, 240, 22);
            chkSoftEdge.CheckedChanged += OnUiChanged;
            g2.Controls.Add(chkSoftEdge);

            Lab(g2, 390, 154, "光晕");
            nGlow = Num(g2, 470, 152, 56, 0, 100, 0, 5);
            nGlow.ValueChanged += OnUiChanged;

            GroupBox g3 = new GroupBox();
            g3.Text = "准星存档（自定义名字保存/载入）";
            g3.SetBounds(10, 368, 680, 96);
            Controls.Add(g3);

            Lab(g3, 10, 22, "名称");
            txtName = new TextBox();
            txtName.SetBounds(120, 19, 180, 22);
            g3.Controls.Add(txtName);

            btnSavePreset = new Button();
            btnSavePreset.Text = "保存";
            btnSavePreset.SetBounds(310, 18, 80, 26);
            btnSavePreset.Click += delegate { SavePreset(); };
            g3.Controls.Add(btnSavePreset);

            btnGallery = new Button();
            btnGallery.Text = "存档库...";
            btnGallery.SetBounds(400, 18, 120, 26);
            btnGallery.Click += delegate { owner.ShowGallery(); };
            g3.Controls.Add(btnGallery);

            Lab(g3, 10, 58, "已存");
            cbPresetList = new ComboBox();
            cbPresetList.SetBounds(120, 55, 240, 22);
            cbPresetList.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPresetList.SelectedIndexChanged += delegate
            {
                if (cbPresetList.SelectedItem != null) txtName.Text = cbPresetList.SelectedItem.ToString();
            };
            g3.Controls.Add(cbPresetList);

            btnLoadPreset = new Button();
            btnLoadPreset.Text = "载入";
            btnLoadPreset.SetBounds(370, 54, 80, 26);
            btnLoadPreset.Click += delegate { LoadPreset(); };
            g3.Controls.Add(btnLoadPreset);

            btnDeletePreset = new Button();
            btnDeletePreset.Text = "删除";
            btnDeletePreset.SetBounds(460, 54, 80, 26);
            btnDeletePreset.Click += delegate { DeletePreset(); };
            g3.Controls.Add(btnDeletePreset);

            cbLang = new ComboBox();
            cbLang.SetBounds(10, 474, 200, 24);
            cbLang.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLang.Items.AddRange(Loc.Names);
            cbLang.SelectedIndexChanged += delegate
            {
                if (loading) return;
                if (cbLang.SelectedIndex < 0) return;
                Loc.Current = cbLang.SelectedIndex;
                owner.Cfg.Language = Loc.CodeFor(Loc.Current);
                Relocalize();
                owner.RestyleTray();
            };
            Controls.Add(cbLang);

            lblStatus = new Label();
            lblStatus.SetBounds(220, 477, 470, 20);
            lblStatus.Text = Loc.T("status");
            lblStatus.AutoEllipsis = true;
            Controls.Add(lblStatus);

            btnApply = new Button();
            btnApply.Text = "保存配置 (Enter)";
            btnApply.SetBounds(10, 508, 330, 34);
            btnApply.AutoEllipsis = true;
            btnApply.Click += delegate { ApplyAndSave(); };
            Controls.Add(btnApply);

            btnProfile = new Button();
            btnProfile.Text = "存为当前游戏";
            btnProfile.SetBounds(350, 508, 330, 34);
            btnProfile.AutoEllipsis = true;
            btnProfile.Click += delegate
            {
                PushToConfig();
                string proc = owner.SaveProfileForCurrentGame();
                lblStatus.Text = proc.Length > 0
                    ? ("OK: " + proc + ".exe")
                    : "no foreground game detected";
            };
            Controls.Add(btnProfile);

            btnClose = new Button();
            btnClose.Text = "关闭 (Esc)";
            btnClose.SetBounds(10, 550, 160, 34);
            btnClose.Click += delegate { Hide(); };
            Controls.Add(btnClose);

            btnExit = new Button();
            btnExit.Text = "退出程序";
            btnExit.SetBounds(180, 550, 160, 34);
            btnExit.Click += delegate { owner.QuitApp(); };
            Controls.Add(btnExit);

            AcceptButton = btnApply;
            KeyPreview = true;
            KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { Hide(); e.Handled = true; }
            };
        }


        Label Lab(Control p, int x, int y, string t)
        {
            Label l = new Label();
            l.Text = t;
            l.AutoSize = true;
            l.Location = new Point(x, y);
            p.Controls.Add(l);
            return l;
        }

        NumericUpDown Num(Control p, int x, int y, int w, double min, double max, int dec, decimal inc)
        {
            NumericUpDown n = new NumericUpDown();
            n.SetBounds(x, y, w, 22);
            n.DecimalPlaces = dec;
            n.Minimum = (decimal)min;
            n.Maximum = (decimal)max;
            n.Increment = inc;
            n.ValueChanged += OnUiChanged;
            p.Controls.Add(n);
            return n;
        }

        ComboBox Combo(Control p, int x, int y, int w)
        {
            ComboBox c = new ComboBox();
            c.SetBounds(x, y, w, 22);
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.SelectedIndexChanged += OnUiChanged;
            p.Controls.Add(c);
            return c;
        }

        void LoadCanvasFields()
        {
            if (IsDisposed) return;
            loading = true;
            nW.Value = Clamp(owner.Cfg.CanvasWidth, nW.Minimum, nW.Maximum);
            nH.Value = Clamp(owner.Cfg.CanvasHeight, nH.Minimum, nH.Maximum);
            loading = false;
        }

        void LoadFromConfig()
        {
            loading = true;
            Config c = owner.Cfg;

            cbPreset.Items.Clear();
            cbPreset.Items.Add(Loc.Current == 0 ? "自定义" : "Custom");
            foreach (Point p in c.CanvasPresets)
                cbPreset.Items.Add(p.X == 0 ? "native" : p.X + "x" + p.Y);
            cbPreset.SelectedIndex = 0;

            nW.Value = Clamp(c.CanvasWidth, nW.Minimum, nW.Maximum);
            nH.Value = Clamp(c.CanvasHeight, nH.Minimum, nH.Maximum);
            cbFit.SelectedItem = c.CanvasFit;
            if (cbFit.SelectedIndex < 0) cbFit.SelectedIndex = 0;
            chkSnap.Checked = c.PixelSnap;
            chkAlphaMode.Checked = c.AlphaMode == 1;
            nAx.Value = (decimal)c.AnchorX;
            nAy.Value = (decimal)c.AnchorY;

            cbShape.SelectedItem = c.Shape;
            if (cbShape.SelectedIndex < 0) cbShape.SelectedIndex = 0;

            txtHex.Text = Config.Hex(c.Colors[0]);
            pColor.BackColor = c.Colors[0];

            nSx.Value = (decimal)c.SizeX;
            nSy.Value = (decimal)c.SizeY;
            nTx.Value = (decimal)c.ThicknessX;
            nTy.Value = (decimal)c.ThicknessY;
            nGx.Value = (decimal)c.GapX;
            nGy.Value = (decimal)c.GapY;
            nOut.Value = c.Outline;
            nAlpha.Value = c.Alpha;
            chkSync.Checked = c.SyncArms;
            chkOutlineSides.Checked = c.OutlineMode == 1;
            chkSoftEdge.Checked = c.SoftEdge;
            cbUpscale.SelectedIndex = c.Upscale == 1 ? 1 : 0;
            nGlow.Value = Math.Max(0, Math.Min(100, c.Glow));
            UpdateSyncUi();
            loading = false;
        }

        void UpdateSyncUi()
        {
            bool on = chkSync.Checked;
            nSy.Enabled = !on;
            nTy.Enabled = !on;
            nGy.Enabled = !on;
        }

        static decimal Clamp(int v, decimal min, decimal max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        void OnUiChanged(object sender, EventArgs e)
        {
            if (loading) return;
            if (sender == cbPreset)
            {
                int i = cbPreset.SelectedIndex - 1;
                if (i >= 0 && i < owner.Cfg.CanvasPresets.Count)
                {
                    Point p = owner.Cfg.CanvasPresets[i];
                    loading = true;
                    if (p.X == 0)
                    {
                        Rectangle b = Screen.PrimaryScreen.Bounds;
                        nW.Value = Clamp(b.Width, nW.Minimum, nW.Maximum);
                        nH.Value = Clamp(b.Height, nH.Minimum, nH.Maximum);
                    }
                    else
                    {
                        nW.Value = Clamp(p.X, nW.Minimum, nW.Maximum);
                        nH.Value = Clamp(p.Y, nH.Minimum, nH.Maximum);
                    }
                    loading = false;
                }
            }
            if (chkSync.Checked)
            {
                loading = true;
                nSy.Value = nSx.Value;
                nTy.Value = nTx.Value;
                nGy.Value = nGx.Value;
                loading = false;
            }
            UpdateSyncUi();
            PushToConfig();
            pColor.BackColor = ParseHexSafe(txtHex.Text, pColor.BackColor);
            owner.RefreshOverlay();
        }

        Color ParseHexSafe(string s, Color dflt)
        {
            Color c = Config.ParseColor(s, Color.Empty);
            return c == Color.Empty ? dflt : c;
        }

        void PushToConfig()
        {
            Config c = owner.Cfg;
            c.CanvasWidth = (int)nW.Value;
            c.CanvasHeight = (int)nH.Value;
            if (cbFit.SelectedItem != null) c.CanvasFit = cbFit.SelectedItem.ToString();
            c.PixelSnap = chkSnap.Checked;
            c.AlphaMode = chkAlphaMode.Checked ? 1 : 0;
            c.AnchorX = (double)nAx.Value;
            c.AnchorY = (double)nAy.Value;
            if (cbShape.SelectedItem != null) c.Shape = cbShape.SelectedItem.ToString();

            Color col = Config.ParseColor(txtHex.Text, Color.Empty);
            if (col != Color.Empty)
            {
                if (c.Colors.Length == 0) c.Colors = new Color[] { col };
                else c.Colors[0] = col;
            }

            c.SizeX = (double)nSx.Value;
            c.SizeY = (double)nSy.Value;
            c.ThicknessX = (double)nTx.Value;
            c.ThicknessY = (double)nTy.Value;
            c.GapX = (double)nGx.Value;
            c.GapY = (double)nGy.Value;
            c.Outline = (int)nOut.Value;
            c.Alpha = (int)nAlpha.Value;
            c.SyncArms = chkSync.Checked;
            c.OutlineMode = chkOutlineSides.Checked ? 1 : 0;
            c.SoftEdge = chkSoftEdge.Checked;
            c.Upscale = cbUpscale.SelectedIndex == 1 ? 1 : 0;
            c.Glow = (int)nGlow.Value;
            if (c.SyncArms)
            {
                c.SizeY = c.SizeX;
                c.ThicknessY = c.ThicknessX;
                c.GapY = c.GapX;
            }
        }

        void RefreshPresetList()
        {
            string path = PresetStore.PathFor(owner.Cfg.Path);
            object sel = cbPresetList.SelectedItem;
            cbPresetList.Items.Clear();
            foreach (string n in PresetStore.Names(path)) cbPresetList.Items.Add(n);
            if (sel != null)
            {
                int i = cbPresetList.Items.IndexOf(sel);
                if (i >= 0) cbPresetList.SelectedIndex = i;
            }
        }

        void SavePreset()
        {
            string name = PresetStore.CleanName(txtName.Text);
            if (name.Length == 0) { lblStatus.Text = "先给准星起个名字，再点保存"; return; }
            PushToConfig();
            PresetStore.Save(PresetStore.PathFor(owner.Cfg.Path), name, owner.Cfg);
            RefreshPresetList();
            cbPresetList.SelectedItem = name;
            lblStatus.Text = "已保存准星存档：" + name + "（presets.ini）";
        }

        void LoadPreset()
        {
            string name = cbPresetList.SelectedItem as string;
            if (name == null || name.Length == 0) name = PresetStore.CleanName(txtName.Text);
            if (name.Length == 0) { lblStatus.Text = "先选一个已存档的准星"; return; }
            if (!PresetStore.Apply(PresetStore.PathFor(owner.Cfg.Path), name, owner.Cfg))
            {
                lblStatus.Text = "载入失败：找不到存档 " + name;
                return;
            }
            LoadFromConfig();
            owner.RefreshOverlay();
            lblStatus.Text = "已载入：" + name;
        }

        void DeletePreset()
        {
            string name = cbPresetList.SelectedItem as string;
            if (name == null || name.Length == 0) { lblStatus.Text = "先选一个要删除的存档"; return; }
            PresetStore.Delete(PresetStore.PathFor(owner.Cfg.Path), name);
            RefreshPresetList();
            lblStatus.Text = "已删除：" + name;
        }

        public void Relocalize()
        {
            loading = true;
            Text = Loc.T("title");
            LocalizeTree(this);
            int up = cbUpscale.SelectedIndex;
            cbUpscale.Items.Clear();
            cbUpscale.Items.Add(Loc.T("nearest"));
            cbUpscale.Items.Add(Loc.T("bilinear"));
            cbUpscale.SelectedIndex = up < 0 ? 0 : up;
            lblStatus.Text = Loc.T("status");
            cbLang.SelectedIndex = Loc.Current;
            loading = false;
        }

        void LocalizeTree(Control root)
        {
            foreach (Control c in root.Controls)
            {
                int idx = -1;
                if (c.Tag is int) idx = (int)c.Tag;
                else
                {
                    idx = Loc.KeyIndex(c.Text);
                    if (idx >= 0) c.Tag = idx;
                }
                if (idx >= 0) c.Text = Loc.T(Loc.Keys[idx]);
                LocalizeTree(c);
            }
        }

        void ApplyAndSave()
        {
            PushToConfig();
            try
            {
                owner.Cfg.Save(owner.Cfg.Path);
                lblStatus.Text = "已保存到 " + owner.Cfg.Path + "  (" + DateTime.Now.ToString("HH:mm:ss") + ")";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "保存失败: " + ex.Message;
            }
            owner.RefreshOverlay();
        }
    }

    sealed class PresetGalleryForm : Form
    {
        readonly OverlayForm owner;
        FlowLayoutPanel flow;
        TextBox txtName;
        Label lblStatus;

        public PresetGalleryForm(OverlayForm o)
        {
            owner = o;
            Text = "准星存档库 - Crosshair Overlay";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ClientSize = new Size(620, 500);

            flow = new FlowLayoutPanel();
            flow.SetBounds(10, 10, 600, 380);
            flow.AutoScroll = true;
            flow.WrapContents = true;
            flow.BackColor = Color.FromArgb(24, 26, 30);
            Controls.Add(flow);

            txtName = new TextBox();
            txtName.SetBounds(10, 402, 200, 24);
            txtName.Text = "新准星";
            Controls.Add(txtName);

            Button btnSave = new Button();
            btnSave.Text = "保存当前准星为存档";
            btnSave.SetBounds(218, 400, 160, 28);
            btnSave.Click += delegate { SaveCurrent(); };
            Controls.Add(btnSave);

            Button btnClose = new Button();
            btnClose.Text = "关闭";
            btnClose.SetBounds(520, 400, 90, 28);
            btnClose.Click += delegate { Hide(); };
            Controls.Add(btnClose);

            lblStatus = new Label();
            lblStatus.SetBounds(10, 436, 600, 50);
            lblStatus.Text = "点卡片载入 / 右键卡片删除；存档保存在 presets.ini";
            Controls.Add(lblStatus);

            RefreshCards();
        }

        string StorePath { get { return PresetStore.PathFor(owner.Cfg.Path); } }

        void SaveCurrent()
        {
            string name = PresetStore.CleanName(txtName.Text);
            if (name.Length == 0) { lblStatus.Text = "先起个名字"; return; }
            PresetStore.Save(StorePath, name, owner.Cfg);
            RefreshCards();
            lblStatus.Text = "已保存准星存档：" + name;
        }

        void RefreshCards()
        {
            flow.SuspendLayout();
            flow.Controls.Clear();
            foreach (string name in PresetStore.Names(StorePath))
                flow.Controls.Add(MakeCard(name));
            flow.ResumeLayout();
        }

        static Bitmap Preview(Config c, int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(28, 30, 34));
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Config t = new Config();
                t.CopyFrom(c);
                t.PixelSnap = false;
                double hx = t.GapX + t.SizeX + Math.Max(1, t.Outline) + 1;
                double hy = t.GapY + t.SizeY + Math.Max(1, t.Outline) + 1;
                double s = Math.Min((w * 0.38) / Math.Max(1, hx), (h * 0.38) / Math.Max(1, hy));
                if (s < 0.4) s = 0.4;
                CrosshairDraw.Render(g, t, w / 2.0, h / 2.0, s, s, 255, t.Colors[0], t.OutlineColor);
            }
            return bmp;
        }

        Panel MakeCard(string name)
        {
            Config tmp = new Config();
            tmp.CopyFrom(owner.Cfg);
            PresetStore.Apply(StorePath, name, tmp);

            Panel card = new Panel();
            card.SetBounds(0, 0, 138, 158);
            card.BorderStyle = BorderStyle.FixedSingle;
            card.BackColor = Color.FromArgb(36, 38, 44);
            card.Margin = new Padding(4);

            PictureBox pic = new PictureBox();
            pic.SetBounds(8, 8, 120, 104);
            pic.Image = Preview(tmp, 120, 104);
            pic.SizeMode = PictureBoxSizeMode.CenterImage;
            card.Controls.Add(pic);

            Label lbl = new Label();
            lbl.Text = name;
            lbl.SetBounds(6, 114, 126, 20);
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.ForeColor = Color.White;
            lbl.AutoEllipsis = true;
            card.Controls.Add(lbl);

            Label info = new Label();
            info.Text = tmp.CanvasWidth + "x" + tmp.CanvasHeight + " " + tmp.CanvasFit;
            info.SetBounds(6, 134, 126, 16);
            info.TextAlign = ContentAlignment.MiddleCenter;
            info.ForeColor = Color.Gray;
            card.Controls.Add(info);

            EventHandler load = delegate { LoadPreset(name); };
            card.Click += load;
            pic.Click += load;
            lbl.Click += load;
            info.Click += load;

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("载入", null, delegate { LoadPreset(name); });
            menu.Items.Add("删除", null, delegate { DeletePreset(name); });
            card.ContextMenuStrip = menu;

            return card;
        }

        void LoadPreset(string name)
        {
            if (!PresetStore.Apply(StorePath, name, owner.Cfg))
            {
                lblStatus.Text = "载入失败：" + name;
                return;
            }
            owner.RefreshOverlay();
            owner.ReloadSettingsUi();
            lblStatus.Text = "已载入：" + name;
        }

        void DeletePreset(string name)
        {
            if (MessageBox.Show(this, "删除准星存档 \"" + name + "\" ？", "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;
            PresetStore.Delete(StorePath, name);
            RefreshCards();
            lblStatus.Text = "已删除：" + name;
        }
    }

    static class SelfTest
    {
        static int fails;

        static void Check(string name, bool ok)
        {
            Console.WriteLine((ok ? "PASS  " : "FAIL  ") + name);
            if (!ok) fails++;
        }

        public static int Run(string dir)
        {
            Config c = new Config();
            c.CanvasWidth = 1234; c.CanvasHeight = 777;
            c.SizeX = 7; c.SizeY = 9; c.ThicknessX = 2; c.ThicknessY = 3;
            c.GapX = 1; c.GapY = 2; c.Glow = 42; c.Upscale = 1; c.PixelSnap = false;
            string cp = Path.Combine(dir, "selftest.ini");
            c.Save(cp);
            Config d = Config.Load(cp);
            Check("config: canvas", d.CanvasWidth == 1234 && d.CanvasHeight == 777);
            Check("config: geometry", Math.Abs(d.SizeX - 7) < 0.01 && Math.Abs(d.SizeY - 9) < 0.01);
            Check("config: glow/upscale/snap", d.Glow == 42 && d.Upscale == 1 && d.PixelSnap == false);
            try { File.Delete(cp); } catch { }

            Config e = new Config();
            Check("profile: valid line", Config.ApplyProfileLine(e, "800x600|stretch|6,10|2,2|4,4")
                  && e.CanvasWidth == 800 && e.CanvasHeight == 600 && Math.Abs(e.SizeX - 6) < 0.01);
            Check("profile: reject garbage", !Config.ApplyProfileLine(e, "not-a-profile"));

            string ps = Path.Combine(dir, "selftest_presets.ini");
            try { File.Delete(ps); } catch { }
            PresetStore.Save(ps, "unit test", c);
            bool has = false;
            foreach (string n in PresetStore.Names(ps)) if (n == "unit test") has = true;
            Check("preset: saved", has);
            Config f = new Config();
            Check("preset: applied", PresetStore.Apply(ps, "unit test", f) && f.CanvasWidth == 1234);
            PresetStore.Delete(ps, "unit test");
            has = false;
            foreach (string n in PresetStore.Names(ps)) if (n == "unit test") has = true;
            Check("preset: deleted", !has);
            try { File.Delete(ps); } catch { }

            Config g = new Config();
            g.CanvasWidth = 64; g.CanvasHeight = 64; g.CanvasFit = "stretch";
            g.PixelSnap = true; g.Outline = 0; g.OutlineMode = 1;
            g.SizeX = 2; g.SizeY = 2; g.ThicknessX = 2; g.ThicknessY = 2; g.GapX = 0; g.GapY = 0;
            g.Colors = new Color[] { Color.FromArgb(255, 0, 255, 0) };
            g.Alpha = 255;
            using (Bitmap bmp = new Bitmap(64, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics gr = Graphics.FromImage(bmp))
                {
                    gr.Clear(Color.Transparent);
                    gr.SmoothingMode = SmoothingMode.None;
                    CrosshairDraw.Render(gr, g, 32, 32, 1, 1, 255, g.Colors[0], Color.Black);
                }
                int solid = 0, minx = 99, maxx = -1, miny = 99, maxy = -1;
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        Color px = bmp.GetPixel(x, y);
                        if (px.A > 200 && px.G > 200 && px.R < 60)
                        {
                            solid++;
                            if (x < minx) minx = x;
                            if (x > maxx) maxx = x;
                            if (y < miny) miny = y;
                            if (y > maxy) maxy = y;
                        }
                    }
                Check("render: 12 solid pixels", solid == 12);
                Check("render: 4x4 bounding box", maxx - minx == 3 && maxy - miny == 3);
                Check("render: symmetric", (minx + maxx) == 63 && (miny + maxy) == 63);
            }

            Config h = new Config();
            h.CanvasWidth = 64; h.CanvasHeight = 64; h.CanvasFit = "stretch";
            h.PixelSnap = false; h.Outline = 0;
            h.Shape = "circle";
            h.SizeX = 6; h.SizeY = 6; h.ThicknessX = 2; h.ThicknessY = 2; h.GapX = 4; h.GapY = 4;
            h.Colors = new Color[] { Color.FromArgb(255, 0, 255, 0) };
            using (Bitmap bmp = new Bitmap(64, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics gr = Graphics.FromImage(bmp))
                {
                    gr.Clear(Color.Transparent);
                    gr.SmoothingMode = SmoothingMode.AntiAlias;
                    CrosshairDraw.Render(gr, h, 31.5, 31.5, 1, 1, 255, h.Colors[0], Color.Black);
                }
                int partial = 0;
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        int al = bmp.GetPixel(x, y).A;
                        if (al > 10 && al < 245) partial++;
                    }
                Check("render: antialiased circle edge", partial > 0);
            }

            Check("i18n: all language tables complete", Loc.TableOk());
            Check("i18n: lookup works for every language", Loc.T("title").Length > 0);

            Console.WriteLine();
            Console.WriteLine(fails == 0 ? "ALL TESTS PASSED" : (fails + " TEST(S) FAILED"));
            return fails == 0 ? 0 : 1;
        }
    }


    static class Program
    {
        public const string AppVersion = "1.1.1";
        static System.Threading.Mutex singleInstance;

        [STAThread]
        static void Main(string[] args)
        {
            try { OverlayForm.SetProcessDPIAware(); } catch { }
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string cfgPath = Path.Combine(exeDir, "crosshair.ini");
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--config" && i + 1 < args.Length) { cfgPath = args[i + 1]; i++; }
            }
            Config cfg = Config.Load(cfgPath);
            int langIdx = Loc.LangFromCode(cfg.Language);
            Loc.Current = langIdx >= 0 ? langIdx : Loc.AutoDetect();

            if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
            {
                Console.WriteLine("Crosshair Overlay " + AppVersion);
                return;
            }

            if (args.Length > 0 && args[0] == "--selftest")
            {
                string sd = Path.GetDirectoryName(Path.GetFullPath(cfgPath));
                Environment.ExitCode = SelfTest.Run(sd);
                return;
            }

            if (args.Length > 1 && args[0] == "--snapshot")
            {
                string outp = args[1];
                Rectangle scr = Screen.PrimaryScreen.Bounds;
                int px = scr.X + (int)(scr.Width * cfg.AnchorX / 100.0);
                int py = scr.Y + (int)(scr.Height * cfg.AnchorY / 100.0);
                int crop = 160;
                OverlayForm f = new OverlayForm(cfg);
                f.Show();
                Timer t = new Timer();
                t.Interval = 700;
                t.Tick += delegate
                {
                    t.Stop();
                    try
                    {
                        using (Bitmap bmp = new Bitmap(crop, crop))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                                g.CopyFromScreen(px - crop / 2, py - crop / 2, 0, 0, bmp.Size);
                            bmp.Save(outp, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        Console.WriteLine("wrote " + outp + " mode=" + (cfg.AlphaMode == 1 ? "alpha" : "key"));
                    }
                    catch (Exception ex) { Console.WriteLine("snapshot failed: " + ex.Message); }
                    f.QuitApp();
                };
                t.Start();
                Application.Run(f);
                return;
            }

            if (args.Length > 1 && args[0] == "--ui-shot")
            {
                OverlayForm f = new OverlayForm(cfg);
                f.Show();
                SettingsForm sf = new SettingsForm(f);
                sf.Show();
                Application.DoEvents();
                using (Bitmap b = new Bitmap(sf.Width, sf.Height))
                {
                    sf.DrawToBitmap(b, new Rectangle(0, 0, b.Width, b.Height));
                    b.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                }
                sf.Close();
                f.QuitApp();
                Console.WriteLine("ui shot " + args[1]);
                return;
            }

            if (args.Length > 1 && args[0] == "--layered-png")
            {
                OverlayForm f2 = new OverlayForm(cfg);
                using (Bitmap b = f2.RenderLayeredImage())
                {
                    b.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine("layered png " + b.Width + "x" + b.Height);
                }
                f2.Dispose();
                return;
            }

            if (args.Length > 1 && args[0] == "--render")
            {
                string outp = args[1];
                Rectangle scr = Screen.PrimaryScreen.Bounds;
                int sw = scr.Width, sh = scr.Height;
                double cwv = cfg.CanvasWidth, chv = cfg.CanvasHeight;
                if (cwv < 8 || chv < 8 || cfg.CanvasFit == "native") { cwv = sw; chv = sh; }
                double kx, ky;
                if (cfg.CanvasFit == "fit") { double s = Math.Min(sw / cwv, sh / chv); kx = s; ky = s; }
                else { kx = sw / cwv; ky = sh / chv; }

                using (Bitmap canvas = new Bitmap((int)cwv, (int)chv, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (Graphics gc = Graphics.FromImage(canvas))
                    {
                        gc.Clear(Color.Transparent);
                        gc.SmoothingMode = cfg.PixelSnap ? SmoothingMode.None : SmoothingMode.AntiAlias;
                        gc.PixelOffsetMode = PixelOffsetMode.Half;
                        CrosshairDraw.Render(gc, cfg, cwv * cfg.AnchorX / 100.0, chv * cfg.AnchorY / 100.0, 1.0, 1.0, 255, cfg.Colors[0], cfg.OutlineColor);
                    }
                    using (Bitmap screen = new Bitmap(sw, sh, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using (Graphics gs = Graphics.FromImage(screen))
                        {
                            gs.Clear(Color.FromArgb(64, 96, 64));
                            gs.InterpolationMode = cfg.PixelSnap ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBilinear;
                            gs.PixelOffsetMode = cfg.PixelSnap ? PixelOffsetMode.Half : PixelOffsetMode.HighQuality;
                            int dw = (int)Math.Round(cwv * kx), dh = (int)Math.Round(chv * ky);
                            gs.DrawImage(canvas, new Rectangle((sw - dw) / 2, (sh - dh) / 2, dw, dh));
                        }
                        int crop = 240;
                        foreach (string a in args)
                        {
                            int n;
                            if (int.TryParse(a, out n) && n >= 16)
                            {
                                crop = n;
                                break;
                            }
                        }
                        using (Bitmap c2 = new Bitmap(crop, crop, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                        {
                            using (Graphics g2 = Graphics.FromImage(c2))
                            {
                                g2.DrawImage(screen, new Rectangle(0, 0, crop, crop), new Rectangle(sw / 2 - crop / 2, sh / 2 - crop / 2, crop, crop), GraphicsUnit.Pixel);
                                g2.InterpolationMode = InterpolationMode.NearestNeighbor;
                            }
                            c2.Save(outp, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }
                Console.WriteLine("wrote " + outp + "  canvas=" + cwv + "x" + chv + " scale=" + Math.Round(kx, 3) + "x" + Math.Round(ky, 3));
                return;
            }

            if (args.Length > 0 && args[0] == "--presets")
            {
                string pp = PresetStore.PathFor(cfgPath);
                Console.WriteLine("presets file : " + pp);
                Config t = new Config();
                t.CopyFrom(cfg);
                t.CanvasWidth = 1234; t.CanvasHeight = 777; t.SizeX = 9; t.SizeY = 3;
                PresetStore.Save(pp, "test_preset", t);
                Console.WriteLine("names        : " + string.Join(", ", PresetStore.Names(pp).ToArray()));
                Config r = new Config();
                r.CopyFrom(cfg);
                bool ok = PresetStore.Apply(pp, "test_preset", r);
                Console.WriteLine("apply        : ok=" + ok + " canvas=" + r.CanvasWidth + "x" + r.CanvasHeight + " size=" + r.SizeX + "x" + r.SizeY);
                PresetStore.Delete(pp, "test_preset");
                Console.WriteLine("after delete : " + string.Join(", ", PresetStore.Names(pp).ToArray()));
                return;
            }

            if (args.Length > 0 && args[0] == "--profiles")
            {
                string pp = Path.Combine(Path.GetDirectoryName(cfgPath), "profiles.ini");
                Console.WriteLine("profiles file : " + pp + "  exists=" + File.Exists(pp));
                if (File.Exists(pp))
                {
                    foreach (string raw in File.ReadAllLines(pp, Encoding.UTF8))
                    {
                        string t = raw.Trim();
                        if (t.Length == 0 || t.StartsWith(";")) continue;
                        int eq = t.IndexOf('=');
                        if (eq <= 0) continue;
                        Config tmp = new Config();
                        tmp.CopyFrom(cfg);
                        bool ok = Config.ApplyProfileLine(tmp, t.Substring(eq + 1).Trim());
                        Console.WriteLine("  " + t.Substring(0, eq).Trim() + " -> ok=" + ok +
                                          " canvas=" + tmp.CanvasWidth + "x" + tmp.CanvasHeight +
                                          " fit=" + tmp.CanvasFit +
                                          " size=" + tmp.SizeX + "x" + tmp.SizeY);
                    }
                }
                return;
            }

            if (args.Length > 0 && args[0] == "--test")
            {
                Console.WriteLine("config   : " + cfgPath);
                Console.WriteLine("shape    : " + cfg.Shape + "  alpha=" + cfg.Alpha + " outline=" + cfg.Outline + "(mode " + cfg.OutlineMode + ") snap=" + cfg.PixelSnap + " alphaMode=" + cfg.AlphaMode);
                Console.WriteLine("canvas   : " + cfg.CanvasWidth + "x" + cfg.CanvasHeight + " fit=" + cfg.CanvasFit + " anchor=" + cfg.AnchorX + "%," + cfg.AnchorY + "%");
                StringBuilder ps = new StringBuilder();
                foreach (Point p in cfg.CanvasPresets) ps.Append((p.X == 0 ? "native" : p.X + "x" + p.Y) + " ");
                Console.WriteLine("presets  : " + ps);
                Console.WriteLine("geometry : size=" + cfg.SizeX + "x" + cfg.SizeY + " thickness=" + cfg.ThicknessX + "x" + cfg.ThicknessY + " gap=" + cfg.GapX + "x" + cfg.GapY);
                Console.WriteLine("colors   : " + JoinColors(cfg));
                Console.WriteLine("hotkeys  : settings=" + cfg.KeySettings + " canvas=" + cfg.KeyCanvas + " toggle=" + cfg.KeyToggle + " color=" + cfg.KeyColor + " reload=" + cfg.KeyReload);
                Console.WriteLine("screens  : " + Screen.AllScreens.Length);
                if (Screen.AllScreens.Length > 0)
                {
                    Rectangle b = Screen.AllScreens[0].Bounds;
                    double kx = (double)b.Width / cfg.CanvasWidth, ky = (double)b.Height / cfg.CanvasHeight;
                    Console.WriteLine("sample   : screen " + b.Width + "x" + b.Height + " / canvas " + cfg.CanvasWidth + "x" + cfg.CanvasHeight + " -> scaleX=" + Math.Round(kx, 3) + " scaleY=" + Math.Round(ky, 3));
                    Console.WriteLine("           arm " + cfg.SizeX + "x" + cfg.SizeY + " canvas px -> " + Math.Round(cfg.SizeX * kx, 1) + "x" + Math.Round(cfg.SizeY * ky, 1) + " screen px");
                }
                return;
            }

            if (args.Length == 0)
            {
                bool created;
                singleInstance = new System.Threading.Mutex(true, "Global\\CrosshairOverlay_SingleInstance", out created);
                if (!created)
                {
                    MessageBox.Show("Crosshair Overlay 已经在运行，请使用托盘图标操作。", "Crosshair Overlay",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool geom = false;
            foreach (string a in args) if (a == "--geom") geom = true;
            if (geom)
            {
                OverlayForm f = new OverlayForm(cfg);
                Console.WriteLine("window     : " + f.Bounds.X + "," + f.Bounds.Y + " " + f.Bounds.Width + "x" + f.Bounds.Height);
                Console.WriteLine("transparent: " + (f.UsingAlphaMode ? "per-pixel alpha" : "color key " + Config.Hex(f.TransparencyKey)));
                Console.WriteLine("backcolor  : " + Config.Hex(f.BackColor));
                Console.WriteLine("screen     : " + Screen.AllScreens[0].Bounds.Width + "x" + Screen.AllScreens[0].Bounds.Height);
                f.Dispose();
                return;
            }

            bool smoke = false;
            foreach (string a in args) if (a == "--smoke") smoke = true;

            if (smoke)
            {
                string log = Path.Combine(exeDir, "smoke_result.txt");
                try
                {
                    OverlayForm f = new OverlayForm(cfg);
                    f.Show();
                    f.ShowSettings();
                    f.ShowGallery();
                    Timer t = new Timer();
                    t.Interval = 1500;
                    t.Tick += delegate { t.Stop(); f.QuitApp(); };
                    t.Start();
                    Application.Run(f);
                    File.WriteAllText(log, "OK: overlay + settings form created and painted" + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    File.WriteAllText(log, "FAIL: " + ex.ToString() + Environment.NewLine);
                }
                return;
            }

            Application.Run(new OverlayForm(cfg));
        }

        static string JoinColors(Config c)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Color col in c.Colors)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append(Config.Hex(col));
            }
            return sb.ToString();
        }
    }
}
