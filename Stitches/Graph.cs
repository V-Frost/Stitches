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
            bool isValid = false;
            while (!isValid)
            {
                // Очищуємо попередні вузли
                Nodes.Clear();

                // Ініціалізуємо всі клітинки для поля розміром size x size
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        Nodes.Add(new Node(x, y));
                    }
                }

                // Встановлюємо кількість блоків відповідно до розміру поля
                int blockCount = size == 5 || size == 7 ? 5 : size == 10 ? 8 : size == 15 ? 20 : 5;

                Random random = new Random();
                int groupId = 0;
                List<List<Node>> blocks = new List<List<Node>>();

                // Генеруємо блоки поки не досягнемо необхідної кількості
                for (int i = 0; i < blockCount; i++)
                {
                    Node seed = Nodes.Where(n => n.GroupID == -1).OrderBy(n => random.Next()).FirstOrDefault();
                    if (seed == null) break;

                    List<Node> newBlock = new List<Node>();
                    seed.GroupID = groupId;
                    newBlock.Add(seed);

                    while (newBlock.Count < minBlockSize)
                    {
                        List<Node> expandableNodes = newBlock
                            .SelectMany(n => GetUnassignedNeighbors(n))
                            .Distinct()
                            .ToList();

                        if (expandableNodes.Count == 0) break;

                        Node nextNode = expandableNodes[random.Next(expandableNodes.Count)];
                        nextNode.GroupID = groupId;
                        newBlock.Add(nextNode);
                    }

                    if (newBlock.Count >= minBlockSize)
                    {
                        blocks.Add(newBlock);
                        groupId++;
                    }
                }

                EnsureAllNodesAssigned(blocks, groupId);

                // Перевіряємо, чи рівень коректний
                isValid = IsValidLevel();
            }

            // Генеруємо підказки для рядків та стовпців
            GenerateHints(size, size);
        }

        private bool IsValidLevel()
        {
            foreach (var node in Nodes)
            {
                int thickBorders = 0;

                // Перевіряємо кожен сусідній блок на наявність з'єднання (товстий бордер)
                if (!Nodes.Any(n => n.X == node.X && n.Y == node.Y - 1 && n.GroupID == node.GroupID)) thickBorders++; // Верх
                if (!Nodes.Any(n => n.X == node.X && n.Y == node.Y + 1 && n.GroupID == node.GroupID)) thickBorders++; // Низ
                if (!Nodes.Any(n => n.X == node.X - 1 && n.Y == node.Y && n.GroupID == node.GroupID)) thickBorders++; // Ліворуч
                if (!Nodes.Any(n => n.X == node.X + 1 && n.Y == node.Y && n.GroupID == node.GroupID)) thickBorders++; // Праворуч

                // Якщо клітинка має більше 3 товстих бордерів або ізольований блок, рівень некоректний
                if (thickBorders > 3 || IsIsolatedSingleBlock(node))
                {
                    return false;
                }
            }
            return true;
        }



        // Перевіряє, чи є клітинка ізольованим одиничним блоком
        private bool IsIsolatedSingleBlock(Node node)
        {
            return node.GroupID != -1 && Nodes.Count(n => n.GroupID == node.GroupID) == 1;
        }


        // Перевіряє, чи всі клітинки належать до блоку, і об’єднує їх за необхідності
        private void EnsureAllNodesAssigned(List<List<Node>> blocks, int groupId)
        {
            List<Node> unassignedNodes = Nodes.Where(n => n.GroupID == -1).ToList();
            Random random = new Random();

            foreach (var node in unassignedNodes)
            {
                // Об'єднуємо незакріплену клітинку з випадковим сусіднім блоком
                List<Node> neighborBlocks = GetNeighborBlocks(node);
                if (neighborBlocks.Count > 0)
                {
                    Node neighborToMerge = neighborBlocks[random.Next(neighborBlocks.Count)];
                    node.GroupID = neighborToMerge.GroupID;
                    blocks.First(b => b[0].GroupID == neighborToMerge.GroupID).Add(node);
                }
                else
                {
                    // Створюємо новий блок, якщо сусідів немає (як запасний варіант)
                    node.GroupID = groupId++;
                    blocks.Add(new List<Node> { node });
                }
            }
        }


        public int GetCellValue(int row, int col)
        {
            var node = Nodes.FirstOrDefault(n => n.X == col && n.Y == row);
            return node != null && node.HasConnection ? 1 : 0;
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

            // Якщо блок не досяг мінімального розміру, повертаємо порожній блок (ігноруємо його)
            return block.Count >= minBlockSize ? block : new List<Node>();
        }

        private void EnsureNoSingleNodeBlocks(List<List<Node>> blocks, Random random)
        {
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
                            // Знаходимо випадковий сусідній блок для об'єднання
                            Node neighborToMerge = neighborBlocks[random.Next(neighborBlocks.Count)];
                            singleNode.GroupID = neighborToMerge.GroupID;

                            // Додаємо одинокий вузол у сусідній блок
                            blocks.First(g => g[0].GroupID == neighborToMerge.GroupID).Add(singleNode);
                            blocks.Remove(block); // Видаляємо одинокий блок
                        }
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
