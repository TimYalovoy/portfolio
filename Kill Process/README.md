# Kill Process - Утилита управления процессами Unity

## Описание задачи (Problem Statement)

### Что нужно было сделать?

Требовалось создать инструмент для редактора Unity, который мог бы автоматически находить и завершать процессы Unity, содержащие определенные модули (например, DLL-библиотеки). Проблема возникала при разработке, когда несколько экземпляров Unity Editor запускались с подключенными внешними модулями, и некоторые из них оставались "висеть" в памяти после закрытия, блокируя ресурсы или создавая конфликты.

### Почему это было важно?

Оставшиеся процессы Unity с подключенными модулями могли:
- Блокировать файлы и ресурсы, необходимые для работы текущего экземпляра редактора
- Создавать конфликты при работе с внешними библиотеками
- Занимать системную память и замедлять работу системы
- Усложнять процесс отладки и разработки

Автоматизация поиска и завершения таких процессов значительно упрощала рабочий процесс разработчика.

## Решение (Solution)

### Как была решена задача?

Реализован ScriptableObject-класс `KillProcessScriptable`, который:

1. **Определяет текущий процесс Unity** — при инициализации сохраняет ID и имя главного процесса Unity Editor
2. **Ищет процессы по модулям** — сканирует все запущенные процессы с тем же именем, что и текущий Unity, и проверяет их подключенные модули (DLL)
3. **Фильтрует по шаблону** — находит процессы, содержащие модули, имя или путь которых соответствует заданному шаблону (например, `CustomHardware.dll`)
4. **Помечает для завершения** — сохраняет ID найденных процессов в список
5. **Завершает процессы** — по команде завершает все помеченные процессы

Решение использует `System.Diagnostics.Process` для работы с процессами операционной системы и их модулями.

### Альтернативы и обоснование выбора

Рассматривался вариант использования командной строки (taskkill), но прямой доступ через `System.Diagnostics` дает больше контроля и информации о процессах. ScriptableObject выбран для удобной настройки через Inspector Unity и возможности сохранения конфигурации как asset.

## Ключевые особенности и демонстрируемые навыки

- **Работа с процессами операционной системы** — использование `System.Diagnostics.Process` для получения информации о процессах и их модулях
- **Создание инструментов для Unity Editor** — разработка ScriptableObject-based утилит с настройками через Inspector
- **Безопасная фильтрация процессов** — исключение главного процесса Unity из списка на завершение
- **Логирование и отладка** — подробное логирование процесса поиска и результатов для диагностики
- **Работа с коллекциями модулей** — эффективный обход `ProcessModuleCollection` для поиска нужных DLL

## Структура кода в папке (Code Overview)

- **`KillProcessScriptable.cs`** — основной класс, реализующий всю логику поиска и завершения процессов
  - `Awake()` — инициализация, определение текущего процесса Unity
  - `MarkProcessesToKillByNamePartOfConnectedModule()` — поиск процессов по шаблону модуля
  - `KillMarkedProcesses()` — завершение всех помеченных процессов

## Примеры использования (Usage Examples)

### Пример 1: Инициализация и настройка

```csharp
// Создается ScriptableObject asset через меню: Tools/Kill Process Scriptable
// В Inspector настраивается:
// - moduleNamePart: "CustomHardware.dll" (шаблон для поиска модулей)

[CreateAssetMenu(fileName = "KillProcessScriptable", menuName = "Tools/Kill Process Scriptable")]
public class KillProcessScriptable : ScriptableObject
{
    [Header("Текущий (главный) процесс Unity")]
    [SerializeField, Disabled] private int unityProcessID = -1;
    [SerializeField, Disabled] private string unityProcessName = string.Empty;

    [Header("Шаблон поиска")]
    public string moduleNamePart = "CustomHardware.dll";
}
```

### Пример 2: Поиск процессов по модулям

```csharp
public void MarkProcessesToKillByNamePartOfConnectedModule()
{
    Process[] allProcesses = Process.GetProcesses();
    
    // Находим все процессы с тем же именем, что и текущий Unity
    var matchedProcesses = allProcesses
        .Where(p => p.Id != unityProcessID && 
                    p.ProcessName.ToLower().Equals(unityProcessName.ToLower()))
        .ToList();

    // Проверяем модули каждого процесса
    foreach (var process in matchedProcesses)
    {
        ProcessModuleCollection unityModules = process.Modules;
        foreach (ProcessModule module in unityModules)
        {
            // Если модуль содержит искомый шаблон - помечаем процесс
            if (module.ModuleName.ToLower().Contains(moduleNamePart.ToLower()) || 
                module.FileName.ToLower().Contains(moduleNamePart.ToLower()))
            {
                processesToKill.Add(process.Id);
                break;
            }
        }
    }
}
```

### Пример 3: Завершение процессов

```csharp
public void KillMarkedProcesses()
{
    if (processesToKill.Count > 0)
    {
        foreach (var processId in processesToKill)
        {
            var process = Process.GetProcessById(processId);
            process.Kill(); // Принудительное завершение процесса
        }
        processesToKill.Clear();
        Debug.Log($"Все отмеченные процессы на завершение - завершены.");
    }
}
```

## Дополнительные детали

- **Безопасность:** Код исключает завершение текущего процесса Unity (`p.Id != unityProcessID`), что предотвращает случайное завершение активного редактора
- **Логирование:** Подробное логирование помогает понять, какие процессы были найдены и почему они были помечены на завершение
- **Производительность:** Использование LINQ для фильтрации процессов и ранний выход из циклов оптимизирует производительность при большом количестве процессов

---

**Технологии:** Unity, C#, System.Diagnostics, ScriptableObject, Unity Editor Tools
