using UnityEngine;

namespace WutaiTactics
{
    public class BattleGrid : MonoBehaviour
    {
        public static BattleGrid Instance { get; private set; }

        [Header("Grid Settings")]
        public int columns = 6;
        public int rows = 3;
        public float cellSize = 1f;
        public float spacing = 0.05f;

        [Header("Visual")]
        public Color meleeRegionColor = new Color(0.9f, 0.4f, 0.4f, 0.4f);
        public Color polearmRegionColor = new Color(0.9f, 0.9f, 0.4f, 0.4f);
        public Color arrowRegionColor = new Color(0.4f, 0.4f, 0.9f, 0.4f);

        private SpriteRenderer[,] cellRenderers;
        private EnemyCharacter[,] occupantMap;

        private void Awake()
        {
            Instance = this;
            CreateGrid();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void CreateGrid()
        {
            cellRenderers = new SpriteRenderer[columns, rows];
            occupantMap = new EnemyCharacter[columns, rows];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    var cellGO = new GameObject($"Cell_{c}_{r}");
                    cellGO.transform.SetParent(transform);
                    cellGO.transform.localPosition = GetCellWorldPosition(c, r);

                    var sr = cellGO.AddComponent<SpriteRenderer>();
                    sr.sprite = SpriteUtil.GetWhiteSprite();
                    sr.color = GetRegionColor(c);
                    sr.sortingOrder = 0;
                    sr.transform.localScale = Vector3.one * (cellSize - spacing);
                    cellRenderers[c, r] = sr;
                }
            }
        }

        public Vector3 GetCellWorldPosition(int col, int row)
        {
            float x = (col - columns / 2f + 0.5f) * cellSize;
            float y = (row - rows / 2f + 0.5f) * cellSize;
            return new Vector3(x, y, 0);
        }

        public Vector3 GetPlayerStandPosition(int row)
        {
            return new Vector3((-columns / 2f - 0.6f) * cellSize, (row - rows / 2f + 0.5f) * cellSize, 0);
        }

        public RegionType GetRegion(int col)
        {
            if (col < 2) return RegionType.Melee;
            if (col < 4) return RegionType.Polearm;
            return RegionType.Arrow;
        }

        private Color GetRegionColor(int col)
        {
            RegionType region = GetRegion(col);
            return region switch
            {
                RegionType.Melee => meleeRegionColor,
                RegionType.Polearm => polearmRegionColor,
                RegionType.Arrow => arrowRegionColor,
                _ => Color.white
            };
        }

        public bool IsValid(int col, int row)
        {
            return col >= 0 && col < columns && row >= 0 && row < rows;
        }

        public EnemyCharacter GetOccupant(int col, int row)
        {
            if (!IsValid(col, row)) return null;
            return occupantMap[col, row];
        }

        public void SetOccupant(int col, int row, EnemyCharacter enemy)
        {
            if (IsValid(col, row))
                occupantMap[col, row] = enemy;
        }

        public void ClearOccupant(int col, int row)
        {
            if (IsValid(col, row))
                occupantMap[col, row] = null;
        }

        public void MoveEnemy(EnemyCharacter enemy, int newCol, int newRow)
        {
            if (enemy == null) return;
            ClearOccupant(enemy.columnIndex, enemy.rowIndex);
            enemy.columnIndex = newCol;
            enemy.rowIndex = newRow;
            enemy.transform.position = GetCellWorldPosition(newCol, newRow);
            SetOccupant(newCol, newRow, enemy);
        }
    }
}
