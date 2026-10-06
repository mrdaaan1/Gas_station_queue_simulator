#!/bin/bash
# Превью заставки без Unity: bash Tools/IntroArt/preview.sh <папка> [ширина высота] [моменты времени...]
# Пример: bash Tools/IntroArt/preview.sh /tmp/intro 1920 1080 0.5 1.2 2.0 3.5
set -e
OUT="${1:-/tmp/intro}"; shift || true
export INTRO_OUT="$OUT/build"
dotnet run -c Release --project "$(dirname "$0")/Preview/IntroPreview.csproj" -- "$OUT" "$@"
