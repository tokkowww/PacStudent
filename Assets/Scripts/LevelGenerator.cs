using System;
using Unity.VisualScripting;
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

    [Header("Level01")]
    public GameObject level01;     
    public float tileSize = 1f;

    static readonly int[] DR = { -1, 0, 1, 0 };
    static readonly int[] DC = { 0, 1, 0, -1 };

    int[,] levelMap =
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

    bool IsOpen(int r, int c)
    {
        if (!InBounds(r, c)) return true;  
        int v = wholeMap[r, c];
        return v == 0 || v == 5 || v == 6;
    }
    bool Conn(int r, int c, int dir)
    {
        int nr = r + DR[dir], nc = c + DC[dir];
        if (!InBounds(nr, nc)) return false;
        int self = wholeMap[r, c], v = wholeMap[nr, nc];
        if (v == 0 || v == 5 || v == 6) return false;
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

        float[] cwAngles = { 270f, 0f, 90f, 180f };

        int best = -1, bestScore = -1;
        for (int i = 0; i < 4; i++)
        {
            int a = i, b = (i + 1) % 4;
            if (!(Conn(r, c, a) && Conn(r, c, b))) continue;

            int o1 = (i + 2) % 4, o2 = (i + 3) % 4;
            int score = 0;
            if (IsOpen(r + DR[o1], c + DC[o1])) score++;
            if (IsOpen(r + DR[o2], c + DC[o2])) score++;
            if (IsOpen(r + DR[o1] + DR[o2], c + DC[o1] + DC[o2])) score++;

            if (score > bestScore) { bestScore = score; best = i; }
        }
        return best < 0 ? 0f : cwAngles[best];
    }

    float TAngle(int r, int c)
    {
        if (!Conn(r, c, 0)) return 0f;
        if (!Conn(r, c, 1)) return 90f;
        if (!Conn(r, c, 2)) return 180f;
        if (!Conn(r, c, 3)) return 270f;
        return 0f;
    }


    Vector3 CellToWorld(int r, int c)
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

            GameObject root = new GameObject("GeneratedLevel");

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int id = wholeMap[r, c];
                    if (id == 0) continue;

                    Vector3 pos = CellToWorld(r, c);
                    GameObject prefab = null;
                    float angle = 0f;

                    switch (id)
                    {
                        case 1: prefab = outCorner; angle = CornerAngle(r, c); break;
                        case 3: prefab = innerCorner; angle = CornerAngle(r, c); break;
                        case 2: prefab = outWall; angle = WallAngle(r, c); break;
                        case 4: prefab = innerWall; angle = WallAngle(r, c); break;
                        case 7: prefab = tJunction; angle = TAngle(r, c); break;
                        case 5: prefab = pellet; break;
                        case 6: prefab = powerPellet; break;
                        case 8: prefab = exit; angle = WallAngle(r, c); break;
                    }

                    if (prefab != null)
                        Instantiate(prefab, pos, Quaternion.Euler(0, 0, angle), root.transform);
                }
            }

            FitCamera();
    }
    

}
