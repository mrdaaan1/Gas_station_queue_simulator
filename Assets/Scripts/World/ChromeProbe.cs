using UnityEngine;
using UnityEngine.Rendering;

namespace GasQueue
{
    /// <summary>
    /// Отражения для хромированной машины (X5 в золотой плёнке): свой realtime-зонд отражений едет вместе с машиной
    /// и раз в четверть секунды снимает окружение (по одной грани за кадр). Саму машину зонд не видит —
    /// её детали переносятся на отдельный слой, который исключён из съёмки.
    /// </summary>
    public class ChromeProbe : MonoBehaviour
    {
        const int CarLayer = 31; // незанятый слой: камеры его рисуют (маска «всё»), а зонд — нет
        ReflectionProbe probe;
        float timer;

        void Start()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = CarLayer;
            var go = new GameObject("ChromeProbe");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = 128;
            probe.size = new Vector3(40f, 30f, 40f);
            probe.nearClipPlane = 0.3f;
            probe.farClipPlane = 200f;
            probe.cullingMask = ~(1 << CarLayer);
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            probe.hdr = true;
            probe.importance = 10;
            probe.RenderProbe();
        }

        void Update()
        {
            if (probe == null) return;
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = 0.25f;
            probe.RenderProbe();
        }
    }
}
