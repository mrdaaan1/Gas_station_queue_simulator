#!/bin/bash
# Собрать игру для Mac из Терминала, не открывая Unity.
# Перед запуском закройте Unity с этим проектом. Запуск:  bash Tools/build_mac.sh
set -e
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION=$(grep 'm_EditorVersion:' "$PROJECT/ProjectSettings/ProjectVersion.txt" | awk '{print $2}')
UNITY="/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity"
if [ ! -x "$UNITY" ]; then
  # Нужной версии нет — берём последнюю установленную через Unity Hub
  UNITY=$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | tail -1)
fi
if [ -z "$UNITY" ] || [ ! -x "$UNITY" ]; then
  echo "Не нашёл Unity в /Applications/Unity/Hub/Editor. Установите Unity через Unity Hub."; exit 1
fi
mkdir -p "$PROJECT/Builds"
echo "Собираю игру через $UNITY ... (несколько минут, окно Unity не откроется)"
if "$UNITY" -batchmode -quit -projectPath "$PROJECT" -executeMethod GameBuilder.BuildMacBatch -logFile "$PROJECT/Builds/build_mac.log"; then
  cd "$PROJECT/Builds/Mac"
  rm -f GasQueue-Mac.zip
  ditto -c -k --keepParent GasQueue.app GasQueue-Mac.zip
  echo "Готово! Игра: $PROJECT/Builds/Mac/GasQueue.app"
  echo "Архив для друзей: $PROJECT/Builds/Mac/GasQueue-Mac.zip"
  open "$PROJECT/Builds/Mac"
else
  echo "Сборка не удалась. Лог: $PROJECT/Builds/build_mac.log"
  echo "Последние ошибки:"; grep -iE "error|exception" "$PROJECT/Builds/build_mac.log" | tail -20
  exit 1
fi
