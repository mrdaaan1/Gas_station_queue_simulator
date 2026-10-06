#!/bin/bash
# Выпустить новую версию игры для Mac одной командой:
#   1) забрать свежий код (git pull);
#   2) проставить номер версии (виден в главном меню игры);
#   3) собрать .app через Unity без окна (Tools/build_mac.sh), подписать и упаковать (Tools/package_mac.sh);
#   4) сложить архив GasQueue-<версия>-Mac.zip и «Что нового» в Builds/Releases.
#
# Запуск (Unity с проектом должна быть закрыта):
#   bash Tools/release_mac.sh                 — версия по дате, например 2026.10.06 (вторая за день — 2026.10.06.2)
#   bash Tools/release_mac.sh 0.14            — своя версия
#   bash Tools/release_mac.sh --no-music      — без своих треков из Assets/Resources/RaceMusic
#                                                (для публичного выпуска: чужую музыку раздавать нельзя)
#   bash Tools/release_mac.sh --no-pull       — собрать как есть, без git pull
set -e
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$PROJECT"

VERSION=""
WITH_MUSIC=1
PULL=1
for arg in "$@"; do
  case "$arg" in
    --no-music) WITH_MUSIC=0 ;;
    --no-pull) PULL=0 ;;
    -h|--help) sed -n '2,15p' "$0"; exit 0 ;;
    *) VERSION="$arg" ;;
  esac
done

RELEASES="$PROJECT/Builds/Releases"
mkdir -p "$RELEASES"

# ---------- 1. Свежий код ----------
if [ "$PULL" = 1 ]; then
  echo "== Забираю свежий код (git pull)..."
  if ! git pull --ff-only; then
    echo "git pull не прошёл (локальные изменения или нет сети). Разберитесь или запустите с --no-pull."
    exit 1
  fi
fi
COMMIT="$(git rev-parse --short HEAD)"

# ---------- 2. Номер версии ----------
if [ -z "$VERSION" ]; then
  VERSION="$(date +%Y.%m.%d)"
  n=2
  BASE="$VERSION"
  while [ -f "$RELEASES/GasQueue-$VERSION-Mac.zip" ]; do VERSION="$BASE.$n"; n=$((n + 1)); done
fi
BUILD_NUMBER="$(git rev-list --count HEAD)"
echo "== Версия $VERSION (сборка $BUILD_NUMBER, коммит $COMMIT)"

# ---------- Музыка ----------
MUSIC_DIR="$PROJECT/Assets/Resources/RaceMusic"
STASH="$PROJECT/Builds/.music_stash"
restore_music() {
  if [ -d "$STASH" ]; then
    # Возвращаем треки на место (вместе с .meta, чтобы Unity не переимпортировала их заново)
    find "$STASH" -mindepth 1 -maxdepth 1 -exec mv {} "$MUSIC_DIR/" \;
    rmdir "$STASH" 2>/dev/null || true
    echo "== Треки возвращены в Assets/Resources/RaceMusic."
  fi
}
TRACKS=$(find "$MUSIC_DIR" -type f \( -iname '*.mp3' -o -iname '*.ogg' -o -iname '*.wav' \) 2>/dev/null | wc -l | tr -d ' ')
if [ "$WITH_MUSIC" = 0 ] && [ "$TRACKS" != "0" ]; then
  echo "== Сборка без своей музыки: временно убираю $TRACKS трек(ов) из проекта..."
  mkdir -p "$STASH"
  trap restore_music EXIT
  find "$MUSIC_DIR" -mindepth 1 -maxdepth 1 ! -name 'README.txt' ! -name 'README.txt.meta' -exec mv {} "$STASH/" \;
elif [ "$TRACKS" != "0" ]; then
  echo "== В сборку войдёт своя музыка: $TRACKS трек(ов) («Гонка FM»)."
  echo "   Это чужие треки: давайте такую сборку только своим. Для публичного выпуска — флаг --no-music."
fi

# ---------- 3. Сборка, подпись, архив ----------
echo "== Собираю (несколько минут)..."
export GASQUEUE_VERSION="$VERSION"
export GASQUEUE_BUILD="$BUILD_NUMBER"
bash "$PROJECT/Tools/build_mac.sh"

ZIP="$PROJECT/Builds/Mac/GasQueue-Mac.zip"
if [ ! -f "$ZIP" ]; then
  echo "Не нашёл архив $ZIP — сборка или упаковка не удались."
  exit 1
fi

# ---------- 4. Что нового ----------
LAST_FILE="$RELEASES/.last_release_commit"
NOTES="$RELEASES/GasQueue-$VERSION — что нового.txt"
{
  echo "Симулятор очереди на заправку — версия $VERSION"
  echo "Сборка $BUILD_NUMBER, $(date '+%d.%m.%Y %H:%M')"
  [ "$WITH_MUSIC" = 1 ] && [ "$TRACKS" != "0" ] && echo "Своя музыка в гонке: $TRACKS трек(ов)"
  echo
  echo "Что нового:"
  if [ -f "$LAST_FILE" ] && git cat-file -e "$(cat "$LAST_FILE")" 2>/dev/null; then
    git log --no-merges --format='• %s' "$(cat "$LAST_FILE")..HEAD"
  else
    git log --no-merges --format='• %s' -15
  fi
} > "$NOTES"

FINAL="$RELEASES/GasQueue-$VERSION-Mac.zip"
mv -f "$ZIP" "$FINAL"
echo "$COMMIT" > "$LAST_FILE"

echo
echo "Готово! Новая версия $VERSION:"
echo "  архив:       $FINAL"
echo "  что нового:  $NOTES"
open "$RELEASES" 2>/dev/null || true
