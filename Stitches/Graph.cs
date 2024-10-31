using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    public class Node
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int GroupID { get; set; } = -1;
        public List<Node> Neighbors { get; set; } = new List<Node>();
        public bool HasConnection { get; set; } = false;

        public Node(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class Graph
    {
        public List<Node> Nodes { get; set; } = new List<Node>();
        public Dictionary<int, int> RowHints { get; private set; } = new Dictionary<int, int>();
        public Dictionary<int, int> ColumnHints { get; private set; } = new Dictionary<int, int>();

        public void GenerateLevel(int size, int minBlockSize = 4, int maxBlockSize = 10)
        {
            Nodes.Clear();
            List<List<Node>> blocks = new List<List<Node>>();

            // Ініціалізація поля
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Nodes.Add(new Node(x, y));
                }
            }

            int[,] startMatrix = InitializeStartMatrix(size);
            int groupId = 0;
            Random random = new Random();

            // Створення блоків з фіксованих точок у матриці
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (startMatrix[x, y] == 1)
                    {
                        List<Node> newBlock = GenerateBlockFromMatrix(x, y, groupId++, minBlockSize, maxBlockSize, random);
                        if (newBlock.Count >= minBlockSize)
                        {
                            blocks.Add(newBlock);
                        }
                        else
                        {
                            // Вилучаємо блоки, які не відповідають мінімальному розміру
                            foreach (var node in newBlock)
                            {
                                node.GroupID = -1;
                            }
                        }
                    }
                }
            }

            EnsureNoSingleNodeBlocks(blocks, random);

            EnsureLevelSolvability(blocks);
            GenerateHints(size, size);
        }

        private List<Node> GenerateBlockFromMatrix(int startX, int startY, int groupId, int minBlockSize, int maxBlockSize, Random random)
        {
            List<Node> block = new List<Node>();
            Queue<Node> toVisit = new Queue<Node>();

            Node startNode = Nodes.FirstOrDefault(n => n.X == startX && n.Y == startY);
            if (startNode == null) return block;

            startNode.GroupID = groupId;
            block.Add(startNode);
            toVisit.Enqueue(startNode);

            // Розширюємо блок, доки він не досягне мінімального розміру
            while (block.Count < minBlockSize && toVisit.Count > 0)
            {
                Node currentNode = toVisit.Dequeue();
                List<Node> unassignedNeighbors = GetUnassignedNeighbors(currentNode)
                    .OrderBy(_ => random.Next()) // Випадковий порядок сусідів
                    .ToList();

                foreach (var neighbor in unassignedNeighbors)
                {
                    if (block.Count >= maxBlockSize) break;

                    neighbor.GroupID = groupId;
                    block.Add(neighbor);
                    toVisit.Enqueue(neighbor);

                    if (block.Count >= minBlockSize) break;
                }
            }

            return block;
        }

        private int[,] InitializeStartMatrix(int size)
        {
            // Точки для ініціалізації блоків залежно від розміру
            if (size == 5)
            {
                return new int[,]
                {
                    { 1, 0, 0, 0, 1 },
                    { 0, 0, 0, 0, 0 },
                    { 0, 0, 0, 0, 0 },
                    { 0, 0, 0, 1, 0 },
                    { 1, 0, 0, 0, 0 }
                };
            }
            else if (size == 7)
            {
                return new int[,]
                {
                    { 1, 0, 0, 0, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0 },
                    { 0, 0, 0, 1, 0, 0, 0 },
                    { 0, 0, 0, 0, 0, 1, 0 },
                    { 0, 0, 1, 0, 0, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0 },
                    { 1, 0, 0, 0, 0, 0, 1 }
                };
            }
            else if (size == 10)
            {
                return new int[,]
                {
                    { 1, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                    { 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
                    { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
                    { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
                    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                    { 0, 0, 0, 1, 0, 0, 0, 1, 0, 0 }
                };
            }
            else if (size == 15)
            {
                return new int[15, 15]; // Додайте конфігурацію для 15x15 за потреби
            }
            else
            {
                throw new ArgumentException("Unsupported size");
            }
        }

        private List<Node> GetUnassignedNeighbors(Node node)
        {
            return Nodes.Where(n =>
                n.GroupID == -1 &&
                ((Math.Abs(n.X - node.X) == 1 && n.Y == node.Y) ||
                 (Math.Abs(n.Y - node.Y) == 1 && n.X == node.X))
            ).ToList();
        }

        private void EnsureNoSingleNodeBlocks(List<List<Node>> blocks, Random random)
        {
            foreach (var block in blocks.ToList())
            {
                if (block.Count < 2)
                {
                    Node singleNode = block[0];
                    List<Node> neighborBlocks = GetNeighborBlocks(singleNode);

                    if (neighborBlocks.Count > 0)
                    {
                        Node neighborToMerge = neighborBlocks[random.Next(neighborBlocks.Count)];
                        singleNode.GroupID = neighborToMerge.GroupID;
                        blocks.First(g => g[0].GroupID == neighborToMerge.GroupID).Add(singleNode);
                        blocks.Remove(block);
                    }
                }
            }
        }

        private List<Node> GetNeighborBlocks(Node node)
        {
            List<Node> neighbors = new List<Node>();

            var topNeighbor = Nodes.FirstOrDefault(n => n.X == node.X && n.Y == node.Y - 1 && n.GroupID != node.GroupID);
            var bottomNeighbor = Nodes.FirstOrDefault(n => n.X == node.X && n.Y == node.Y + 1 && n.GroupID != node.GroupID);
            var leftNeighbor = Nodes.FirstOrDefault(n => n.X == node.X - 1 && n.Y == node.Y && n.GroupID != node.GroupID);
            var rightNeighbor = Nodes.FirstOrDefault(n => n.X == node.X + 1 && n.Y == node.Y && n.GroupID != node.GroupID);

            if (topNeighbor != null) neighbors.Add(topNeighbor);
            if (bottomNeighbor != null) neighbors.Add(bottomNeighbor);
            if (leftNeighbor != null) neighbors.Add(leftNeighbor);
            if (rightNeighbor != null) neighbors.Add(rightNeighbor);

            return neighbors;
        }

        private void EnsureLevelSolvability(List<List<Node>> blocks)
        {
            foreach (var block in blocks)
            {
                foreach (var node in block)
                {
                    List<Node> neighbors = GetNeighborBlocks(node);
                    foreach (var neighbor in neighbors)
                    {
                        AddEdge(node, neighbor);
                    }
                }
            }
        }

        private void AddEdge(Node node1, Node node2)
        {
            if (!node1.Neighbors.Contains(node2))
            {
                node1.Neighbors.Add(node2);
                node2.Neighbors.Add(node1);
            }
        }

        public int GetCellValue(int row, int col)
        {
            var node = Nodes.FirstOrDefault(n => n.X == col && n.Y == row);
            return node != null && node.HasConnection ? 1 : 0;
        }


        private void GenerateHints(int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                RowHints[y] = Nodes.Where(n => n.Y == y).Sum(n => n.Neighbors.Count);
            }

            for (int x = 0; x < width; x++)
            {
                ColumnHints[x] = Nodes.Where(n => n.X == x).Sum(n => n.Neighbors.Count);
            }
        }
    }
}
