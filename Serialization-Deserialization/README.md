# Тестовое задание Saber Interactive на должность Gameplay Programmer  
## Задание 1
Реализуйте функции сериализации и десериализации двусвязного списка, заданного следующим образом:  
```csharp
class ListNode
{
    public ListNode Prev;
    public ListNode Next;
    public ListNode Rand; // произвольный элемент внутри списка
    public string Data;
}


class ListRand
{
    public ListNode Head;
    public ListNode Tail;
    public int Count;

    public void Serialize(FileStream s)
    {
    }

    public void Deserialize(FileStream s)
    {
    }
}
```
__Примечание__:  
Cериализация подразумевает сохранение и восстановление полной структуры списка, включая взаимное соотношение его элементов между собой. Формат сериализованного файла любой.

* Нельзя изменять исходную структуру классов ListNode, ListRand.
* Алгоритмическая сложность решения должна быть меньше квадратичной.
* Для выполнения задания можно использовать любой общеиспользуемый язык.
* Тест нужно выполнить __без__ использования библиотек/стандартных средств сериализации:
    * `BinaryFormatter`
    * `XmlSerializer`
    * `DataContractSerializer`
    * `Newtonsoft.Json`
    * `System.Text.Json`