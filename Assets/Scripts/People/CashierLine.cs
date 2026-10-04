using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Живая очередь в кассу: водители NPC (PumpCustomer) и игрок (WalkerController).
    /// Первый в списке стоит у прилавка, остальные — друг за другом вдоль прилавка и стены.
    /// </summary>
    public static class CashierLine
    {
        public static readonly List<Component> Members = new List<Component>();

        /// <summary>Кассир ушла на перерыв — никого не обслуживают.</summary>
        public static bool CashierAway;
        public static Transform Cashier;

        const float Step = 0.9f;
        const int Straight = 6;

        public static void Reset()
        {
            Members.Clear();
            CashierAway = false;
            Cashier = null;
        }

        public static int Count
        {
            get
            {
                Members.RemoveAll(m => m == null);
                return Members.Count;
            }
        }

        public static int IndexOf(Component c)
        {
            Members.RemoveAll(m => m == null);
            return Members.IndexOf(c);
        }

        public static int Join(Component c)
        {
            if (!Members.Contains(c)) Members.Add(c);
            return IndexOf(c);
        }

        public static void Leave(Component c) => Members.Remove(c);

        public static void InsertAt(Component c, int index)
        {
            Members.Remove(c);
            Members.RemoveAll(m => m == null);
            Members.Insert(Mathf.Clamp(index, 0, Members.Count), c);
        }

        public static bool IsFront(Component c) => IndexOf(c) == 0;

        /// <summary>Где стоит i-й: вдоль прилавка, потом заворачиваем вдоль стены обратно к двери.</summary>
        public static Vector3 Slot(int i)
        {
            var front = CityLayout.CounterFront;
            if (i < Straight) return front + new Vector3(0f, 0f, Step * i);
            int j = Mathf.Min(i - Straight, 6);
            return new Vector3(front.x - 1.4f, 0f, front.z + Step * (Straight - 1) - Step * j);
        }

        /// <summary>Куда смотрит стоящий на i-м месте: на того, кто впереди (первый — на кассира).</summary>
        public static Vector3 FacingPoint(int i) =>
            i == 0 ? CityLayout.CashierSpot : Slot(i - 1);
    }
}
