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
# Проект открыт в Unity — две Unity не могут работать с одним проектом
if pgrep -fil "Unity.app/Contents/MacOS/Unity" | grep -iF -- "$PROJECT" >/dev/null 2>&1; then
  echo "Проект сейчас открыт в Unity. Либо закройте Unity (Cmd+Q) и запустите скрипт снова,"
  echo "либо соберите прямо в Unity: меню Gas Queue → Собрать для Mac (.app)."
  exit 1
fi
# Unity не запущена, но после вылета остался файл-замок — убираем
if [ -f "$PROJECT/Temp/UnityLockfile" ] && ! pgrep -x Unity >/dev/null 2>&1; then
  echo "Удаляю старый замок проекта (Unity не запущена)."
  rm -f "$PROJECT/Temp/UnityLockfile"
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
  if grep -q "another Unity instance is running" "$PROJECT/Builds/build_mac.log" 2>/dev/null; then
    echo "Проект открыт в Unity. Закройте Unity (Cmd+Q) или соберите из меню Gas Queue → Собрать для Mac (.app)."
  else
    echo "Последние ошибки:"; grep -iE "error|exception" "$PROJECT/Builds/build_mac.log" | tail -20
  fi
  exit 1
fi
