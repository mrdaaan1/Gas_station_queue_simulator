#!/bin/bash
# Подготовить собранную игру к отправке друзьям:
#  - права на запуск, иконка, заново подписать (ad-hoc) — иначе после правок внутри .app
#    macOS у друзей пишет «Не удаётся открыть программу»;
#  - архив <Имя>-Mac.zip с игрой и инструкцией «Как запустить.txt».
# Запуск:  bash Tools/package_mac.sh [путь/к/игре.app]   (по умолчанию Builds/Mac/GasQueue.app)
set -e
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
APP="$1"
if [ -z "$APP" ]; then
  # Без аргумента — самое свежее .app в Builds/Mac (как бы его ни переименовали)
  APP="$(ls -dt "$PROJECT"/Builds/Mac/*.app 2>/dev/null | head -1)"
fi
if [ -z "$APP" ] || [ ! -d "$APP" ]; then
  echo "Не нашёл игру (.app) в $PROJECT/Builds/Mac."
  echo "Положите туда собранную игру или укажите путь: перетащите .app в окно Терминала после команды."
  exit 1
fi
APP="$(cd "$(dirname "$APP")" && pwd)/$(basename "$APP")"
echo "Игра: $APP"

echo "1/4 Ставлю иконку..."
if [ -f "$PROJECT/Tools/Icon/AppIcon.icns" ]; then
  cp "$PROJECT/Tools/Icon/AppIcon.icns" "$APP/Contents/Resources/PlayerIcon.icns"
fi
echo "2/4 Права на запуск и подпись (несколько секунд)..."
chmod -R u+rwX,go+rX "$APP"
chmod +x "$APP/Contents/MacOS/"*
xattr -cr "$APP"
codesign --force --deep --sign - "$APP"
codesign --verify --deep "$APP" && echo "Подпись в порядке."
touch "$APP"

echo "3/4 Готовлю архив с инструкцией..."
NAME="$(basename "$APP" .app)"
OUT="$(dirname "$APP")"
STAGE="$(mktemp -d)/$NAME"
mkdir -p "$STAGE"
ditto "$APP" "$STAGE/$(basename "$APP")"
cat > "$STAGE/Как запустить.txt" <<TXT
Как запустить игру на Mac

1. Перетащите «$(basename "$APP")» в папку «Программы» (или оставьте в «Загрузках»).
2. Откройте игру двойным кликом. macOS скажет, что не может проверить разработчика — нажмите «Готово».
3. Откройте «Системные настройки» → «Конфиденциальность и безопасность», пролистайте вниз
   и нажмите «Всё равно открыть» напротив игры. Подтвердите паролем.

Если не помогло — откройте Терминал и выполните (путь поправьте, если игра лежит не в «Загрузках»):

   xattr -cr ~/Downloads/"$(basename "$APP")"

и снова откройте игру.

Управление: W/S — газ/тормоз, A/D — руль, F — выйти из машины, Tab — подсказки, Esc — пауза.
TXT
# Та же инструкция страницей с кнопками «Скопировать» (Tools/mac_howto.html)
if [ -f "$PROJECT/Tools/mac_howto.html" ]; then
  {
    echo '<!doctype html><html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">'
    echo '</head><body>'
    cat "$PROJECT/Tools/mac_howto.html"
    echo '</body></html>'
  } > "$STAGE/Как запустить.html"
fi
rm -f "$OUT/$NAME-Mac.zip"
echo "4/4 Упаковываю (это самый долгий шаг)..."
ditto -c -k --keepParent "$STAGE" "$OUT/$NAME-Mac.zip"
rm -rf "$(dirname "$STAGE")"
echo "Готово! Архив для друзей: $OUT/$NAME-Mac.zip"
open "$OUT"
