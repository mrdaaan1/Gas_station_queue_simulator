#!/bin/bash
# Пересобрать модель и отрендерить: bash run.sh <prefix> [paint] [views-json]
set -e
SP=${SP:-/tmp/claude-0/-home-user-Gas-station-queue-simulator/d2c0a8a9-7e97-5f5a-8173-26ca73b8e82a/scratchpad}
HERE=$(cd "$(dirname "$0")" && pwd)
dotnet build "$HERE/ModelPreview.csproj" -c Release -o $SP/mp 2>&1 | grep -E " error |rror\(s\)" | grep -v "0 Error" || true
cd $SP/view
dotnet $SP/mp/ModelPreview.dll ${MODEL:-supra} model.json
cp "$HERE/view.html" "$HERE/shoot.js" .
node shoot.js $SP/view "$1" "${2:-#c8141e}" "$3"
python3 "$HERE/grid.py" $SP/view "$1" $SP/view/$1_grid.png ${COLS:-2}
