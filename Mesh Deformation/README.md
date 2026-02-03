# Mesh Deformation - Система деформации мешей с физикой на GPU

## Описание задачи (Problem Statement)

### Что нужно было сделать?

Требовалось реализовать систему интерактивной деформации 3D-мешей в реальном времени с физической симуляцией. Меш должен был реагировать на внешние воздействия (например, касания или столкновения), деформироваться с учетом физических свойств материала (упругость, демпфирование) и плавно возвращаться в исходное состояние. Система должна была работать с высокой производительностью, обрабатывая тысячи вершин каждый кадр без падения FPS.

### Почему это было важно?

Интерактивная деформация мешей необходима для:
- Создания реалистичной симуляции мягких тканей (например, в медицинских симуляторах)
- Интерактивных объектов, реагирующих на прикосновения
- Визуализации физических воздействий в реальном времени
- Улучшения пользовательского опыта через тактильную обратную связь

Высокая производительность критична, так как деформация должна происходить в реальном времени без задержек, особенно при работе с детализированными мешами.

## Решение (Solution)

### Как была решена задача?

Реализована двухуровневая система деформации мешей:

1. **Архитектура на основе интерфейсов** — создан `IMeshDeformer` интерфейс и абстрактный класс `MeshDeformer` для единообразного API различных реализаций

2. **GPU-ускорение через Compute Shaders** — основная реализация `MeshDeformerWithPhysicsByComputeShader` использует:
   - **GraphicsBuffer** для передачи данных вершин на GPU
   - **Compute Shader** для параллельной обработки всех вершин одновременно
   - **Constant Buffer** для передачи параметров деформации

3. **Физическая модель** — реализована пружинная физика:
   - **Spring Force** — сила, возвращающая вершины в исходное положение
   - **Damping** — демпфирование для предотвращения колебаний
   - **Velocity** — учет скорости движения вершин для плавности

4. **Система воздействий** — поддержка множественных точек деформации:
   - Точки воздействия добавляются через `AddDeformingForce()`
   - Радиусы воздействия (`contactRadius`, `closestToContactRadius`) определяют область влияния
   - Фильтрация по нормалям через скалярное произведение для более точного контроля

5. **Настройки деформации** — гибкая система параметров через `DeformSettings`:
   - Силы пружины, демпфирования, воздействия
   - Радиусы влияния
   - Пороги фильтрации по углам
   - Масштабирование

### Альтернативы и обоснование выбора

Рассматривались варианты:
- **CPU-обработка** — отложена из-за низкой производительности при большом количестве вершин
- **Vertex Shader** — не подходит, так как требует обновления меша каждый кадр и не поддерживает физику с состоянием
- **Compute Shader** — выбран как оптимальное решение: параллельная обработка на GPU, поддержка состояния (velocity), высокая производительность

## Ключевые особенности и демонстрируемые навыки

- **Работа с Compute Shaders и GPU-вычислениями** — использование GraphicsBuffer, ComputeBuffer и HLSL для высокопроизводительных операций
- **Оптимизация производительности** — перенос вычислений на GPU для обработки тысяч вершин параллельно
- **Архитектурное проектирование** — использование интерфейсов и абстрактных классов для расширяемости и переиспользования кода
- **Физическое моделирование** — реализация пружинной физики с демпфированием и учетом скорости
- **Работа с нативными структурами** — использование `StructLayout` и `UnsafeUtility` для эффективной передачи данных между CPU и GPU
- **Управление ресурсами** — правильное освобождение GraphicsBuffer через `IDisposable`

## Структура кода в папке (Code Overview)

- **`IMeshDeformer.cs`** — интерфейс, определяющий контракт для всех реализаций деформации
- **`MeshDeformer.cs`** — абстрактный базовый класс с общей логикой (хранение вершин, нормалей, точек воздействия)
- **`MeshDeformerWithPhysicsByComputeShader.cs`** — основная реализация с GPU-ускорением через Compute Shader
- **`DeformSettings.cs`** — класс настроек деформации с валидацией и методами передачи параметров в Compute Shader
- **`DeformSettingsCompute.cs`** — структура для передачи настроек в Compute Shader (оптимизирована для GPU)
- **`VertexData.cs`** — структура данных вершины для передачи на GPU (original, displaced, velocity, normal)

## Примеры использования (Usage Examples)

### Пример 1: Инициализация деформатора

```csharp
public class MeshDeformerWithPhysicsByComputeShader : MeshDeformer, IDisposable
{
    private GraphicsBuffer vertexBuffer;
    private ComputeShader compute;
    private int kernel;
    
    public MeshDeformerWithPhysicsByComputeShader(Deformer deformer) : base(deformer)
    {
        var mesh = meshDeformTarget.MeshFilter.mesh;
        vertexCount = mesh.vertexCount;
        
        // Сохраняем оригинальные вершины
        originalVertices = new Vector3[vertexCount];
        mesh.vertices.CopyTo(originalVertices, 0);
        
        // Инициализируем данные вершин
        vertexData = new VertexData[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            vertexData[i].original = originalVertices[i];
            vertexData[i].displaced = originalVertices[i];
            vertexData[i].velocity = Vector3.zero;
            vertexData[i].normal = normals[i];
        }
        
        // Создаем буфер для GPU
        vertexBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            vertexCount,
            UnsafeUtility.SizeOf<VertexData>()
        );
        vertexBuffer.SetData(vertexData);
        
        // Настраиваем Compute Shader
        compute = deformer.compute;
        kernel = compute.FindKernel("ComputeVertexDisplacement");
        compute.SetBuffer(kernel, "vertices", vertexBuffer);
    }
}
```

### Пример 2: Добавление точки деформации

```csharp
public override void AddDeformingForce(Vector3 point)
{
    // Преобразуем мировые координаты в локальные координаты меша
    deformingForcePoints.Add(meshDeformTarget.transform.InverseTransformPoint(point));
}

// Использование:
meshDeformer.AddDeformingForce(contactPoint); // contactPoint в мировых координатах
```

### Пример 3: Обновление деформации каждый кадр

```csharp
public override void Update()
{
    // Создаем буфер для точек воздействия
    deformBuffer = new GraphicsBuffer(
        GraphicsBuffer.Target.Structured,
        deformingForcePoints.Count == 0 ? 6 : deformingForcePoints.Count,
        UnsafeUtility.SizeOf<Vector3>()
    );
    deformBuffer.SetData(deformingForcePoints);
    
    // Передаем параметры в Compute Shader
    compute.SetInt("deformCount", deformingForcePoints.Count);
    compute.SetBuffer(kernel, "deformingForcePoints", deformBuffer);
    
    // Запускаем вычисления на GPU
    int threadGroups = Mathf.CeilToInt(vertexCount / 4f);
    compute.Dispatch(kernel, threadGroups, 1, 1);
    
    // Получаем результаты обратно на CPU
    vertexBuffer.GetData(vertexData);
    for (int i = 0; i < vertexCount; i++)
        displacedVertices[i] = vertexData[i].displaced;
    
    // Применяем изменения к мешу
    meshDeformTarget.MeshFilter.mesh.vertices = displacedVertices;
    meshDeformTarget.MeshFilter.mesh.RecalculateNormals();
    
    deformingForcePoints.Clear();
}
```

### Пример 4: Настройки деформации

```csharp
[Serializable]
public class DeformSettings
{
    [Header("Spring")]
    public float springForce = 20f;        // Сила возврата в исходное положение
    public float edgeSpringForce = 15f;    // Дополнительная сила для краев
    public float damping = 5f;             // Демпфирование колебаний
    
    [Header("Main Force")]
    public float force = 10f;              // Основная сила воздействия
    public float contactRadius = 0.012f;  // Радиус прямого воздействия
    public float outForce = 1f;            // Сила для вершин вне радиуса
    
    [Header("Filtering")]
    [Range(-1f, 0.5f)]
    public float dotValExcludeForce = -.08f;      // Порог исключения вершины
    public float dotValMultipliedForce = -.85f;   // Порог увеличенной силы
    public float dotValEdgeForce = -.08f;         // Порог рассеивающей силы
}
```

### Пример 5: Структура данных для GPU

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct VertexData
{
    public Vector3 original;   // Исходная позиция вершины
    public Vector3 displaced;  // Текущая деформированная позиция
    public Vector3 velocity;    // Скорость движения вершины (для физики)
    public Vector3 normal;      // Нормаль вершины (для фильтрации)
}

// Использование UnsafeUtility для определения размера структуры
vertexBuffer = new GraphicsBuffer(
    GraphicsBuffer.Target.Structured,
    vertexCount,
    UnsafeUtility.SizeOf<VertexData>() // Автоматический расчет размера
);
```

### Пример 6: Освобождение ресурсов

```csharp
public void Dispose()
{
    // Важно освобождать GraphicsBuffer для предотвращения утечек памяти
    if (vertexBuffer != null)
        vertexBuffer.Release();
    if (deformBuffer != null)
        deformBuffer.Release();
}

// Использование:
using (var deformer = new MeshDeformerWithPhysicsByComputeShader(deformer))
{
    // работа с деформатором
} // автоматическое освобождение ресурсов
```

## Дополнительные детали

- **Производительность:** Обработка на GPU позволяет деформировать меши с десятками тысяч вершин при стабильных 60 FPS
- **Фильтрация по нормалям:** Система использует скалярное произведение нормали вершины и направления воздействия для более реалистичной деформации (вершины, "смотрящие" от точки контакта, получают меньшее воздействие)
- **Множественные точки воздействия:** Система поддерживает одновременную обработку нескольких точек деформации, что позволяет создавать сложные интерактивные сценарии
- **Масштабирование:** Поддержка независимого масштабирования (`uniformScale`) и автоматического масштабирования на основе размера объекта
- **Валидация параметров:** Метод `OnValidate()` в `DeformSettings` автоматически корректирует некорректные значения параметров

---

**Технологии:** Unity, C#, Compute Shaders, HLSL, GraphicsBuffer, GPU Computing, Физическое моделирование, Структуры данных для GPU
