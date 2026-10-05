# Просмотр моделей машин

Собирает модель из `Assets/Scripts/World/Models` (чистый C#, без Unity) и рендерит её
в headless-браузере через three.js с нескольких ракурсов.

```bash
cd Tools/ModelPreview
npm install three@0.160.0 --prefix "$SP/view"   # один раз, SP — рабочая папка
bash run.sh supra1                               # → $SP/view/supra1_grid.png
bash run.sh cab "#c8141e" '{"cab":"pos=0.37,1.065,-0.86&at=0.37,0.98,1.0&fov=72"}'
```

Ракурс задаётся строкой: `pos` — камера, `at` — куда смотрит (координаты Unity: X вправо, Y вверх, Z вперёд),
`fov`, `hide=Узел,материал` — спрятать части, `paint=#цвет`.
Материалы в `view.html` подобраны похоже на игровые (`CarMaterials` в `ModelSpawner.cs`).
