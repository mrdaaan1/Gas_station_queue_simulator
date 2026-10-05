using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Настоящие зеркала в машине игрока: салонное и два боковых. За каждым — маленькая камера,
    /// которая смотрит назад и рисует картинку в текстуру; на стекле она отражена по горизонтали, как в зеркале.
    /// Камеры работают только при виде из салона, чтобы не тратить кадры зря.
    /// </summary>
    public class CarMirrors : MonoBehaviour
    {
        class Mirror
        {
            public Camera cam;
            public RenderTexture texture;
        }

        Mirror rear, left, right;
        CameraRig rig;

        public void Init(CarVisual visual, CameraRig cameraRig)
        {
            rig = cameraRig;
            // Салонное: смотрим назад над крышей сидений, всё, что ближе заднего бампера, не рисуем —
            // иначе в зеркале была бы спина собственного сиденья.
            rear = Create(visual.mirrorRear, 384, 96, 13f, visual.rearMirrorNear, 0f, 1.5f);
            left = Create(visual.mirrorLeft, 192, 128, 24f, 0.3f, 12f, 2f);
            right = Create(visual.mirrorRight, 192, 128, 24f, 0.3f, -14f, 2f);
        }

        /// <summary>Камера за стеклом зеркала. yawOut — насколько смотрит вбок от «строго назад».</summary>
        Mirror Create(Transform glass, int w, int h, float fov, float near, float yawOut, float pitch)
        {
            if (glass == null) return null;
            var texture = new RenderTexture(w, h, 16) { name = glass.name + "View", antiAliasing = 1 };
            texture.Create();

            var camGo = new GameObject(glass.name + "Camera");
            camGo.transform.SetParent(FindBody(glass), false);
            camGo.transform.position = glass.position;
            camGo.transform.localRotation = Quaternion.Euler(pitch, 180f + yawOut, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.targetTexture = texture;
            cam.fieldOfView = fov;
            cam.nearClipPlane = near;
            cam.farClipPlane = 220f;
            cam.depth = -10f;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.useOcclusionCulling = false;
            if (RenderSettings.skybox != null) cam.clearFlags = CameraClearFlags.Skybox;
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = RenderSettings.fogColor;
            }

            var mat = new Material(MirrorBase()) { name = glass.name + "Mat", mainTexture = texture };
            // Зеркало переворачивает картинку слева направо
            mat.mainTextureScale = new Vector2(-1f, 1f);
            mat.mainTextureOffset = new Vector2(1f, 0f);
            glass.GetComponent<Renderer>().sharedMaterial = mat;
            return new Mirror { cam = cam, texture = texture };
        }

        /// <summary>Камеры вешаем на кузов (а не на повёрнутое к водителю зеркало), чтобы они смотрели ровно назад.</summary>
        static Transform FindBody(Transform glass)
        {
            var t = glass;
            while (t.parent != null && t.name != "Body") t = t.parent;
            return t;
        }

        static Material mirrorBase;

        static Material MirrorBase()
        {
            if (mirrorBase != null) return mirrorBase;
            mirrorBase = Resources.Load<Material>("GasQueueGenerated/Mirror"); // есть в сборке
            if (mirrorBase == null)
            {
                var shader = Shader.Find("Unlit/Texture");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                mirrorBase = shader != null ? new Material(shader) : Shapes.Mat(Color.white);
            }
            return mirrorBase;
        }

        void LateUpdate()
        {
            bool on = rig != null && !rig.OnFoot && rig.Mode == CameraMode.Cabin;
            Set(rear, on);
            Set(left, on);
            Set(right, on);
        }

        static void Set(Mirror m, bool on)
        {
            if (m != null && m.cam.enabled != on) m.cam.enabled = on;
        }

        void OnDestroy()
        {
            foreach (var m in new[] { rear, left, right })
            {
                if (m == null) continue;
                if (m.cam != null) m.cam.targetTexture = null;
                if (m.texture != null) m.texture.Release();
            }
        }
    }
}
