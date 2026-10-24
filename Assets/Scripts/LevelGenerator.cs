using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject empty;
    public GameObject outCorner;
    public GameObject outWall;
    public GameObject innerCorner;
    public GameObject innerWall;
    public GameObject pellet;
    public GameObject powerPellet;
    public GameObject tJunction;
    public GameObject exit;

    [Header("Collectables")]
    public GameObject pelletItem;
    public GameObject powerPillItem;
    public int itemSortingOrder = 5;

    [Header("Level01")]
    public GameObject level01;
    public float tileSize = 1f;

    [Header("Characters")]
    public GameObject pacStudentPrefab;
    public GameObject[] ghostPrefabs = new GameObject[4];
    public int characterSortingOrder = 10;

    static readonly int[] DR = { -1, 0, 1, 0 };
    static readonly int[] DC = { 0, 1, 0, -1 };

    public int[,] levelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
    };

    int[,] wholeMap;
    int rows, cols;
    Vector2Int pacCell;
    List<Vector2Int> ghostCells;

    public int[,] WholeMap => wholeMap;
    public int Rows => rows;
    public int Cols => cols;

    int[,] GetWholeMap(int[,] arr)
    {
        int height = arr.GetLength(0);
        int width = arr.GetLength(1);
        int H = height * 2 - 1;
        int W = width * 2;
        int[,] map = new int[H, W];

        for (int r = 0; r < height; r++)
        {
            for (int c = 0; c < width; c++)
            {
                int v = arr[r, c];
                map[r, c] = v;
                map[r, W - 1 - c] = v;
                map[H - 1 - r, c] = v;
                map[H - 1 - r, W - 1 - c] = v;
            }
        }
        return map;
    }

    bool InBounds(int r, int c) { return r >= 0 && r < rows && c >= 0 && c < cols; }

    static bool IsFloor(int v) { return v == 0 || v == 5 || v == 6; }

    bool IsOpen(int r, int c)
    {
        if (!InBounds(r, c)) return true;
        return IsFloor(wholeMap[r, c]);
    }

    bool Conn(int r, int c, int dir)
    {
        int nr = r + DR[dir], nc = c + DC[dir];
        if (!InBounds(nr, nc)) return false;
        int self = wholeMap[r, c], v = wholeMap[nr, nc];
        if (IsFloor(v)) return false;
        if (self == 7) return true;
        if (self == 1 || self == 2) return v == 1 || v == 2 || v == 7;
        return v == 3 || v == 4 || v == 7 || v == 8;
    }

    float WallAngle(int r, int c)
    {
        bool u = Conn(r, c, 0), rt = Conn(r, c, 1), d = Conn(r, c, 2), l = Conn(r, c, 3);
        bool vert = u && d, horiz = l && rt;
        if (vert && !horiz) return 90f;
        if (horiz && !vert) return 0f;
        if ((u || d) && !(l || rt)) return 90f;
        if ((l || rt) && !(u || d)) return 0f;
        if (IsOpen(r, c - 1) || IsOpen(r, c + 1)) return 90f;
        return 0f;
    }

    float CornerAngle(int r, int c)
    {
        float[] angles = { 90f, 0f, 270f, 180f };

        int best = -1, bestScore = -1;
        for (int i = 0; i < 4; i++)
        {
            int a = i, b = (i + 1) % 4;
            if (!(Conn(r, c, a) && Conn(r, c, b))) continue;

            int o1 = (i + 2) % 4, o2 = (i + 3) % 4;
            int score = 0;
            if (IsOpen(r + DR[a] + DR[b], c + DC[a] + DC[b])) score += 10;
            if (IsOpen(r + DR[o1], c + DC[o1])) score++;
            if (IsOpen(r + DR[o2], c + DC[o2])) score++;
            if (IsOpen(r + DR[o1] + DR[o2], c + DC[o1] + DC[o2])) score++;

            if (score > bestScore) { bestScore = score; best = i; }
        }
        return best < 0 ? 0f : angles[best];
    }

    float TAngle(int r, int c)
    {
        if (!Conn(r, c, 0)) return 0f;
        if (!Conn(r, c, 3)) return 90f;
        if (!Conn(r, c, 2)) return 180f;
        if (!Conn(r, c, 1)) return 270f;
        return 0f;
    }

    public Vector3 CellToWorld(int r, int c)
    {
        float x = (c - (cols - 1) / 2f) * tileSize;
        float y = ((rows - 1) / 2f - r) * tileSize;
        return new Vector3(x, y, 0);
    }

    void FitCamera()
    {
        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.transform.position = new Vector3(0, 0, -10);
        float sizeByH = rows * tileSize / 2f;
        float sizeByW = cols * tileSize / 2f / cam.aspect;
        cam.orthographicSize = Mathf.Max(sizeByH, sizeByW) + tileSize;
    }

    void Start()
    {
        if (level01 != null) Destroy(level01);

        wholeMap = GetWholeMap(levelMap);
        rows = wholeMap.GetLength(0);
        cols = wholeMap.GetLength(1);

        pacCell = FindNearestCells(0f, 0f, v => v == 5, 1)[0];
        wholeMap[pacCell.x, pacCell.y] = 0;
        ghostCells = new List<Vector2Int>();
        int midRow = rows / 2;
        int firstCol = cols / 2 - 2;
        for (int i = 0; i < 4; i++)
            ghostCells.Add(new Vector2Int(midRow, firstCol + i));

        GameObject root = new GameObject("GeneratedLevel");
        GameObject itemRoot = new GameObject("Collectables");

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int id = wholeMap[r, c];
                Vector3 pos = CellToWorld(r, c);

                if (IsFloor(id) && empty != null)
                    Instantiate(empty, pos, Quaternion.identity, root.transform);

                if (id == 0) continue;

                GameObject prefab = null;
                float angle = 0f;

                switch (id)
                {
                    case 1: prefab = outCorner; angle = CornerAngle(r, c); break;
                    case 2: prefab = outWall; angle = WallAngle(r, c); break;
                    case 3: prefab = innerCorner; angle = CornerAngle(r, c); break;
                    case 4: prefab = innerWall; angle = WallAngle(r, c); break;
                    case 5: prefab = pellet; break;
                    case 6: prefab = powerPellet; break;
                    case 7: prefab = tJunction; angle = TAngle(r, c); break;
                    case 8: prefab = exit; angle = WallAngle(r, c); break;
                }

                if (prefab == null) continue;

                bool isPellet = id == 5 || id == 6;
                Vector3 spawnPos = isPellet ? pos + new Vector3(0, 0, -0.1f) : pos;
                GameObject go = Instantiate(prefab, spawnPos, Quaternion.Euler(0, 0, angle), root.transform);

                if (isPellet)
                {
                    foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                        sr.sortingOrder += 1;

                    SpawnItem(id == 5 ? pelletItem : powerPillItem, pos, itemRoot.transform);
                }
            }
        }

        SpawnCharacters();
        FitCamera();
    }

    void SpawnItem(GameObject prefab, Vector3 pos, Transform parent)
    {
        if (prefab == null) return;
        GameObject item = Instantiate(prefab, pos + new Vector3(0, 0, -0.2f), Quaternion.identity, parent);
        foreach (var sr in item.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = itemSortingOrder;
    }

    List<Vector2Int> FindNearestCells(float tr, float tc, System.Func<int, bool> match, int count)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (match(wholeMap[r, c])) cells.Add(new Vector2Int(r, c));

        cells.Sort((a, b) =>
        {
            float da = (a.x - tr) * (a.x - tr) + (a.y - tc) * (a.y - tc);
            float db = (b.x - tr) * (b.x - tr) + (b.y - tc) * (b.y - tc);
            if (da != db) return da.CompareTo(db);
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });

        if (cells.Count > count) cells.RemoveRange(count, cells.Count - count);
        return cells;
    }

    void SpawnCharacters()
    {
        GameObject root = new GameObject("Characters");

        if (pacStudentPrefab != null)
        {
            GameObject pac = Instantiate(pacStudentPrefab, CellToWorld(pacCell.x, pacCell.y), Quaternion.identity, root.transform);
            pac.name = "PacStudent";
            SetSorting(pac);
            PacStudentController ctrl = pac.GetComponent<PacStudentController>();
            if (ctrl != null) ctrl.Init(this, pacCell.x, pacCell.y);
        }

        if (ghostPrefabs == null || ghostPrefabs.Length == 0) return;

        for (int i = 0; i < ghostCells.Count; i++)
        {
            GameObject prefab = ghostPrefabs[i % ghostPrefabs.Length];
            if (prefab == null) continue;
            Vector2Int cell = ghostCells[i];
            GameObject ghost = Instantiate(prefab, CellToWorld(cell.x, cell.y), Quaternion.identity, root.transform);
            ghost.name = "Ghost" + (i + 1);
            SetSorting(ghost);
            GhostController gc = ghost.GetComponent<GhostController>();
            if (gc != null) gc.Init(this, i + 1, cell.x, cell.y);
        }
    }

    void SetSorting(GameObject go)
    {
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = characterSortingOrder;
    }
}