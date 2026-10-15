using System.Collections.Generic;

namespace WutaiTactics.Map
{
    [System.Serializable]
    public class MapData
    {
        public List<MapNode> nodes = new List<MapNode>();
        public string currentNodeId;
        public int currentRow;

        public MapNode GetNode(string id)
        {
            return nodes.Find(n => n.id == id);
        }

        public List<MapNode> GetNodesInRow(int row)
        {
            return nodes.FindAll(n => n.row == row);
        }
    }
}
