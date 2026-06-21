using System.Text;

namespace csharp_solution
{
    class ListNode
    {
        public ListNode Prev;
        public ListNode Next;
        public ListNode Rand;
        public string Data;
    }


    class ListRand
    {
        public ListNode Head;
        public ListNode Tail;
        public int Count;

        public void Serialize(FileStream s)
        {
            using (BinaryWriter writer = new BinaryWriter(s, Encoding.UTF8, true))
            {
                writer.Write(Count);
                
                var nodeMap = new  Dictionary<ListNode, int>();
                var current = Head;
                for (int i = 0; i < Count; i++)
                {
                    nodeMap[current] = i;
                    current = current.Next;
                }
                
                current = Head;
                for (int i = 0; i < Count; i++)
                {
                    writer.Write(current.Data ?? string.Empty);
                    
                    writer.Write(current.Prev != null ? nodeMap[current.Prev] : -1);
                    writer.Write(current.Next != null ? nodeMap[current.Next] : -1);
                    writer.Write(current.Rand != null ? nodeMap[current.Rand] : -1);
                    
                    current = current.Next;
                }
            }
        }

        public void Deserialize(FileStream s)
        {
            using (BinaryReader reader = new BinaryReader(s, Encoding.UTF8, true))
            {
                Count = reader.ReadInt32();
                if (Count == 0)
                {
                    Head = null;
                    Tail = null;
                    return;
                }

                int i = 0;
                var dataArr = new string[Count];
                var prevIndices = new int[Count];
                var nextIndices = new int[Count];
                var randIndices = new int[Count];

                for (i = 0; i < Count; i++)
                {
                    dataArr[i] = reader.ReadString();
                    prevIndices[i] = reader.ReadInt32();
                    nextIndices[i] = reader.ReadInt32();
                    randIndices[i] = reader.ReadInt32();
                }
                
                var nodes = new ListNode[Count];
                for (i = 0; i < Count; i++)
                {
                    nodes[i] = new ListNode { Data = dataArr[i] };
                }

                for (i = 0; i < Count; i++)
                {
                    nodes[i].Prev = prevIndices[i] != -1 ? nodes[prevIndices[i]] : null;
                    nodes[i].Next = nextIndices[i] != -1 ? nodes[nextIndices[i]] : null;
                    nodes[i].Rand = randIndices[i] != -1 ? nodes[randIndices[i]] : null;
                }
                
                Head = nodes[0];
                Tail = nodes[^1];
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                BuiltInTests();
            }
            else
            {
                if (args.Length == 1)
                {
                    ReadExternalTestFile(args[0]);
                }
                else if (args.Length >= 2)
                {
                    string filePath = args[0];
                    string mode = args[1].ToLower();

                    if (mode == "-r")
                    {
                        ReadExternalTestFile(filePath);
                    }
                    else if (mode == "-w")
                    {
                        RunWriteMode(filePath);
                    }
                    else
                    {
                        Console.WriteLine("Invalid mode. Use '-r' for read, '-w' for write.");
                    }
                }
            }
            Console.WriteLine("Press any key, to close program");
            Console.ReadLine();
            return;
        }

        static void BuiltInTests()
        {
            var tests = new List<(string name, Func<bool> test)>
            {
                ("Empty list", TestEmptyList),
                ("One-node without Rand", TestSingleNodeNoRand),
                ("Few nodes with Rand", TestMultipleNodesWithRand),
                ("Validate list structure (self-test)", TestValidateList)
            };

            int passed = 0, failed = 0;
            foreach (var (name, test) in tests)
            {
                Console.Write($"Тест \"{name}\": ");
                if (test())
                {
                    Console.WriteLine("SUCCESS");
                    passed++;
                }
                else
                {
                    Console.WriteLine("FAILED");
                    failed++;
                }
            }
            Console.WriteLine($"\nSummary: {passed} passed, {failed} failed");
        }

        static void ReadExternalTestFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var list = new ListRand();
                list.Deserialize(fs);

                PrintList(list);

                if (ValidateList(list))
                    Console.WriteLine("File is a valid ListRand structure.");
                else
                    Console.WriteLine("Invalid ListRand structure.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during deserialization: {ex.Message}");
            }
        }

        static void RunWriteMode(string filePath)
        {
            var list = new ListRand();
            var nodeList = new List<ListNode>(); // для быстрого доступа по индексу

            Console.WriteLine("Interactive ListRand editor.");
            bool exit = false;
            while (!exit)
            {
                PrintList(list);
                Console.WriteLine("Enter commands:");
                Console.WriteLine("1. Add new Node");
                Console.WriteLine("2. Move Node");
                Console.WriteLine("3. Edit Node");
                Console.WriteLine("4. Remove Node");
                Console.WriteLine("5. Save & Exit");
                Console.Write("Choose: ");
                string choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1": AddNode(list, nodeList); break;
                    case "2": MoveNode(list, nodeList); break;
                    case "3": EditNode(nodeList); break;
                    case "4": RemoveNode(list, nodeList); break;
                    case "5":
                        SaveAndExit(list, filePath, out bool saved);
                        exit = saved;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Try again.");
                        break;
                }
            }
        }

        static void AddNode(ListRand list, List<ListNode> nodeList)
        {
            Console.WriteLine("Adding new node...");
            var newNode = new ListNode();

            Console.Write("Enter Data (empty for empty string): ");
            string data = Console.ReadLine();
            newNode.Data = data ?? string.Empty;

            
            int? prevIndex = ReadIndex("Enter Prev index (empty for null): ");
            
            int? nextIndex = ReadIndex("Enter Next index (empty for null): ");
            
            int? randIndex = ReadIndex("Enter Rand index (empty for null): ");

            if (nodeList.Count == 0)
            {
                newNode.Prev = null;
                newNode.Next = null;
                list.Head = newNode;
                list.Tail = newNode;
            }
            else
            {
                if (prevIndex.HasValue && prevIndex >= 0 && prevIndex < nodeList.Count)
                {
                    var prevNode = nodeList[prevIndex.Value];
                    newNode.Prev = prevNode;
                    newNode.Next = prevNode.Next;
                    prevNode.Next = newNode;
                    if (newNode.Next != null)
                        newNode.Next.Prev = newNode;
                    else
                        list.Tail = newNode;
                }
                else if (nextIndex.HasValue && nextIndex >= 0 && nextIndex < nodeList.Count)
                {
                    var nextNode = nodeList[nextIndex.Value];
                    newNode.Next = nextNode;
                    newNode.Prev = nextNode.Prev;
                    nextNode.Prev = newNode;
                    if (newNode.Prev != null)
                        newNode.Prev.Next = newNode;
                    else
                        list.Head = newNode;
                }
                else
                {
                    var tail = list.Tail;
                    tail.Next = newNode;
                    newNode.Prev = tail;
                    newNode.Next = null;
                    list.Tail = newNode;
                }
            }

            if (randIndex.HasValue && randIndex >= 0 && randIndex < nodeList.Count)
                newNode.Rand = nodeList[randIndex.Value];
            else
                newNode.Rand = null;

            nodeList.Add(newNode);
            list.Count = nodeList.Count;

            Console.WriteLine("Node added successfully.");
        }

        static void MoveNode(ListRand list, List<ListNode> nodeList)
        {
            if (nodeList.Count == 0)
            {
                Console.WriteLine("List is empty.");
                return;
            }

            int? idx = ReadIndex("Enter index of node to move: ");
            if (!idx.HasValue || idx < 0 || idx >= nodeList.Count)
            {
                Console.WriteLine("Invalid index.");
                return;
            }

            var node = nodeList[idx.Value];
            if (node.Prev != null)
                node.Prev.Next = node.Next;
            else
                list.Head = node.Next;

            if (node.Next != null)
                node.Next.Prev = node.Prev;
            else
                list.Tail = node.Prev;

            int? newPrev = ReadIndex("Enter new Prev index (empty for null): ");
            int? newNext = ReadIndex("Enter new Next index (empty for null): ");

            if (newPrev.HasValue && newPrev >= 0 && newPrev < nodeList.Count)
            {
                var prevNode = nodeList[newPrev.Value];
                node.Prev = prevNode;
                node.Next = prevNode.Next;
                prevNode.Next = node;
                if (node.Next != null)
                    node.Next.Prev = node;
                else
                    list.Tail = node;
            }
            else if (newNext.HasValue && newNext >= 0 && newNext < nodeList.Count)
            {
                var nextNode = nodeList[newNext.Value];
                node.Next = nextNode;
                node.Prev = nextNode.Prev;
                nextNode.Prev = node;
                if (node.Prev != null)
                    node.Prev.Next = node;
                else
                    list.Head = node;
            }
            else
            {
                var tail = list.Tail;
                if (tail != null)
                {
                    tail.Next = node;
                    node.Prev = tail;
                    node.Next = null;
                    list.Tail = node;
                }
                else
                {
                    list.Head = list.Tail = node;
                    node.Prev = node.Next = null;
                }
            }

            RebuildNodeList(list, nodeList);

            Console.WriteLine("Node moved successfully.");
        }

        static void EditNode(List<ListNode> nodeList)
        {
            if (nodeList.Count == 0)
            {
                Console.WriteLine("List is empty.");
                return;
            }

            int? idx = ReadIndex("Enter index of node to edit: ");
            if (!idx.HasValue || idx < 0 || idx >= nodeList.Count)
            {
                Console.WriteLine("Invalid index.");
                return;
            }

            Console.Write("Enter new Data (empty for empty string): ");
            string newData = Console.ReadLine();
            nodeList[idx.Value].Data = newData ?? string.Empty;
            Console.WriteLine("Node edited successfully.");
        }

        static void RemoveNode(ListRand list, List<ListNode> nodeList)
        {
            if (nodeList.Count == 0)
            {
                Console.WriteLine("List is empty.");
                return;
            }

            int? idx = ReadIndex("Enter index of node to remove: ");
            if (!idx.HasValue || idx < 0 || idx >= nodeList.Count)
            {
                Console.WriteLine("Invalid index.");
                return;
            }

            var node = nodeList[idx.Value];
            if (node.Prev != null)
                node.Prev.Next = node.Next;
            else
                list.Head = node.Next;

            if (node.Next != null)
                node.Next.Prev = node.Prev;
            else
                list.Tail = node.Prev;

            nodeList.RemoveAt(idx.Value);
            list.Count = nodeList.Count;

            foreach (var n in nodeList)
            {
                if (n.Rand == node)
                    n.Rand = null;
            }

            Console.WriteLine("Node removed successfully.");
        }

        static void SaveAndExit(ListRand list, string filePath, out bool success)
        {
            success = false;
            while (!success)
            {
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        list.Serialize(fs);
                    Console.WriteLine($"List saved to {filePath}");
                    success = true;
                }
                catch (IOException ex) when (ex.Message.Contains("used by another process"))
                {
                    Console.WriteLine($"File is locked. {ex.Message}");
                    Console.Write("Close the file and press Enter to retry, or type 'cancel' to abort: ");
                    if (Console.ReadLine()?.Trim().ToLower() == "cancel") return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Save error: {ex.Message}");
                    Console.Write("Press Enter to retry or 'cancel' to abort: ");
                    if (Console.ReadLine()?.Trim().ToLower() == "cancel") return;
                }
            }
            try
            {
                using var readFs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var restored = new ListRand();
                restored.Deserialize(readFs);
                Console.WriteLine("Testing restored list...");
                Console.WriteLine(ValidateList(restored) ? "Restored list is valid." : "Restored list is INVALID.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validation after save failed: {ex.Message}");
            }
        }

        static int? ReadIndex(string prompt)
        {
            Console.Write(prompt);
            string input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
                return null;
            if (int.TryParse(input, out int idx))
                return idx;
            Console.WriteLine("Invalid number, treating as null.");
            return null;
        }

        static void PrintList(ListRand list)
        {
            Console.WriteLine("Current list:");
            var indexMap = new Dictionary<ListNode, int>();
            var curr = list.Head;
            int idx = 0;
            while (curr != null)
            {
                indexMap[curr] = idx;
                curr = curr.Next;
                idx++;
            }

            curr = list.Head;
            idx = 0;
            while (curr != null)
            {
                string prevStr = curr.Prev != null ? $"[{indexMap[curr.Prev]}]" : "null";
                string nextStr = curr.Next != null ? $"[{indexMap[curr.Next]}]" : "null";

                string randStr;
                if (curr.Rand == null)
                    randStr = "null";
                else if (indexMap.TryGetValue(curr.Rand, out int randIdx))
                    randStr = $"[{randIdx}]";
                else
                    randStr = "[unknown]"; // Rand указывает на узел вне списка

                Console.WriteLine($"[{idx}] Data: \"{curr.Data}\", Prev: {prevStr}, Next: {nextStr}, Rand: {randStr}");
                curr = curr.Next;
                idx++;
            }
        }

        static void RebuildNodeList(ListRand list, List<ListNode> nodeList)
        {
            nodeList.Clear();
            var curr = list.Head;
            while (curr != null)
            {
                nodeList.Add(curr);
                curr = curr.Next;
            }
        }

        static ListRand CreateEmptyList()
        {
            return new ListRand { Head = null, Tail = null, Count = 0 };
        }

        static ListRand CreateSingleNode(string data)
        {
            var node = new ListNode { Data = data };
            return new ListRand { Head = node, Tail = node, Count = 1 };
        }

        static ListRand CreateList(string[] data, int[] randIndices)
        {
            var nodes = new ListNode[data.Length];
            for (int i = 0; i < data.Length; i++)
                nodes[i] = new ListNode { Data = data[i] };

            for (int i = 0; i < data.Length; i++)
            {
                nodes[i].Prev = i > 0 ? nodes[i - 1] : null;
                nodes[i].Next = i < data.Length - 1 ? nodes[i + 1] : null;
                nodes[i].Rand = randIndices[i] >= 0 && randIndices[i] < data.Length ? nodes[randIndices[i]] : null;
            }

            return new ListRand { Head = nodes[0], Tail = nodes[^1], Count = data.Length };
        }

        static bool ValidateList(ListRand list)
        {
            if (list.Count == 0)
                return list.Head == null && list.Tail == null;

            if (list.Head == null || list.Tail == null)
                return false;

            // Проверка прямого прохода (Head -> Tail)
            var visited = new HashSet<ListNode>();
            var current = list.Head;
            int forwardCount = 0;
            while (current != null)
            {
                if (visited.Contains(current))
                    return false; // Цикл в Next
                visited.Add(current);
                forwardCount++;

                if (current.Next != null && current.Next.Prev != current)
                    return false; // Несогласованность Prev/Next

                current = current.Next;
            }
            if (forwardCount != list.Count)
                return false;
            if (list.Tail != visited.Last())
                return false; // Tail не совпадает с последним узлом

            // Проверка обратного прохода (Tail -> Head)
            int backwardCount = 0;
            current = list.Tail;
            while (current != null)
            {
                if (!visited.Contains(current))
                    return false; // Узел не был в прямом проходе (невозможно)
                backwardCount++;
                current = current.Prev;
            }
            if (backwardCount != list.Count)
                return false;

            // Проверка Rand
            current = list.Head;
            while (current != null)
            {
                if (current.Rand != null && !visited.Contains(current.Rand))
                    return false; // Rand указывает на узел вне списка
                current = current.Next;
            }

            return true;
        }

        static bool TestValidateList()
        {
            // Правильный список
            var validList = CreateList(
                new[] { "A", "B", "C" },
                new[] { 1, -1, 0 } // A->B, B->null, C->A
            );
            if (!ValidateList(validList))
            {
                Console.WriteLine("Valid list reported as invalid");
                return false;
            }

            // Сломанный список: Count завышен
            var badList1 = CreateList(new[] { "X" }, new[] { -1 });
            badList1.Count = 2;
            if (ValidateList(badList1))
            {
                Console.WriteLine("Bad list (wrong Count) passed validation");
                return false;
            }

            // Сломанный список: Rand указывает за пределы
            var badList2 = CreateList(new[] { "Y" }, new[] { -1 });
            badList2.Head.Rand = new ListNode { Data = "alien" };
            if (ValidateList(badList2))
            {
                Console.WriteLine("Bad list (external Rand) passed validation");
                return false;
            }

            return true;
        }

        static bool TestEmptyList()
        {
            string tempFile = null;
            try
            {
                tempFile = Path.GetTempFileName();
                var list = CreateEmptyList();

                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    list.Serialize(fs);

                var restored = new ListRand();
                using (var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
                    restored.Deserialize(fs);

                if (restored.Count != 0 || restored.Head != null || restored.Tail != null)
                {
                    Console.WriteLine("Empty list restored incorrect");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAILED] Exception: {ex.Message}");
                return false;
            }
            finally
            {
                if (tempFile != null && File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        static bool TestSingleNodeNoRand()
        {
            string tempFile = null;
            try
            {
                tempFile = Path.GetTempFileName();
                var list = CreateSingleNode("node0");

                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    list.Serialize(fs);

                var restored = new ListRand();
                using (var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
                    restored.Deserialize(fs);

                if (restored.Count != 1 || restored.Head == null || restored.Head != restored.Tail ||
                    restored.Head.Data != "node0" ||
                    restored.Head.Prev != null || restored.Head.Next != null || restored.Head.Rand != null)
                {
                    Console.WriteLine("One-node list restored incorrect");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAILED] Exception: {ex.Message}");
                return false;
            }
            finally
            {
                if (tempFile != null && File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        static bool TestMultipleNodesWithRand()
        {
            string tempFile = null;
            try
            {
                tempFile = Path.GetTempFileName();
                var list = CreateList(
                    new[] { "A", "B", "C", "D" },
                    new[] { 2, 0, 2, -1 }
                );

                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    list.Serialize(fs);

                var restored = new ListRand();
                using (var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
                    restored.Deserialize(fs);

                if (restored.Count != 4)
                {
                    Console.WriteLine($"Incorrect nodes count: {restored.Count} instead 4");
                    return false;
                }

                var restoredNodes = new ListNode[4];
                var cur = restored.Head;
                for (int i = 0; i < 4; i++)
                {
                    restoredNodes[i] = cur;
                    cur = cur.Next;
                }

                string[] expectedData = { "A", "B", "C", "D" };
                int?[] expectedPrev = { null, 0, 1, 2 };
                int?[] expectedNext = { 1, 2, 3, null };
                int?[] expectedRand = { 2, 0, 2, null };

                for (int i = 0; i < 4; i++)
                {
                    if (restoredNodes[i].Data != expectedData[i])
                    {
                        Console.WriteLine($"Node {i}: data '{restoredNodes[i].Data}' instead '{expectedData[i]}'");
                        return false;
                    }
                    if (!NodeEquals(restoredNodes[i].Prev, restoredNodes, expectedPrev[i]))
                    {
                        Console.WriteLine($"Node {i}: Prev incorrect");
                        return false;
                    }
                    if (!NodeEquals(restoredNodes[i].Next, restoredNodes, expectedNext[i]))
                    {
                        Console.WriteLine($"Node {i}: Next incorrect");
                        return false;
                    }
                    if (!NodeEquals(restoredNodes[i].Rand, restoredNodes, expectedRand[i]))
                    {
                        Console.WriteLine($"Node {i}: Rand incorrect");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAILED] Exception: {ex.Message}");
                return false;
            }
            finally
            {
                if (tempFile != null && File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        static bool NodeEquals(ListNode actual, ListNode[] nodes, int? expectedIndex)
        {
            if (expectedIndex == null)
                return actual == null;
            if (expectedIndex < 0 || expectedIndex >= nodes.Length)
                return false;
            return actual == nodes[expectedIndex.Value];
        }
    }
}