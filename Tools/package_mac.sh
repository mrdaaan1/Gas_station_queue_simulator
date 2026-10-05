#!/bin/bash
# Подготовить собранную игру к отправке друзьям:
#  - права на запуск, иконка, заново подписать (ad-hoc) — иначе после правок внутри .app
#    macOS у друзей пишет «Не удаётся открыть программу»;
#  - архив <Имя>-Mac.zip с игрой и инструкцией «Как запустить.txt».
# Запуск:  bash Tools/package_mac.sh [путь/к/игре.app]   (по умолчанию Builds/Mac/GasQueue.app)
set -e
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
APP="${1:-$PROJECT/Builds/Mac/GasQueue.app}"
APP="$(cd "$(dirname "$APP")" && pwd)/$(basename "$APP")"
if [ ! -d "$APP" ]; then echo "Не нашёл приложение: $APP"; exit 1; fi

# Иконка
if [ -f "$PROJECT/Tools/Icon/AppIcon.icns" ]; then
  cp "$PROJECT/Tools/Icon/AppIcon.icns" "$APP/Contents/Resources/PlayerIcon.icns"
fi
# Права на запуск и подпись
chmod -R u+rwX,go+rX "$APP"
chmod +x "$APP/Contents/MacOS/"*
xattr -cr "$APP"
codesign --force --deep --sign - "$APP"
codesign --verify --deep "$APP" && echo "Подпись в порядке."
touch "$APP"

# Архив: игра + инструкция
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
rm -f "$OUT/$NAME-Mac.zip"
ditto -c -k --keepParent "$STAGE" "$OUT/$NAME-Mac.zip"
rm -rf "$(dirname "$STAGE")"
echo "Архив для друзей: $OUT/$NAME-Mac.zip"
open "$OUT"
