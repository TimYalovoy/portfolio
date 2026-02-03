# Knot Detection - Система детекции узлов на веревке

## Описание задачи (Problem Statement)

### Что нужно было сделать?

Требовалось реализовать систему автоматического распознавания узлов на физической веревке в Unity. Веревка моделировалась с помощью физического движка Obi, и необходимо было определить, когда веревка формирует определенные типы узлов (Trefoil, Figure-Eight, Square, Granny, Frictional). Система должна была работать в реальном времени, анализируя самопересечения веревки и определяя тип узла на основе математических принципов теории узлов.

### Почему это было важно?

Детекция узлов необходима для:
- Симуляции обучения завязыванию узлов (например, в медицинских симуляторах)
- Валидации правильности выполнения узла пользователем
- Обратной связи в обучающих приложениях
- Автоматической оценки качества завязанного узла

Точное определение типа узла позволяет системе реагировать на действия пользователя и предоставлять соответствующую обратную связь.

## Решение (Solution)

### Как была решена задача?

Реализован класс `RopeKnotDetector`, который использует **Skein Relation** (скейн-соотношение) из теории узлов для детекции самопересечений веревки:

1. **Анализ самопересечений (Skein Relation)** — каждый кадр проверяются все пары элементов веревки на пересечение:
   - Вычисляется расстояние между центрами элементов
   - Проверяется угол между направлениями элементов (через скалярное произведение)
   - Определяется знак пересечения по осям координат

2. **Структура Intersection** — каждая точка пересечения сохраняется с информацией:
   - Индексы пересекающихся элементов
   - Скалярное произведение направлений
   - Расстояние между элементами
   - Знак пересечения (для определения ориентации)

3. **Алгоритмы распознавания узлов** — для каждого типа узла реализован отдельный алгоритм:
   - **Trefoil Knot** — проверяет последовательность из 3 пересечений с определенной структурой индексов и знаков
   - **Figure-Eight Knot** — анализирует 4 пересечения с центральным элементом
   - Другие типы узлов (Square, Granny, Frictional) — заготовки для будущей реализации

4. **Событийная система** — при обнаружении узла генерируется событие `OnKnotDetected` с индексами начала и конца узла

### Альтернативы и обоснование выбора

Рассматривались варианты:
- **Машинное обучение** — отложен из-за сложности обучения и необходимости большого датасета
- **Анализ топологии через гомологии** — слишком вычислительно затратно для real-time
- **Skein Relation** — выбран как математически обоснованный и эффективный метод, основанный на фундаментальных принципах теории узлов

## Ключевые особенности и демонстрируемые навыки

- **Применение теории узлов в программировании** — использование математических концепций (Skein Relation) для решения практических задач
- **Работа с физическим движком Obi** — интеграция с внешней библиотекой для работы с веревками и мягкими телами
- **Оптимизация производительности** — эффективный алгоритм проверки пересечений с ограничением области поиска (leftIndex, rightIndex)
- **Расширяемая архитектура** — использование делегатов (`knotFindAlgorithm`) для переключения между алгоритмами детекции разных типов узлов
- **Векторная математика** — вычисления скалярных произведений, расстояний и нормалей для определения геометрических свойств пересечений

## Структура кода в папке (Code Overview)

- **`RopeKnotDetector.cs`** — основной класс детектора узлов
  - `Intersection` — структура для хранения данных о пересечении элементов
  - `KnotType` — enum с типами узлов (Trefoil, Figure-Eight, Square, Granny, Frictional)
  - `Update()` — основной цикл проверки пересечений каждый кадр
  - `SkeinRelationCheck()` — проверка пары элементов на пересечение
  - `CheckForTrefoilKnot()` — алгоритм детекции трилистника
  - `CheckForFigureEightKnot()` — алгоритм детекции восьмерки
  - `OnKnotDetected` — событие, вызываемое при обнаружении узла

## Примеры использования (Usage Examples)

### Пример 1: Инициализация детектора

```csharp
public class RopeKnotDetector : MonoBehaviour
{
    [SerializeField] private KnotType knotToDetection;
    [SerializeField] private float distanceThreshold; // Порог расстояния для пересечения
    [Min(0)][SerializeField] private float degresOffset; // Смещение угла для фильтрации

    private ObiRope _rope;
    private ObiSolver _solver;
    
    private void Awake()
    {
        _rope = GetComponent<ObiRope>();
        _solver = transform.parent.GetComponent<ObiSolver>();
        
        // Автоматический расчет порога расстояния на основе радиуса частиц
        var radius = _rope.blueprint.GetParticleMaxRadius(0);
        var diametr = 2 * radius;
        distanceThreshold = diametr + (radius / 2f);
        
        ChangeKnotTypeToDetection(knotToDetection);
    }
}
```

### Пример 2: Проверка Skein Relation (самопересечения)

```csharp
private void SkeinRelationCheck(ObiStructuralElement fEl, ObiStructuralElement sEl, 
                                Action<float, float, int> OnSuccess)
{
    // Получаем геометрические данные элементов
    GetFullDataOfElement(fEl, out var fElCenter, out var fElDirection, out var fElNormal);
    GetFullDataOfElement(sEl, out var sElCenter, out var sElDirection, out var sElNormal);

    var distance = Vector3.Distance(fElCenter, sElCenter);
    
    // Проверка 1: Расстояние должно быть меньше порога
    if (distance < distanceThreshold)
    {
        var dot = Vector3.Dot(fElDirection, sElDirection);
        var absDot = Mathf.Abs(dot);
        
        // Проверка 2: Элементы не должны быть параллельны
        if (!(Mathf.Approximately(absDot, 1f) || absDot >= radiansThreshold))
        {
            // Определяем знак пересечения по осям координат
            var yDiff = fElCenter.y - sElCenter.y;
            var sign = Mathf.Approximately(yDiff, 0f) ? 
                      (Mathf.Approximately(zDiff, 0f) ? 0 : (int)Mathf.Sign(zDiff)) : 
                      (int)Mathf.Sign(yDiff);
            
            OnSuccess(dot, distance, sign); // Пересечение обнаружено
        }
    }
}
```

### Пример 3: Алгоритм детекции Trefoil Knot

```csharp
private void CheckForTrefoilKnot(List<Intersection> intersections)
{
    if (intersections == null || intersections.Count < 3) return;

    // Trefoil knot требует 3 пересечения с определенной структурой:
    // pairs (crosses): (p0, p1), (p2, p3), (p4, p5)
    // sequence: p0 < p2 < p4 < p1 < p3 < p5
    
    var min = sequence.Min();
    var max = sequence.Max();
    
    // Проверка структуры: центральное пересечение должно связывать min и max
    if (intersections[centralIntersectionIndex].secondElementIndex != min) return;
    if (intersections[centralIntersectionIndex].firstElementIndex != max) return;
    
    // Проверка знаков: первое и последнее пересечения должны иметь одинаковый знак,
    // отличный от центрального
    if (!(intersections[0].sign == intersections[^1].sign && 
          intersections[0].sign != intersections[centralIntersectionIndex].sign))
        return;
    
    // Узел обнаружен!
    OnKnotDetected(_rope.elements[min - 1].particle1, 
                   _rope.elements[max + 1].particle2);
}
```

### Пример 4: Подписка на событие обнаружения узла

```csharp
public event Action<int, int> OnKnotDetected = (begin, end) => { };

// В другом классе:
knotDetector.OnKnotDetected += (beginParticleIndex, endParticleIndex) =>
{
    Debug.Log($"Trefoil knot detected! From particle {beginParticleIndex} to {endParticleIndex}");
    // Можно добавить логику: показать сообщение, засчитать очки, и т.д.
};
```

## Дополнительные детали

- **Математическая основа:** Алгоритм основан на Skein Relation из теории узлов — фундаментальном математическом принципе для анализа узлов
- **Производительность:** Поиск пересечений ограничен окном вокруг каждого элемента (leftIndex, rightIndex), что снижает сложность с O(n²) до практически линейной
- **Расширяемость:** Система легко расширяется для новых типов узлов через добавление новых методов проверки и регистрацию их в `ChangeKnotTypeToDetection()`
- **Точность:** Использование порогов расстояния и углов позволяет фильтровать ложные срабатывания и учитывать физические свойства веревки

---

**Технологии:** Unity, C#, Obi Physics, Теория узлов (Knot Theory), Skein Relation, Векторная математика
