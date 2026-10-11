# Исправление 01 — ошибка запуска CharacterBuilder

На скриншоте: Unity 2021.3.38f1, Android, Stage1, Play Mode.
Первая ошибка: CreateImpl is not allowed to be called from a MonoBehaviour constructor (or instance field initializer).
Затем две NullReferenceException в CharacterBuilder.Paint, строка 35 исходной версии.

В исходном коде MaterialPropertyBlock создавался при инициализации поля MonoBehaviour. Его создание перенесено в Awake, до загрузки материала и создания мешей. Paint использует этот экземпляр после Awake. Остальной runtime-код, GUID, сцена и настройки не изменены.

Как обновить существующий проект:
1. Остановить Play Mode.
2. Из нового архива скопировать только Assets/LowPolyStage1/Scripts/CharacterBuilder.cs поверх одноимённого файла проекта. .meta менять не нужно.
3. Дождаться компиляции, очистить старые сообщения Console и снова нажать Play.
4. Проверить отсутствие CreateImpl и NullReferenceException; должен открыться экран кастомизации.
5. Изменить цвет одежды, нажать «Готово», проверить появление персонажа и локации. Остановить и снова запустить Play.

Выполнены просмотр исходной строки ошибки и минимального diff, проверка того, что создание native-ресурса теперь находится в Awake. Другие new Material/Mesh/Texture/RenderTexture/GameObject в runtime-коде находятся внутри методов, а не в инициализаторах полей.
Unity Editor здесь недоступен: повторный Play Mode, компиляция и Android-сборка после исправления не выполнены. Причина поддержана скриншотом и кодом; устранение сбоя в вашем Editor требует повторного запуска.
