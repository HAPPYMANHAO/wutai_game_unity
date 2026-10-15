using System.Collections.Generic;

namespace WutaiTactics.Map
{
    [System.Serializable]
    public class MapNode
    {
        public string id;
        public MapNodeType nodeType;
        public int row;
        public int col;
        public List<string> nextNodeIds = new List<string>();
        public bool visited;
        public bool reachable;

        public MapNode(string id, MapNodeType type, int row, int col)
        {
            this.id = id;
            this.nodeType = type;
            this.row = row;
            this.col = col;
        }
    }
}
