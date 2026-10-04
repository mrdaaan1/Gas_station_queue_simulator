#!/bin/bash
# Поставить иконку на уже собранную игру (без пересборки) и заново упаковать архив для друзей.
# Запуск:  bash Tools/set_mac_icon.sh            (по умолчанию Builds/Mac/GasQueue.app)
#          bash Tools/set_mac_icon.sh путь/к/игре.app
set -e
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
APP="${1:-$PROJECT/Builds/Mac/GasQueue.app}"
if [ ! -d "$APP" ]; then echo "Не нашёл приложение: $APP"; exit 1; fi
cp "$PROJECT/Tools/Icon/AppIcon.icns" "$APP/Contents/Resources/PlayerIcon.icns"
touch "$APP"   # чтобы Finder перечитал иконку
cd "$(dirname "$APP")"
NAME="$(basename "$APP" .app)"
rm -f "$NAME-Mac.zip"
ditto -c -k --keepParent "$(basename "$APP")" "$NAME-Mac.zip"
echo "Иконка установлена: $APP"
echo "Архив для друзей обновлён: $(pwd)/$NAME-Mac.zip"
open "$(pwd)"
