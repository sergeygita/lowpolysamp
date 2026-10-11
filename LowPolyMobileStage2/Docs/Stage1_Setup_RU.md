# LowPolyMobile — полный проект этапа 1

**Unity 2021.3.38f1 · URP 12.1.16 · Android API 24+**

## Как открыть

1. Распакуйте архив. Корень проекта — папка **LowPolyMobile**, содержащая `Assets`, `Packages`, `ProjectSettings`.
2. В Unity Hub установите **2021.3.38f1**. Для APK нужны модули **Android Build Support**, **Android SDK & NDK Tools**, **OpenJDK**.
3. В Hub выберите **Open / Add project from disk** и укажите папку **LowPolyMobile**. Не создавайте новый проект и не импортируйте архив как unitypackage.
4. Дождитесь загрузки пакетов и компиляции. Для первого открытия нужен интернет: Unity восстановит URP и зависимости из `Packages/manifest.json`. Кэш пакетов, Unity Editor и Android SDK в архив не включены.
5. При первом успешном импорте редактор применит платформенные настройки через Unity API и откроет `Assets/Scenes/Stage1.unity`. Если открыта другая сцена, используйте **LowPoly → Open Stage1**.
6. Нажмите **Play**. Выберите внешность и нажмите **Готово**.

Настройка сцены и создание материала вручную не требуются. В Edit Mode в сцене находится GameManager. Мир, персонаж, камеры и интерфейс создаются программно при Play — именно так устроена процедурная сцена этого этапа.

## Что включено

- `Assets/Scenes/Stage1.unity`: стартовая сцена с привязанным GameManager; включена в Build Settings.
- `Assets/Settings/LowPolyURP.asset`: URP-профиль, назначенный в Graphics и Quality.
- `Assets/Settings/LowPolyRenderer.asset`: Forward Renderer с разрешёнными ссылками на шейдеры пакета Unity.
- `Assets/Resources/LowPolyDefault.mat` и `Assets/LowPolyStage1/Shaders/FlatColor.shader`.
- 13 runtime C#-классов: игровой режим и callbacks, игроки, команды, персонаж, анимация, камера, ввод, спрайты, мир, кастомизация.
- `Assets/Editor/LowPolyProjectSetup.cs`: первичная настройка и меню открытия сцены/сборки APK. В Android Player этот файл не компилируется.
- `Packages/manifest.json`: фиксированная версия URP и встроенные модули.
- `ProjectSettings`: версия Unity, Graphics, Quality, Android Player Settings, Build Settings, слои, старый Input Manager и Time Manager. Неуказанные стандартные настройки Unity создаст со своими значениями по умолчанию при импорте.
- `.meta` для всех ресурсов и каталогов Assets.
- `Docs/VALIDATION.json`: фактически выполненные проверки и ограничения.

## Настройки

| Параметр | Значение |
|---|---|
| Unity | 2021.3.38f1 |
| URP | 12.1.16, совместимая ветка для Unity 2021.3 |
| Renderer | Forward, Depth Priming Disabled |
| Quality | Low, единственный профиль |
| HDR / MSAA | Off / Disabled |
| Render Scale | 1 |
| Shadow Distance / Cascades | 50 / 1 |
| Soft Shadows / Additional Lights | Off / Disabled |
| Depth Texture / Opaque Texture | Off / Off |
| Color Space | Linear |
| Android minimum SDK | API 24 |
| Android target SDK | Automatic (highest installed) |
| Android backend / architecture | IL2CPP / ARM64 |
| Android graphics API | OpenGLES3 |
| Input | Input Manager (Old) |
| Orientation | Landscape Left |
| Package identifier | com.lowpolystudio.stage1 |
| UI resolution | 1280×720, Scale With Screen Size, Safe Area |
| Player layer / preview layer | Ignore Raycast (2) / CharacterPreview (31) |

Шейдер Unlit не использует внешние текстуры, не принимает и не отбрасывает тени. Настройки теней сохранены для дальнейшего развития проекта. UI использует процедурные Texture2D; превью — RenderTexture.

После первого импорта появляется `ProjectSettings/LowPolySetupVersion.txt`. Он предотвращает повторное перезаписывание ваших настроек при открытии проекта. Команда **LowPoly → Apply Project Settings** вручную повторяет начальную конфигурацию; используйте её только когда хотите вернуть параметры из таблицы.

## Сборка APK

1. **File → Build Settings → Android → Switch Platform**. Активная платформа — локальное состояние Editor; архив не может установить её на другом компьютере заранее.
2. Дождитесь перекомпиляции.
3. **LowPoly → Build Android APK**.
4. Результат: `Builds/Android/LowPolyMobile.apk` — Development Build для тестирования.

Можно также использовать обычные **Build / Build And Run**. Сцена Stage1 уже добавлена. Для CI после активации Unity лицензии и установки Android-модулей:

```bash
/path/to/Unity -batchmode -quit -projectPath /absolute/path/LowPolyMobile -buildTarget Android -executeMethod LowPolyProjectSetup.BuildAndroid -logFile /absolute/path/android-build.log
```

Настройки ключа публикации не добавлены. Для релиза нужны ваш application identifier, signing и отдельная проверка требований магазина. Исходники проекта включены; собранного APK в архиве нет.

## Управление

| Действие | Телефон | Editor |
|---|---|---|
| Движение | Левый джойстик | WASD |
| Бег | Удержание Sprint | Left Shift |
| Прыжок | Jump | Space |
| Камера | Свайп справа вне кнопок | Правая кнопка мыши + движение |
| Action | Кнопка Action | E |
| Команды | Поле вверху + Send | Поле вверху + Send |

Команды: `/help`, `/spawn`, `/heal`, `/skin 0`, `/skin 1`, `/skin 2`, `/color shirt 255 0 0`, `/randomize`, `/hello`.

Части для `/color`: `skin`, `hair`, `shirt`, `pants`, `shoes`, `hat`. RGB — целые числа 0–255. `/skin` выбирает один из трёх процедурных пресетов, а не GTA skin ID.

## Архитектура и границы этапа

GameManager управляет жизненным циклом игроков и вызывает виртуальные методы BaseGameMode. MyGameMode содержит правила по образцу PAWN. При подключении локальный игрок сначала проходит кастомизацию; первый спавн происходит после «Готово». Мир содержит землю 100×100, 18 деревьев, 7 зданий, дорогу и точку спавна.

Это **локальный прототип**, а не готовый сетевой сервер: транспорт, репликация и серверная проверка команд ещё не реализованы. Action демонстрирует вызов `/hello`. Переходы между картами, массовая отрисовка игроков и выпуск в магазин требуют следующего этапа разработки.

## Проверка

В среде подготовки нет Unity Editor, C#-компилятора и Android SDK. Выполнена только статическая проверка: структура проекта, корректность JSON/YAML, GUID и ссылки сцены, материала и URP; настройки Android и состав архива. Успешный импорт Unity, компиляция C#/HLSL, Play Mode и сборка APK **не подтверждены**.

На вашем компьютере проверьте:

1. После загрузки пакетов Console без ошибок, материал не розовый.
2. Play открывает кастомизацию, цвета и типы одежды меняются; «Готово» спавнит игрока.
3. Движение, прыжок, бег и коллизии со зданиями работают; камера приближается перед стеной.
4. Команды с правильными и ошибочными аргументами не вызывают исключения.
5. Повторный Play не создаёт дубликатов Singleton/UI.
6. На телефоне одновременно работают джойстик, Sprint и свайп; после сворачивания ввод не залипает.
7. Development APK собирается и запускается на целевом устройстве.

Официальные источники параметров: [URP Asset 12.1](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.1/manual/universalrp-asset.html), [Android Player Settings 2021.3](https://docs.unity3d.com/2021.3/Documentation/Manual/class-PlayerSettingsAndroid.html), [исходники Unity Graphics для 2021.3](https://github.com/Unity-Technologies/Graphics/tree/2021.3/staging/Packages/com.unity.render-pipelines.universal).
