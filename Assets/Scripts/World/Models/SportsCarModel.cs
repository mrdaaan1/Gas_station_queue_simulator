using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Общее для моделей спорткаров: дерево узлов (кузов, капот, двери, бамперы…), номера, эмблемы,
    /// стрелочные приборы. Конкретная машина задаёт форму (<see cref="CarBody"/>) и детали.
    /// </summary>
    public abstract class SportsCarModel : CarBody
    {
        protected Model model;
        protected ModelNode body;
        readonly Dictionary<string, ModelNode> nodes = new Dictionary<string, ModelNode>();

        /// <summary>Шарнир узла (капот поднимается от него, бампер повисает на нём).</summary>
        protected virtual Vector3 Pivot(string name) => Vector3.zero;

        /// <summary>Глаза водителя — к ним повёрнуты приборы.</summary>
        protected abstract Vector3 Eyes { get; }

        protected void StartModel()
        {
            model = new Model();
            body = model.root.Child("Body");
            nodes["Shell"] = body.Child("Shell");
        }

        protected ModelNode N(string name)
        {
            if (nodes.TryGetValue(name, out var n)) return n;
            n = body.Child(name, Pivot(name));
            nodes[name] = n;
            return n;
        }

        /// <summary>Российский номер на поверхности кузова (текст добавляет игра).</summary>
        protected void Plate(ModelNode node, Vector3 origin, Vector3 dir, string name)
        {
            OnBody(origin, dir, 1f, out var p, out var n);
            PlateAt(node, p + n * 0.012f, n, name);
        }

        protected void PlateAt(ModelNode node, Vector3 p, Vector3 n, string name)
        {
            var f = node.WorldFrame();
            var plateNode = node.Child(name, f.ToLocal(p), LookEuler(-n));
            Geo.RoundBox(plateNode.M("plate"), Frame.Identity, Vector3.zero, new Vector3(0.52f, 0.112f, 0.012f), 0.12f, 6);
        }

        /// <summary>Углы Эйлера (как в Unity), поворачивающие +Z в направление dir с осью Y вверх.</summary>
        public static Vector3 LookEuler(Vector3 dir)
        {
            dir = dir.normalized;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            return new Vector3(pitch, yaw, 0f);
        }

        /// <summary>Овальная эмблема без настоящего логотипа.</summary>
        protected void Emblem(ModelNode node, Vector3 origin, Vector3 dir, float size)
        {
            OnBody(origin, dir, 1f, out var p, out var n);
            var f = node.WorldFrame();
            var lf = Frame.Look(f.ToLocal(p + n * 0.006f), f.DirToLocal(n), Vector3.up);
            var ring = new Frame { o = lf.o, x = lf.x, y = lf.z, z = -lf.y * 0.62f };
            Geo.Torus(node.M("chrome"), ring, size * 0.5f, 0.004f, 28, 6);
            Geo.Torus(node.M("chrome"), ring, size * 0.24f, 0.0035f, 20, 6);
        }

        /// <summary>Круглый прибор лицом к водителю; стрелка — отдельный узел «…Needle», крутится вокруг своей Z.</summary>
        protected void Gauge(ModelNode parent, string name, Vector3 center, float r, bool ticks, string face = "gauge_face", string tickMat = "white")
        {
            var away = (center - Eyes).normalized;
            var g = parent.Child("Gauge" + name, center, LookEuler(away));
            var id = Frame.Identity;
            Geo.Polygon(g.M(face), id, Geo.Circle(r, 28));
            var ringF = new Frame { o = new Vector3(0, 0, -0.002f), x = Vector3.right, y = Vector3.back, z = Vector3.up };
            Geo.Torus(g.M("chrome"), ringF, r + 0.004f, 0.0045f, 32, 6);
            if (ticks)
                for (int i = 0; i <= 12; i++)
                {
                    float a = Mathf.Lerp(210f, -30f, i / 12f) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    var tf = Frame.Look(dir * r * 0.82f + new Vector3(0, 0, -0.002f), Vector3.back, dir);
                    Geo.Box(g.M(tickMat), tf, Vector3.zero, new Vector3(0.003f, i % 2 == 0 ? r * 0.2f : r * 0.11f, 0.001f));
                }
            var needle = g.Child(name + "Needle", new Vector3(0f, 0f, -0.004f));
            Geo.Box(needle.M("needle"), id, new Vector3(0f, r * 0.38f, 0f), new Vector3(0.0035f, r * 0.85f, 0.002f));
            Geo.Cylinder(g.M("int_black"), new Frame { o = new Vector3(0, 0, -0.006f), x = Vector3.right, y = Vector3.back, z = Vector3.up }, 0.008f, 0f, 0.004f, 10);
        }

        /// <summary>Водитель: туловище и голова (их прячем при виде из салона, руки достраивает игра).</summary>
        protected void Driver(float torsoY, float torsoZ, float lean)
        {
            var torso = body.Child("DriverTorso", new Vector3(Eyes.x, torsoY, torsoZ), new Vector3(lean, 0f, 0f));
            Geo.RoundBox(torso.M("jacket"), Frame.Identity, Vector3.zero, new Vector3(0.42f, 0.5f, 0.24f), 0.45f, 10);
            var head = body.Child("DriverHead", Eyes + new Vector3(0f, 0.02f, -0.05f));
            Geo.RoundBox(head.M("skin"), Frame.Identity, Vector3.zero, new Vector3(0.19f, 0.24f, 0.22f), 0.9f, 12);
            Geo.RoundBox(head.M("hair"), Frame.Identity, new Vector3(0f, 0.06f, -0.025f), new Vector3(0.2f, 0.15f, 0.21f), 0.85f, 12);
        }
    }
}
