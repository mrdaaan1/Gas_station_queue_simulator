using System;
using System.Collections.Generic;
using GasQueue.Nardy;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Длинные нарды с Гариком у мангала (правила — <see cref="Board"/>). Игрок — белые, Гарик — чёрные.
    /// Доска рисуется кодом (дерево, 24 «треугольника»), шашки и кубики — IMGUI.
    /// Управление: «Бросить кубики» (или Пробел), щелчок по своей шашке — подсвечиваются куда можно, щелчок туда — ход;
    /// выбросить с доски — щелчок по лотку справа. Пока играете, жизнь на заправке идёт своим чередом.
    /// </summary>
    public class NardyGame : MonoBehaviour
    {
        public static bool Active => instance != null;
        static NardyGame instance;

        enum Phase { Opening, PlayerRoll, PlayerMove, AiThink, AiMove, Over }

        Board board;
        Phase phase;
        Action<bool> onEnd;
        readonly System.Random rnd = new System.Random();
        int d1, d2;
        List<int> dice = new List<int>();
        int headUsed, headLimit;
        int selected = -1;                  // выбранная точка (свой счёт), −1 — нет
        List<Move> allowed = new List<Move>();
        List<Move> aiPlan;
        float timer;
        string say = "", sayWho = "";
        float sayTime;
        int openW, openB;
        bool playerWon;

        static Texture2D boardTex, whiteTex, blackTex, dotTex, pixel, glowTex;

        static readonly string[] DieName = { "", "ек", "ду", "се", "джар", "беш", "шеш" };
        static readonly string[] GarikGood =
        {
            "Вай, какой бросок, ахпер!", "Э-э, хорошо идёшь, джан.", "Повезло тебе, клянусь мамой!",
        };
        static readonly string[] GarikOwn =
        {
            "Смотри, как надо, джан!", "Ара, учись, пока я жив.", "Так, так, так...", "Сейчас закрою тебе всё, ахпер!",
            "Шашлык сам себя не выиграет, да?",
        };

        /// <summary>Открыть партию. onEnd(true) — игрок выиграл.</summary>
        public static void Open(Action<bool> onEnd)
        {
            if (instance != null) return;
            var go = new GameObject("Nardy");
            instance = go.AddComponent<NardyGame>();
            instance.onEnd = onEnd;
            instance.board = Board.Start();
            instance.phase = Phase.Opening;
            instance.timer = 1.2f;
            instance.Say("Гарик", "Садись, ахпер! Кидаем, кто первый ходит.");
            GameManager.Instance?.CameraRig?.SetCursorLocked(false);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Finished) gm.CameraRig?.SetCursorLocked(true);
        }

        void Say(string who, string text)
        {
            sayWho = who;
            say = text;
            sayTime = Time.unscaledTime;
        }

        static string RollName(int a, int b)
        {
            if (a == b) return a == 6 ? "Джут-шеш! Дубль шесть!" : $"Дубль {DieName[a]}!";
            int hi = Mathf.Max(a, b), lo = Mathf.Min(a, b);
            return $"{DieName[hi]}-{DieName[lo]}!";
        }

        // ---------- Ход партии ----------

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            timer -= dt;
            switch (phase)
            {
                case Phase.Opening:
                    if (timer > 0f) break;
                    openW = rnd.Next(1, 7);
                    openB = rnd.Next(1, 7);
                    if (openW == openB) { timer = 0.8f; Say("Гарик", $"{openW} и {openB} — ещё раз!"); break; }
                    if (openW > openB) { Say("Гарик", $"У тебя {openW}, у меня {openB}. Ходи первым, джан."); phase = Phase.PlayerRoll; }
                    else { Say("Гарик", $"У тебя {openW}, у меня {openB}. Я начинаю!"); StartAi(); }
                    break;
                case Phase.PlayerRoll:
                    if (GameInput.JumpPressed) RollForPlayer();
                    break;
                case Phase.PlayerMove:
                    if (allowed.Count == 0 && timer <= 0f) EndPlayerTurn();
                    break;
                case Phase.AiThink:
                    if (timer > 0f) break;
                    aiPlan = board.BestTurn(Board.Black, d1, d2, rnd);
                    if (aiPlan.Count == 0) Say("Гарик", "Вай, некуда ходить... Твой ход.");
                    else if (rnd.NextDouble() < 0.35) Say("Гарик", GarikOwn[rnd.Next(GarikOwn.Length)]);
                    phase = Phase.AiMove;
                    timer = 0.5f;
                    break;
                case Phase.AiMove:
                    if (timer > 0f) break;
                    if (aiPlan.Count > 0)
                    {
                        var m = aiPlan[0];
                        aiPlan.RemoveAt(0);
                        board.Apply(Board.Black, m);
                        dice.Remove(m.Die);
                        timer = 0.45f;
                        if (board.Won(Board.Black)) { GameOver(false); break; }
                    }
                    else
                    {
                        board.firstDone[Board.Black] = true;
                        dice.Clear();
                        phase = Phase.PlayerRoll;
                    }
                    break;
            }
        }

        void RollForPlayer()
        {
            d1 = rnd.Next(1, 7);
            d2 = rnd.Next(1, 7);
            dice = d1 == d2 ? new List<int> { d1, d1, d1, d1 } : new List<int> { d1, d2 };
            headUsed = 0;
            headLimit = board.HeadLimit(Board.White, d1, d2);
            selected = -1;
            allowed = board.AllowedMoves(Board.White, dice, headUsed, headLimit);
            phase = Phase.PlayerMove;
            Say("Гарик", RollName(d1, d2) + (d1 == d2 && rnd.NextDouble() < 0.6 ? " " + GarikGood[rnd.Next(GarikGood.Length)] : ""));
            if (allowed.Count == 0)
            {
                Say("Гарик", RollName(d1, d2) + " Ходить некуда, джан. Пропускаешь.");
                timer = 1.6f;
            }
        }

        void PlayerMove(Move m)
        {
            board.Apply(Board.White, m);
            if (m.From == 0) headUsed++;
            dice.Remove(m.Die);
            selected = -1;
            if (board.Won(Board.White)) { GameOver(true); return; }
            allowed = board.AllowedMoves(Board.White, dice, headUsed, headLimit);
            if (allowed.Count == 0) { timer = 0.4f; }
        }

        void EndPlayerTurn()
        {
            board.firstDone[Board.White] = true;
            dice.Clear();
            StartAi();
        }

        void StartAi()
        {
            d1 = rnd.Next(1, 7);
            d2 = rnd.Next(1, 7);
            dice = d1 == d2 ? new List<int> { d1, d1, d1, d1 } : new List<int> { d1, d2 };
            phase = Phase.AiThink;
            timer = 0.9f;
            Say("Гарик", RollName(d1, d2));
        }

        void GameOver(bool won)
        {
            playerWon = won;
            phase = Phase.Over;
            int oppOff = board.off[won ? Board.Black : Board.White];
            bool mars = oppOff == 0;
            if (won)
                Say("Ашот", mars ? "Марс! Вай, ахпер, ты где так научился?! Гарик, позор! Шашлык — за счёт заведения!"
                                 : "Вай, обыграл Гарика! Клянусь мамой, заслужил. Шашлык — за счёт заведения!");
            else
                Say("Гарик", mars ? "Марс, джан! Не расстраивайся, приходи отыграться." : "Ничего, ахпер, бывает. Приходи отыграться!");
        }

        void Close()
        {
            var cb = onEnd;
            bool won = phase == Phase.Over && playerWon;
            Destroy(gameObject);
            cb?.Invoke(won);
        }

        // ---------- Рисование ----------

        Rect boardRect, inner, trayW, trayB;
        float colW, barW, pointH;

        void Layout()
        {
            float sh = Screen.height, sw = Screen.width;
            float h = Mathf.Min(sh * 0.78f, sw * 0.5f);
            float w = h * 1.5f;
            boardRect = new Rect((sw - w) / 2f, (sh - h) / 2f - sh * 0.03f, w, h);
            float frame = h * 0.05f;
            inner = new Rect(boardRect.x + frame, boardRect.y + frame, boardRect.width - frame * 2f, boardRect.height - frame * 2f);
            barW = inner.width * 0.05f;
            colW = (inner.width - barW) / 12f;
            pointH = inner.height * 0.42f;
            float tw = colW * 1.1f;
            trayW = new Rect(boardRect.xMax + 6f, boardRect.y, tw, boardRect.height / 2f - 3f);          // белые выбрасывают сюда (их дом — справа сверху)
            trayB = new Rect(boardRect.x - 6f - tw, boardRect.y + boardRect.height / 2f + 3f, tw, boardRect.height / 2f - 3f);
        }

        /// <summary>Колонка (0…11 слева направо) и верх/низ для точки в общем счёте.</summary>
        void Spot(int abs, out float x, out bool top)
        {
            int col;
            if (abs >= 12) { top = true; col = abs - 12; }
            else { top = false; col = 11 - abs; }
            x = inner.x + col * colW + (col >= 6 ? barW : 0f) + colW / 2f;
        }

        int AbsAt(Vector2 p)
        {
            if (!inner.Contains(p)) return -1;
            float lx = p.x - inner.x;
            int col;
            if (lx < colW * 6f) col = Mathf.FloorToInt(lx / colW);
            else if (lx < colW * 6f + barW) return -1;
            else col = 6 + Mathf.FloorToInt((lx - colW * 6f - barW) / colW);
            col = Mathf.Clamp(col, 0, 11);
            bool top = p.y < inner.center.y;
            return top ? 12 + col : 11 - col;
        }

        void OnGUI()
        {
            GUI.depth = -90;
            EnsureTextures();
            Layout();
            var dim = new Color(0f, 0f, 0f, 0.55f);
            GUI.color = dim;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), pixel);
            GUI.color = Color.white;
            GUI.DrawTexture(boardRect, boardTex);
            DrawTray(trayW, Board.White);
            DrawTray(trayB, Board.Black);

            // Подсветка: выбранная точка и куда можно
            var targets = new List<int>();
            bool canOff = false;
            if (phase == Phase.PlayerMove && selected >= 0)
                foreach (var m in allowed)
                    if (m.From == selected) { if (m.To == 24) canOff = true; else targets.Add(Board.Abs(Board.White, m.To)); }
            var movable = new HashSet<int>();
            if (phase == Phase.PlayerMove) foreach (var m in allowed) movable.Add(Board.Abs(Board.White, m.From));

            float r = Mathf.Min(colW * 0.92f, pointH / 5f);
            for (int abs = 0; abs < 24; abs++)
            {
                Spot(abs, out float x, out bool top);
                int n = board.count[abs];
                bool isSel = selected >= 0 && Board.Abs(Board.White, selected) == abs;
                if (movable.Contains(abs) && !isSel)
                {
                    GUI.color = new Color(1f, 0.85f, 0.3f, 0.18f);
                    GUI.DrawTexture(new Rect(x - colW / 2f, top ? inner.y : inner.yMax - pointH, colW, pointH), pixel);
                }
                if (isSel)
                {
                    GUI.color = new Color(1f, 0.85f, 0.2f, 0.45f);
                    GUI.DrawTexture(new Rect(x - colW / 2f, top ? inner.y : inner.yMax - pointH, colW, pointH), pixel);
                }
                GUI.color = Color.white;
                int shown = Mathf.Min(n, 6);
                float step = n <= 5 ? r : (pointH - r) / Mathf.Max(1, shown - 1);
                for (int k = 0; k < shown; k++)
                {
                    float cy = top ? inner.y + r / 2f + k * step : inner.yMax - r / 2f - k * step;
                    GUI.DrawTexture(new Rect(x - r / 2f, cy - r / 2f, r, r), board.owner[abs] == Board.White ? whiteTex : blackTex);
                }
                if (n > 6)
                {
                    float cy = top ? inner.y + r / 2f + (shown - 1) * step : inner.yMax - r / 2f - (shown - 1) * step;
                    Label(new Rect(x - r / 2f, cy - r / 2f, r, r), n.ToString(), board.owner[abs] == Board.White ? Color.black : Color.white, 0.45f * r, TextAnchor.MiddleCenter);
                }
                if (targets.Contains(abs))
                {
                    int k = n;
                    float cy = top ? inner.y + r / 2f + Mathf.Min(k, 5) * r : inner.yMax - r / 2f - Mathf.Min(k, 5) * r;
                    GUI.color = new Color(0.3f, 1f, 0.4f, 0.75f);
                    GUI.DrawTexture(new Rect(x - r * 0.35f, cy - r * 0.35f, r * 0.7f, r * 0.7f), glowTex);
                    GUI.color = Color.white;
                }
            }
            if (canOff)
            {
                GUI.color = new Color(0.3f, 1f, 0.4f, 0.35f);
                GUI.DrawTexture(trayW, pixel);
                GUI.color = Color.white;
            }

            DrawDice();
            DrawPanel();
            HandleClicks(canOff);
            GUI.color = Color.white;
        }

        void DrawTray(Rect rect, int player)
        {
            GUI.color = new Color(0.25f, 0.16f, 0.09f, 0.95f);
            GUI.DrawTexture(rect, pixel);
            GUI.color = Color.white;
            int n = board.off[player];
            float h = Mathf.Min(rect.height / 15f, rect.width * 0.22f);
            for (int i = 0; i < n; i++)
            {
                var row = player == Board.White ? new Rect(rect.x + 4, rect.y + 4 + i * h, rect.width - 8, h - 1) : new Rect(rect.x + 4, rect.yMax - 4 - (i + 1) * h, rect.width - 8, h - 1);
                GUI.color = player == Board.White ? new Color(0.94f, 0.92f, 0.86f) : new Color(0.12f, 0.12f, 0.12f);
                GUI.DrawTexture(row, pixel);
            }
            GUI.color = Color.white;
            Label(new Rect(rect.x, player == Board.White ? rect.yMax - 22 : rect.y + 2, rect.width, 20), player == Board.White ? "вы" : "Гарик", new Color(1f, 0.9f, 0.7f), 13, TextAnchor.MiddleCenter);
        }

        void DrawDice()
        {
            if (d1 == 0 || (phase != Phase.PlayerMove && phase != Phase.AiThink && phase != Phase.AiMove)) return;
            float s = colW * 0.9f;
            bool aiSide = phase != Phase.PlayerMove;
            float cx = aiSide ? inner.x + colW * 3f : inner.x + colW * 9f + barW;
            float cy = inner.center.y - s / 2f;
            var all = d1 == d2 ? new[] { d1, d1, d1, d1 } : new[] { d1, d2 };
            var left = new List<int>(dice);
            float total = all.Length * s + (all.Length - 1) * s * 0.25f;
            float x0 = cx - total / 2f;
            for (int i = 0; i < all.Length; i++)
            {
                bool used = !left.Remove(all[i]);
                DrawDie(new Rect(x0 + i * s * 1.25f, cy, s, s), all[i], used);
            }
        }

        void DrawDie(Rect r, int v, bool used)
        {
            GUI.color = used ? new Color(0.7f, 0.68f, 0.62f, 0.55f) : new Color(0.98f, 0.97f, 0.93f);
            GUI.DrawTexture(r, pixel);
            GUI.color = used ? new Color(0.3f, 0.3f, 0.3f, 0.6f) : new Color(0.12f, 0.1f, 0.1f);
            float d = r.width * 0.2f;
            void Dot(float fx, float fy) => GUI.DrawTexture(new Rect(r.x + r.width * fx - d / 2f, r.y + r.height * fy - d / 2f, d, d), dotTex);
            if (v % 2 == 1) Dot(0.5f, 0.5f);
            if (v >= 2) { Dot(0.25f, 0.25f); Dot(0.75f, 0.75f); }
            if (v >= 4) { Dot(0.75f, 0.25f); Dot(0.25f, 0.75f); }
            if (v == 6) { Dot(0.25f, 0.5f); Dot(0.75f, 0.5f); }
            GUI.color = Color.white;
        }

        void DrawPanel()
        {
            var bar = new Rect(boardRect.x, boardRect.yMax + 8f, boardRect.width, 64f);
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(bar, pixel);
            GUI.color = Color.white;
            Label(new Rect(bar.x + 12, bar.y + 4, bar.width - 330, 26), sayWho + ": «" + say + "»", new Color(1f, 0.88f, 0.45f), 17, TextAnchor.MiddleLeft);
            string status = phase switch
            {
                Phase.Opening => "Розыгрыш первого хода...",
                Phase.PlayerRoll => "Ваш ход: бросьте кубики (Пробел)",
                Phase.PlayerMove => allowed.Count > 0 ? "Щёлкните свою шашку, потом — куда ходить" + (board.AllHome(Board.White) ? " (выбросить — лоток справа)" : "") : "Ходов нет",
                Phase.Over => playerWon ? "Вы выиграли! Шашлык бесплатно." : "Гарик выиграл.",
                _ => "Ходит Гарик...",
            };
            Label(new Rect(bar.x + 12, bar.y + 32, bar.width - 330, 24), status + $"   ·   до конца: вы {board.Pips(Board.White)}, Гарик {board.Pips(Board.Black)}", new Color(0.85f, 0.85f, 0.85f), 14, TextAnchor.MiddleLeft);

            var b1 = new Rect(bar.xMax - 310, bar.y + 10, 170, 44);
            var b2 = new Rect(bar.xMax - 130, bar.y + 10, 120, 44);
            if (phase == Phase.PlayerRoll && GUI.Button(b1, "Бросить кубики")) RollForPlayer();
            if (phase == Phase.Over)
            {
                if (GUI.Button(b2, "Встать")) Close();
            }
            else if (GUI.Button(b2, "Сдаться"))
            {
                playerWon = false;
                Say("Гарик", "Сдаёшься? Эх, ахпер... Приходи ещё!");
                phase = Phase.Over;
            }
        }

        void HandleClicks(bool canOff)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || phase != Phase.PlayerMove) return;
            var p = e.mousePosition;
            if (canOff && trayW.Contains(p))
            {
                foreach (var m in allowed)
                    if (m.From == selected && m.To == 24) { PlayerMove(PreferBigOff(m)); e.Use(); return; }
            }
            int abs = AbsAt(p);
            if (abs < 0) return;
            int rel = Board.Rel(Board.White, abs);
            if (selected >= 0)
            {
                foreach (var m in allowed)
                    if (m.From == selected && m.To == rel) { PlayerMove(m); e.Use(); return; }
            }
            bool hasMoves = false;
            foreach (var m in allowed) if (m.From == rel) hasMoves = true;
            selected = hasMoves ? rel : -1;
            e.Use();
        }

        /// <summary>Выбрасываем по возможности точным кубиком, а не большим.</summary>
        Move PreferBigOff(Move m)
        {
            Move best = m;
            foreach (var a in allowed)
                if (a.From == m.From && a.To == 24 && a.Die < best.Die) best = a;
            return best;
        }

        static void Label(Rect r, string text, Color c, float size, TextAnchor anchor)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(8, Mathf.RoundToInt(size)), alignment = anchor, fontStyle = FontStyle.Bold, wordWrap = false };
            st.normal.textColor = c;
            GUI.Label(r, text, st);
        }

        // ---------- Текстуры ----------

        static void EnsureTextures()
        {
            if (pixel == null) { pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply(); }
            if (boardTex == null) boardTex = MakeBoard(900, 600);
            if (whiteTex == null) whiteTex = MakeChecker(new Color(0.95f, 0.93f, 0.87f), new Color(0.72f, 0.68f, 0.58f));
            if (blackTex == null) blackTex = MakeChecker(new Color(0.13f, 0.12f, 0.12f), new Color(0.38f, 0.36f, 0.34f));
            if (dotTex == null) dotTex = MakeChecker(Color.white, Color.white);
            if (glowTex == null) glowTex = MakeChecker(Color.white, new Color(1f, 1f, 1f, 0.6f));
        }

        static Texture2D MakeChecker(Color face, Color rim)
        {
            const int n = 96;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((1f - d) * n * 0.5f);
                    var c = d > 0.82f ? rim : d > 0.62f && d < 0.68f ? rim : face;
                    float shade = 1f - 0.12f * Mathf.Clamp01(dy + 0.3f);
                    px[y * n + x] = new Color(c.r * shade, c.g * shade, c.b * shade, a * c.a);
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        /// <summary>Доска: тёмная рама, светлое дерево с прожилками, 24 «треугольника» двух цветов, перегородка посередине.</summary>
        static Texture2D MakeBoard(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            var rnd = new System.Random(4);
            float frame = h * 0.05f;
            float iw = w - frame * 2f, ih = h - frame * 2f;
            float barW = iw * 0.05f, colW = (iw - barW) / 12f, pointH = ih * 0.42f;
            var wood = new Color(0.82f, 0.62f, 0.38f);
            var frameC = new Color(0.36f, 0.2f, 0.1f);
            var dark = new Color(0.45f, 0.13f, 0.08f);
            var light = new Color(0.93f, 0.86f, 0.68f);
            var noise = new float[h];
            for (int y = 0; y < h; y++) noise[y] = (float)rnd.NextDouble();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // Текстура дерева: волнистые полосы вдоль доски
                    float grain = 0.92f + 0.08f * Mathf.Sin(y * 0.09f + Mathf.Sin(x * 0.013f) * 3f) + 0.03f * noise[(y + x / 37) % h];
                    Color c;
                    float ix = x - frame, iy = y - frame;
                    if (ix < 0 || iy < 0 || ix >= iw || iy >= ih) c = frameC * grain;
                    else if (ix >= colW * 6f && ix < colW * 6f + barW) c = frameC * 1.15f * grain;
                    else
                    {
                        c = wood * grain;
                        float lx = ix < colW * 6f ? ix : ix - barW;
                        int col = Mathf.Min(11, (int)(lx / colW));
                        float u = (lx - col * colW) / colW;             // 0…1 поперёк точки
                        // Текстура снизу вверх: y = 0 — низ картинки (низ доски)
                        float fromEdge = iy < ih / 2f ? iy : ih - 1 - iy;
                        float tri = 1f - fromEdge / pointH;              // ширина треугольника на этой высоте
                        if (tri > 0f && Mathf.Abs(u - 0.5f) < tri * 0.5f)
                        {
                            bool topRow = iy >= ih / 2f;
                            bool alt = (col + (topRow ? 1 : 0)) % 2 == 0;
                            c = (alt ? dark : light) * (0.96f + 0.04f * grain);
                        }
                    }
                    px[y * w + x] = c;
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }
}
