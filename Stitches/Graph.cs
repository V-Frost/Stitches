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
        public bool HasConnection { get; set; } = false; // Нове поле для позначення наявності з'єднання

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
            // Очищаем предыдущие узлы
            Nodes.Clear();
            List<List<Node>> blocks = new List<List<Node>>();

            // Инициализируем все клетки для поля размером size x size
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Nodes.Add(new Node(x, y));
                }
            }

            // Матрица с фиксированными стартовыми точками блоков
            int[,] startMatrix = new int[,]
            {
        { 1, 0, 0, 0, 1 },
        { 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0 },
        { 0, 0, 0, 1, 0 },
        { 1, 0, 0, 0, 0 }
            };

            int groupId = 0;
            Random random = new Random();

            // Проходим по полю и создаем блоки, пока не будет пять блоков
            for (int x = 0; x < size && blocks.Count < 5; x++)
            {
                for (int y = 0; y < size && blocks.Count < 5; y++)
                {
                    if (startMatrix[x, y] == 1)
                    {
                        List<Node> newBlock = GenerateBlockFromStart(x, y, groupId++, minBlockSize, maxBlockSize, random);
                        if (newBlock.Count > 0) blocks.Add(newBlock);
                    }
                }
            }

            // Обеспечиваем проходимость уровня
            EnsureLevelSolvability(blocks);

            // Генерируем подсказки для рядов и столбцов
            GenerateHints(size, size);
        }



        private List<Node> GenerateBlockFromStart(int startX, int startY, int groupId, int minBlockSize, int maxBlockSize, Random random)
        {
            List<Node> block = new List<Node>();
            Node startNode = Nodes.FirstOrDefault(n => n.X == startX && n.Y == startY);
            if (startNode == null) return block;

            startNode.GroupID = groupId;
            block.Add(startNode);

            List<Node> unassignedNeighbors = GetUnassignedNeighbors(startNode);

            // Розширюємо блок, щоб досягти мінімального розміру, але не перевищити максимальний розмір
            while (block.Count < minBlockSize && unassignedNeighbors.Count > 0)
            {
                Node nextNode = unassignedNeighbors[random.Next(unassignedNeighbors.Count)];
                nextNode.GroupID = groupId;
                block.Add(nextNode);

                // Оновлюємо список сусідів
                unassignedNeighbors = GetUnassignedNeighbors(nextNode).Where(n => !block.Contains(n)).ToList();
            }

            return block;
        }

        private void EnsureNoSingleNodeBlocks(List<List<Node>> blocks, Random random)
        {
            // Перевірка і видалення всіх блоків, які складаються з однієї клітинки
            bool hasSingleBlocks = true;
            while (hasSingleBlocks)
            {
                hasSingleBlocks = false;

                foreach (var block in blocks.ToList())
                {
                    if (block.Count == 1)
                    {
                        hasSingleBlocks = true;
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
        }

        private List<Node> GetNeighborBlocks(Node node)
        {
            // Знаходимо сусідні блоки, які мають інший GroupID
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



        private List<Node> GetUnassignedNeighbors(Node node)
        {
            // Знаходимо сусідів, які ще не призначені до групи
            return Nodes.Where(n =>
                n.GroupID == -1 &&
                ((Math.Abs(n.X - node.X) == 1 && n.Y == node.Y) ||
                 (Math.Abs(n.Y - node.Y) == 1 && n.X == node.X))
            ).ToList();
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
