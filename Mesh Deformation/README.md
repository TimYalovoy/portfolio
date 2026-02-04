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

2. **GPU-ускорение через Compute Shaders** — реализация `MeshDeformerWithPhysicsByComputeShader` использует:
   - **GraphicsBuffer** для передачи данных вершин на GPU
   - **Compute Shader** (`VertexDisplacement.compute`) для параллельной обработки вершин
   - **ComputeBuffer (Constant Buffer)** для передачи настроек деформации одним блоком

3. **Физическая модель** в HLSL:
   - **Spring Force** — сила возврата вершин в исходное положение
   - **Damping** — демпфирование для затухания колебаний
   - **Velocity** — учёт скорости движения вершин для плавности
   - **Ограничение смещения** — `maxDistance` ограничивает максимальное смещение вершины

4. **Система воздействий** — поддержка нескольких точек деформации:
   - Точки добавляются через `AddDeformingForce()`
   - Радиусы `contactRadius`, `closestToContactRadius` задают область влияния
   - Сила затухает с расстоянием (in-radius / out-radius)

5. **Настройки деформации** — гибкая система через `DeformSettings` и `DeformSettingsCompute`:
   - Силы пружины, демпфирования, воздействия
   - Радиусы влияния
   - Пороги фильтрации по углам (dotVal)
   - Масштабирование и `maxDistance`

### Альтернативы и обоснование выбора

- **CPU-обработка** — отложена из-за низкой производительности при большом числе вершин
- **Vertex Shader** — не подходит: требует обновления меша каждый кадр и не поддерживает состояние (velocity)
- **Compute Shader** — выбран: параллельная обработка на GPU, поддержка состояния, высокая производительность

## Ключевые особенности и демонстрируемые навыки

- **Compute Shaders и GPU-вычисления** — HLSL, GraphicsBuffer, ComputeBuffer, Constant Buffer
- **Оптимизация** — перенос вычислений на GPU, использование `cbuffer` для параметров
- **Архитектурное проектирование** — интерфейсы и абстрактные классы для расширяемости
- **Физическое моделирование** — пружинная физика с демпфированием и ограничением смещения
- **Совпадение структур CPU/GPU** — `StructLayout`, `UnsafeUtility` для передачи данных
- **Управление ресурсами** — освобождение буферов через `IDisposable`

## Структура кода в папке (Code Overview)

| Файл | Назначение |
|------|------------|
| `IMeshDeformer.cs` | Интерфейс контракта для реализаций деформации |
| `MeshDeformer.cs` | Абстрактный базовый класс (вершины, нормали, точки воздействия) |
| `MeshDeformerWithPhysicsByComputeShader.cs` | Реализация с GPU-ускорением через Compute Shader |
| `VertexDisplacement.compute` | HLSL Compute Shader для смещения вершин |
| `DeformSettings.cs` | Класс настроек деформации с валидацией |
| `DeformSettingsCompute.cs` | Структура настроек для GPU (Constant Buffer) |
| `VertexData.cs` | Структура вершины для GPU (original, displaced, velocity, normal) |

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
    if (deformingForcePoints.Count == 0)
    {
        if (deformBuffer != null)
            deformBuffer.Release();
    }

    compute.SetInt("deformCount", deformingForcePoints.Count);

    deformBuffer = new GraphicsBuffer(
        GraphicsBuffer.Target.Structured,
        deformingForcePoints.Count,
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

### Пример 4: Структура VertexDisplacement.compute (HLSL)

```hlsl
#pragma kernel ComputeVertexDisplacement

struct VertexData
{
    float3 original;
    float3 displaced;
    float3 velocity;
    float3 normal;
};

cbuffer DeformSettingsCompute
{
    float maxDistance;
    float deltaTime;
    float springForce;
    float edgeSpringForce;
    float damping;
    float contactRadius;
    float force;
    float closestToContactRadius;
    float edgeForce;
    float outForce;
    float scale;
    // ... dotVal-параметры
};

uint deformCount;
StructuredBuffer<float3> deformingForcePoints;
RWStructuredBuffer<VertexData> vertices;

[numthreads(4, 1, 1)]
void ComputeVertexDisplacement(uint id : SV_DispatchThreadID)
{
    VertexData v = vertices[id];

    // Применение сил от точек воздействия
    for (uint i = 0; i < deformCount; i++)
    {
        float3 deformPoint = deformingForcePoints[i];
        float3 dir = v.displaced - deformPoint;
        float dist = length(dir * scale);
        float inRadius = step(contactRadius, dist);
        v.velocity += normalize(dir) * (lerp(force, outForce, inRadius) / (dist * dist + eps)) * deltaTime;
    }

    // Пружина и демпфирование
    float3 displacement = (v.displaced - v.original) * scale;
    v.velocity -= displacement * springForce * deltaTime;
    v.velocity *= 1.0 - damping * deltaTime;
    v.displaced += v.velocity * deltaTime;

    // Ограничение максимального смещения
    float len = length(v.displaced - v.original);
    v.displaced = len <= maxDistance ? v.displaced : v.original + normalize(v.displaced - v.original) * maxDistance;

    vertices[id] = v;
}
```

### Пример 5: DeformSettingsCompute и Constant Buffer

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct DeformSettingsCompute
{
    public float maxDistance;
    public float deltaTime;
    public float springForce;
    public float edgeSpringForce;
    public float damping;
    public float dotValExcludeForce;
    public float dotValMultipliedForce;
    public float dotValEdgeForce;
    public float contactRadius;
    public float force;
    public float closestToContactRadius;
    public float edgeForce;
    public float outForce;
    public float scale;

    public void CopyFrom(DeformSettings settings)
    {
        maxDistance = settings.maxDistance;
        deltaTime = Time.fixedDeltaTime;
        springForce = settings.springForce;
        // ... копирование остальных полей
        scale = settings.Scale;
    }
}
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

**Технологии:** Unity, C#, Compute Shaders, HLSL, GraphicsBuffer, ComputeBuffer, Constant Buffer, GPU Computing, физическое моделирование
