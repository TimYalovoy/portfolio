using System.Text;

namespace csharp_solution
{
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
                    nodes[i].Prev = randIndices[i] != -1 ? nodes[randIndices[i]] : null;
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
            // string testOutputFile0 = "output0.txt";
            // string testOutputFile1 = "output1.txt";
            // string testOutputFile2 = "output2.txt";
            // ListRand outputSingleNodeRand0 = new ListRand();
            // ListRand outputSingleNodeRand1 = new ListRand();
            // ListRand outputSingleNodeRand2 = new ListRand();
            // ListRand outputSingleNodeRand3 = new ListRand();
            //
            // ListNode singleNode0 = new ListNode()
            // {
            //     Prev = null,
            //     Next = null,
            //     Data = $"This is simple test data",
            //     Rand = null
            // };
            // outputSingleNodeRand0.Head = singleNode0;
            // outputSingleNodeRand0.Tail = singleNode0;
            // outputSingleNodeRand0.Count = outputSingleNodeRand0.Count + 1;
            //
            // ListNode singleNode1 = new ListNode()
            // {
            //     Prev = null,
            //     Next = null,
            //     Data = $"This is test#0 data: Here must be \"",
            //     Rand = null
            // };
            // ListNode singleNode2 = new ListNode()
            // {
            //     Prev = null,
            //     Next = null,
            //     Data = $"This is test#1 data: Here must be \'",
            //     Rand = null
            // };
            //
            //
            // string testInputFile0 = "input0.txt";
            // string testInputFile1 = "input1.txt";
            // string testInputFile2 = "input2.txt";
            // ListRand inputRand0 = new ListRand();
            // ListRand inputRand1 = new ListRand();
            // ListRand inputRand2 = new ListRand();
        }
    }
}