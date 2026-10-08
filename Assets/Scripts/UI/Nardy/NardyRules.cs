using System;
using System.Collections.Generic;

namespace GasQueue.Nardy
{
    /// <summary>Ход одной шашкой: с какой точки (в своём счёте 0…23), на сколько; To = 24 — выброс с доски.</summary>
    public struct Move
    {
        public int From, Die;
        public int To => From + Die > 23 ? 24 : From + Die;
        public Move(int from, int die) { From = from; Die = die; }
        public override string ToString() => $"{From}->{(To == 24 ? "off" : To.ToString())}";
    }

    /// <summary>
    /// Длинные нарды — чистая логика без Unity. Два игрока по 15 шашек, у каждого своя «голова» (у белых — точка 0,
    /// у чёрных — 12 в общем счёте), оба идут в одну сторону по кругу; бить нельзя — точка с хоть одной чужой шашкой закрыта.
    /// Правила: с головы за ход — одна шашка (в первый ход при дублях 6-6, 4-4, 3-3 — две); дубль — четыре хода;
    /// нужно сыграть максимум кубиков, а если можно только один — больший; нельзя строить «забор» из шести точек подряд,
    /// если впереди него нет ни одной чужой шашки; выбрасывать — когда все 15 в своём доме (последняя четверть пути),
    /// точным числом или большим, если дальше от выброса шашек нет.
    /// </summary>
    public class Board
    {
        public const int White = 0, Black = 1;
        public static readonly int[] Offset = { 0, 12 };

        /// <summary>Шашки по точкам в общем счёте 0…23: владелец (−1 — пусто) и сколько.</summary>
        public readonly int[] owner = new int[24];
        public readonly int[] count = new int[24];
        public readonly int[] off = new int[2];
        /// <summary>Сделал ли игрок уже свой первый ход (для исключения с дублями с головы).</summary>
        public readonly bool[] firstDone = new bool[2];

        public static Board Start()
        {
            var b = new Board();
            for (int i = 0; i < 24; i++) b.owner[i] = -1;
            b.owner[Offset[White]] = White; b.count[Offset[White]] = 15;
            b.owner[Offset[Black]] = Black; b.count[Offset[Black]] = 15;
            return b;
        }

        public Board Clone()
        {
            var b = new Board();
            Array.Copy(owner, b.owner, 24);
            Array.Copy(count, b.count, 24);
            b.off[0] = off[0]; b.off[1] = off[1];
            b.firstDone[0] = firstDone[0]; b.firstDone[1] = firstDone[1];
            return b;
        }

        public static int Abs(int player, int rel) => (rel + Offset[player]) % 24;
        public static int Rel(int player, int abs) => (abs - Offset[player] + 24) % 24;

        public int CountAt(int player, int rel)
        {
            int a = Abs(player, rel);
            return owner[a] == player ? count[a] : 0;
        }

        public bool AllHome(int player)
        {
            int inHome = off[player];
            for (int r = 18; r < 24; r++) inHome += CountAt(player, r);
            return inHome == 15;
        }

        public int Pips(int player)
        {
            int p = 0;
            for (int r = 0; r < 24; r++) p += CountAt(player, r) * (24 - r);
            return p;
        }

        public bool Won(int player) => off[player] >= 15;

        public string Key()
        {
            var c = new char[50];
            for (int i = 0; i < 24; i++) { c[i * 2] = (char)('a' + owner[i] + 1); c[i * 2 + 1] = (char)('a' + count[i]); }
            c[48] = (char)('a' + off[0]); c[49] = (char)('a' + off[1]);
            return new string(c);
        }

        /// <summary>Можно ли сделать ход (без учёта правила головы — его считает генератор хода целиком).</summary>
        public bool CanMove(int player, Move m)
        {
            if (CountAt(player, m.From) == 0) return false;
            int to = m.From + m.Die;
            if (to > 23)
            {
                if (!AllHome(player)) return false;
                if (to == 24) return true;
                // Больше нужного — только с самой дальней от выброса шашки
                for (int r = 18; r < m.From; r++) if (CountAt(player, r) > 0) return false;
                return true;
            }
            int a = Abs(player, to);
            if (owner[a] != -1 && owner[a] != player) return false;
            // «Забор» из шести без чужих шашек впереди — нельзя
            var tmp = Clone();
            tmp.Apply(player, m);
            return !tmp.IllegalPrime(player);
        }

        public void Apply(int player, Move m)
        {
            int a = Abs(player, m.From);
            count[a]--;
            if (count[a] == 0) owner[a] = -1;
            if (m.To == 24) { off[player]++; return; }
            int b = Abs(player, m.To);
            owner[b] = player;
            count[b]++;
        }

        /// <summary>Есть ли у игрока шесть точек подряд, перед которыми (по ходу соперника) нет ни одной шашки соперника.</summary>
        bool IllegalPrime(int player)
        {
            int opp = 1 - player;
            int run = 0;
            for (int r = 0; r < 24; r++)
            {
                run = CountAt(player, r) > 0 ? run + 1 : 0;
                if (run < 6) continue;
                // Точки забора в счёте соперника; если забор «переламывается» через его старт — пропускаем (редкость)
                int endOpp = Rel(opp, Abs(player, r));
                int startOpp = Rel(opp, Abs(player, r - 5));
                if (startOpp > endOpp) continue;
                if (off[opp] > 0) continue;
                bool ahead = false;
                for (int q = endOpp + 1; q < 24 && !ahead; q++) if (CountAt(opp, q) > 0) ahead = true;
                if (!ahead) return true;
            }
            return false;
        }

        /// <summary>
        /// Все допустимые ходы целиком (последовательности) для броска: только самые длинные,
        /// при одном возможном ходе из двух разных кубиков — бо́льшим. Одинаковые итоговые позиции склеиваются.
        /// </summary>
        public List<List<Move>> Turns(int player, int d1, int d2)
        {
            var dice = d1 == d2 ? new List<int> { d1, d1, d1, d1 } : new List<int> { d1, d2 };
            bool firstDouble = !firstDone[player] && d1 == d2 && (d1 == 6 || d1 == 4 || d1 == 3);
            int headLimit = firstDouble ? 2 : 1;
            var results = new Dictionary<string, List<Move>>();
            var seen = new HashSet<string>();
            int best = 0;
            Search(this, player, dice, new List<Move>(), 0, headLimit, results, seen, ref best);
            var list = new List<List<Move>>();
            foreach (var kv in results) if (kv.Value.Count == best) list.Add(kv.Value);
            if (best == 1 && d1 != d2)
            {
                int big = Math.Max(d1, d2);
                var withBig = list.FindAll(s => s[0].Die == big);
                if (withBig.Count > 0) list = withBig;
            }
            return list;
        }

        static void Search(Board b, int player, List<int> dice, List<Move> path, int headUsed, int headLimit,
            Dictionary<string, List<Move>> results, HashSet<string> seen, ref int best)
        {
            string key = b.Key() + "|" + string.Join(",", dice) + "|" + headUsed;
            if (!seen.Add(key)) return;
            bool any = false;
            var tried = new HashSet<int>();
            for (int i = 0; i < dice.Count; i++)
            {
                int d = dice[i];
                if (!tried.Add(d)) continue;
                for (int r = 0; r < 24; r++)
                {
                    if (b.CountAt(player, r) == 0) continue;
                    if (r == 0 && headUsed >= headLimit) continue;
                    var m = new Move(r, d);
                    if (!b.CanMove(player, m)) continue;
                    any = true;
                    var nb = b.Clone();
                    nb.Apply(player, m);
                    var rest = new List<int>(dice);
                    rest.RemoveAt(i);
                    path.Add(m);
                    Search(nb, player, rest, path, headUsed + (r == 0 ? 1 : 0), headLimit, results, seen, ref best);
                    path.RemoveAt(path.Count - 1);
                }
            }
            if (!any && path.Count > 0)
            {
                if (path.Count > best) best = path.Count;
                string end = b.Key();
                if (!results.ContainsKey(end) || results[end].Count < path.Count) results[end] = new List<Move>(path);
            }
        }

        /// <summary>Сколько ходов подряд ещё можно сделать этими кубиками (для проверки, какие ходы разрешены).</summary>
        public int MaxDepth(int player, List<int> dice, int headUsed, int headLimit, Dictionary<string, int> memo = null)
        {
            memo ??= new Dictionary<string, int>();
            string key = Key() + "|" + string.Join(",", dice) + "|" + headUsed;
            if (memo.TryGetValue(key, out int cached)) return cached;
            int best = 0;
            var tried = new HashSet<int>();
            for (int i = 0; i < dice.Count && best < dice.Count; i++)
            {
                if (!tried.Add(dice[i])) continue;
                for (int r = 0; r < 24 && best < dice.Count; r++)
                {
                    if (CountAt(player, r) == 0 || (r == 0 && headUsed >= headLimit)) continue;
                    var m = new Move(r, dice[i]);
                    if (!CanMove(player, m)) continue;
                    var nb = Clone();
                    nb.Apply(player, m);
                    var rest = new List<int>(dice);
                    rest.RemoveAt(i);
                    best = Math.Max(best, 1 + nb.MaxDepth(player, rest, headUsed + (r == 0 ? 1 : 0), headLimit, memo));
                }
            }
            memo[key] = best;
            return best;
        }

        /// <summary>
        /// Какие одиночные ходы сейчас разрешены (для игрока-человека): только те, после которых можно сыграть
        /// максимум оставшихся кубиков; если из двух разных кубиков можно сыграть лишь один — только больший.
        /// </summary>
        public List<Move> AllowedMoves(int player, List<int> dice, int headUsed, int headLimit)
        {
            var memo = new Dictionary<string, int>();
            int best = MaxDepth(player, dice, headUsed, headLimit, memo);
            var list = new List<Move>();
            if (best == 0) return list;
            var tried = new HashSet<int>();
            for (int i = 0; i < dice.Count; i++)
            {
                if (!tried.Add(dice[i])) continue;
                for (int r = 0; r < 24; r++)
                {
                    if (CountAt(player, r) == 0 || (r == 0 && headUsed >= headLimit)) continue;
                    var m = new Move(r, dice[i]);
                    if (!CanMove(player, m)) continue;
                    var nb = Clone();
                    nb.Apply(player, m);
                    var rest = new List<int>(dice);
                    rest.RemoveAt(i);
                    if (1 + nb.MaxDepth(player, rest, headUsed + (r == 0 ? 1 : 0), headLimit, memo) == best) list.Add(m);
                }
            }
            if (best == 1 && dice.Count == 2 && dice[0] != dice[1])
            {
                int big = Math.Max(dice[0], dice[1]);
                var withBig = list.FindAll(m => m.Die == big);
                if (withBig.Count > 0) list = withBig;
            }
            return list;
        }

        /// <summary>Сколько шашек можно снять с головы за этот ход.</summary>
        public int HeadLimit(int player, int d1, int d2) =>
            !firstDone[player] && d1 == d2 && (d1 == 6 || d1 == 4 || d1 == 3) ? 2 : 1;

        /// <summary>Оценка позиции для компьютера: меньше пути до выброса, больше перекрытых точек перед соперником, без «колонн».</summary>
        public float Evaluate(int player)
        {
            int opp = 1 - player;
            float s = -Pips(player) + off[player] * 6f;
            // Свои точки на пути соперника перед его шашками мешают ему — это хорошо
            int oppBack = 24;
            for (int r = 0; r < 24; r++) if (CountAt(opp, r) > 0) { oppBack = r; break; }
            int made = 0;
            for (int r = 0; r < 24; r++)
            {
                int c = CountAt(player, r);
                if (c == 0) continue;
                made++;
                int oppRel = Rel(opp, Abs(player, r));
                if (oppRel > oppBack && oppRel < 18) s += 2.5f;
                if (c > 3) s -= (c - 3) * 1.2f;   // не громоздить колонны
            }
            s += made * 0.8f;
            if (CountAt(player, 0) > 1 && firstDone[player]) s -= CountAt(player, 0) * 0.4f; // голову разбирать
            return s;
        }

        /// <summary>Лучший ход для компьютера (с небольшой случайностью, чтобы не играл одинаково).</summary>
        public List<Move> BestTurn(int player, int d1, int d2, Random rnd)
        {
            var turns = Turns(player, d1, d2);
            List<Move> best = null;
            float bestScore = float.MinValue;
            foreach (var t in turns)
            {
                var b = Clone();
                foreach (var m in t) b.Apply(player, m);
                float score = b.Evaluate(player) + (float)rnd.NextDouble() * 0.6f;
                if (score > bestScore) { bestScore = score; best = t; }
            }
            return best ?? new List<Move>();
        }
    }
}
