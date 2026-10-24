using System.Collections.Generic;
using UnityEngine;

public enum GhostState { Normal = 0, Scared = 1, Recovering = 2, Dead = 3 }

public class GhostController : MonoBehaviour
{
    public Vector2 startDirection = Vector2.down;
    public float normalSpeedRatio = 0.9f;
    public float slowSpeedRatio = 0.5f;

    public GhostState State { get; private set; } = GhostState.Normal;
    public bool Frozen { get; set; }
    public bool CanMove => !Frozen && GameManager.Instance != null && GameManager.Instance.IsPlaying;
    public int GhostNumber { get; private set; } = 1;

    static readonly Vector2Int Up = new Vector2Int(0, -1);
    static readonly Vector2Int Down = new Vector2Int(0, 1);
    static readonly Vector2Int Left = new Vector2Int(-1, 0);
    static readonly Vector2Int Right = new Vector2Int(1, 0);
    static readonly Vector2Int[] Dirs = { Up, Left, Down, Right };

    Animator animator;
    LevelGenerator level;
    int[,] map;
    bool ready;

    Vector2Int startCell, cell, prevCell;
    Vector3 startWorld, lerpStart, lerpEnd;
    bool isLerping;
    float lerpProgress, lerpDuration;

    readonly HashSet<Vector2Int> spawnCells = new HashSet<Vector2Int>();
    readonly HashSet<Vector2Int> gateCells = new HashSet<Vector2Int>();
    Vector2Int exitGate, exitStep;

    readonly List<Vector2Int> loopCells = new List<Vector2Int>();
    readonly List<Vector2Int> loopDirs = new List<Vector2Int>();
    int[,] distToLoop;
    int loopIndex = -1;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (GameManager.Instance) GameManager.Instance.RegisterGhost(this);
    }

    void Start()
    {
        SetState(State);
        SetDirection(startDirection);
    }

    void OnDestroy()
    {
        if (GameManager.Instance) GameManager.Instance.UnregisterGhost(this);
    }

    public void Init(LevelGenerator generator, int number, int row, int col)
    {
        level = generator;
        GhostNumber = number;
        map = generator.WholeMap;

        startCell = new Vector2Int(col, row);
        startWorld = CellToWorld(startCell);
        transform.position = startWorld;
        cell = prevCell = startCell;

        BuildSpawnArea();
        BuildExit();
        BuildOuterLoop();
        BuildDistanceToLoop();

        ready = true;
    }

    bool AnimatorReady => animator != null && animator.runtimeAnimatorController != null;

    public void SetState(GhostState s)
    {
        State = s;
        if (AnimatorReady) animator.SetInteger("State", (int)s);
    }

    public void SetDirection(Vector2 dir)
    {
        if (!AnimatorReady || dir == Vector2.zero) return;
        animator.SetFloat("DirX", dir.x);
        animator.SetFloat("DirY", dir.y);
    }

    public void Die()
    {
        SetState(GhostState.Dead);
        isLerping = false;
        loopIndex = -1;
    }

    public void ResetGhost()
    {
        isLerping = false;
        loopIndex = -1;
        cell = prevCell = startCell;
        transform.position = startWorld;
        SetState(GhostState.Normal);
        SetDirection(startDirection);
        Frozen = false;
    }

    void Update()
    {
        if (!ready || !CanMove) return;

        if (State == GhostState.Dead)
        {
            MoveDead();
            return;
        }

        if (!isLerping) ChooseNextMove();
        if (!isLerping) return;

        lerpProgress += Time.deltaTime / lerpDuration;
        if (lerpProgress >= 1f)
        {
            transform.position = lerpEnd;
            isLerping = false;
            ChooseNextMove();
        }
        else
        {
            transform.position = Vector3.Lerp(lerpStart, lerpEnd, lerpProgress);
        }
    }

    void ChooseNextMove()
    {
        Vector2Int dir;

        if (spawnCells.Contains(cell) || gateCells.Contains(cell))
        {
            loopIndex = -1;
            dir = ExitDirection();
        }
        else if (State == GhostState.Scared || State == GhostState.Recovering)
        {
            loopIndex = -1;
            dir = MoveByDistance(true);
        }
        else
        {
            switch (GhostNumber)
            {
                case 1: dir = MoveByDistance(true); break;
                case 2: dir = MoveByDistance(false); break;
                case 3: dir = Pick(ValidDirections()); break;
                default: dir = FollowOuterWall(); break;
            }
            if (GhostNumber != 4) loopIndex = -1;
        }

        if (dir == Vector2Int.zero) return;
        StartLerp(dir);
    }

    void StartLerp(Vector2Int dir)
    {
        prevCell = cell;
        cell += dir;

        lerpStart = transform.position;
        lerpEnd = CellToWorld(cell);
        lerpProgress = 0f;
        lerpDuration = 1f / CurrentSpeed;
        isLerping = true;

        SetDirection(new Vector2(dir.x, -dir.y));
    }

    void MoveDead()
    {
        Vector3 to = startWorld - transform.position;
        if (to.sqrMagnitude > 0.0001f)
        {
            if (Mathf.Abs(to.x) > Mathf.Abs(to.y)) SetDirection(new Vector2(Mathf.Sign(to.x), 0f));
            else SetDirection(new Vector2(0f, Mathf.Sign(to.y)));
        }

        transform.position = Vector3.MoveTowards(transform.position, startWorld, SlowSpeed * TileSize * Time.deltaTime);

        if (transform.position == startWorld) Revive();
    }

    void Revive()
    {
        cell = prevCell = startCell;
        isLerping = false;
        loopIndex = -1;
        SetState(GameManager.Instance ? GameManager.Instance.GlobalGhostState : GhostState.Normal);
    }

    Vector2Int ExitDirection()
    {
        if (spawnCells.Contains(cell) && cell.x != exitGate.x)
            return new Vector2Int(exitGate.x > cell.x ? 1 : -1, 0);
        return exitStep;
    }

    Vector2Int MoveByDistance(bool away)
    {
        List<Vector2Int> options = ValidDirections();
        if (options.Count == 0) return Vector2Int.zero;

        PacStudentController pac = GameManager.Instance ? GameManager.Instance.pacStudent : null;
        if (pac == null) return Pick(options);

        Vector2Int target = pac.GridPosition;
        int current = SqrDistance(cell, target);

        List<Vector2Int> filtered = new List<Vector2Int>();
        foreach (Vector2Int d in options)
        {
            int next = SqrDistance(cell + d, target);
            if (away ? next >= current : next <= current) filtered.Add(d);
        }

        return Pick(filtered.Count > 0 ? filtered : options);
    }

    Vector2Int FollowOuterWall()
    {
        if (loopCells.Count == 0) return Pick(ValidDirections());

        if (loopIndex < 0) loopIndex = FindLoopIndex();

        if (loopIndex >= 0 && loopCells[loopIndex] == cell)
        {
            Vector2Int d = loopDirs[loopIndex];
            loopIndex = (loopIndex + 1) % loopCells.Count;
            return d;
        }

        loopIndex = -1;

        List<Vector2Int> options = ValidDirections();
        if (options.Count == 0) return Vector2Int.zero;

        Vector2Int best = options[0];
        int bestDist = int.MaxValue;
        foreach (Vector2Int d in options)
        {
            Vector2Int n = cell + d;
            int dist = distToLoop[n.y, n.x];
            if (dist < bestDist)
            {
                bestDist = dist;
                best = d;
            }
        }
        return best;
    }

    int FindLoopIndex()
    {
        int fallback = -1;
        for (int i = 0; i < loopCells.Count; i++)
        {
            if (loopCells[i] != cell) continue;
            if (cell + loopDirs[i] != prevCell) return i;
            fallback = i;
        }
        return fallback;
    }

    List<Vector2Int> ValidDirections()
    {
        List<Vector2Int> list = new List<Vector2Int>();
        foreach (Vector2Int d in Dirs)
        {
            Vector2Int n = cell + d;
            if (n != prevCell && Walkable(n)) list.Add(d);
        }

        if (list.Count == 0)
        {
            foreach (Vector2Int d in Dirs)
                if (Walkable(cell + d)) list.Add(d);
        }
        return list;
    }

    Vector2Int Pick(List<Vector2Int> options)
    {
        if (options.Count == 0) return Vector2Int.zero;
        return options[Random.Range(0, options.Count)];
    }

    void BuildSpawnArea()
    {
        spawnCells.Clear();
        gateCells.Clear();

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startCell);
        spawnCells.Add(startCell);

        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            foreach (Vector2Int d in Dirs)
            {
                Vector2Int n = c + d;
                if (!InBounds(n) || spawnCells.Contains(n)) continue;
                int v = map[n.y, n.x];
                if (v == 8)
                {
                    gateCells.Add(n);
                    continue;
                }
                if (v != 0) continue;
                spawnCells.Add(n);
                queue.Enqueue(n);
            }
        }
    }

    void BuildExit()
    {
        bool useTop = GhostNumber % 2 == 1;
        bool found = false;
        int bestOffset = int.MaxValue;

        foreach (Vector2Int g in gateCells)
        {
            bool correctSide = useTop ? g.y < startCell.y : g.y > startCell.y;
            if (!correctSide) continue;
            int offset = Mathf.Abs(g.x - startCell.x);
            if (offset < bestOffset)
            {
                bestOffset = offset;
                exitGate = g;
                found = true;
            }
        }

        if (!found)
        {
            foreach (Vector2Int g in gateCells)
            {
                exitGate = g;
                found = true;
                break;
            }
        }

        if (!found) exitGate = startCell;
        exitStep = exitGate.y < startCell.y ? Up : Down;
    }

    void BuildOuterLoop()
    {
        loopCells.Clear();
        loopDirs.Clear();

        Vector2Int start = Vector2Int.zero;
        bool found = false;
        for (int r = 0; r < map.GetLength(0) && !found; r++)
        {
            for (int c = 0; c < map.GetLength(1); c++)
            {
                Vector2Int p = new Vector2Int(c, r);
                if (Walkable(p))
                {
                    start = p;
                    found = true;
                    break;
                }
            }
        }
        if (!found) return;

        Vector2Int pos = start;
        Vector2Int facing = Right;

        for (int guard = 0; guard < 20000; guard++)
        {
            Vector2Int d = LeftHandStep(pos, facing);
            if (d == Vector2Int.zero) break;
            if (loopCells.Count > 0 && pos == start && d == loopDirs[0]) break;

            loopCells.Add(pos);
            loopDirs.Add(d);
            pos += d;
            facing = d;
        }
    }

    Vector2Int LeftHandStep(Vector2Int pos, Vector2Int facing)
    {
        Vector2Int left = new Vector2Int(facing.y, -facing.x);
        Vector2Int right = new Vector2Int(-facing.y, facing.x);
        Vector2Int[] order = { left, facing, right, -facing };

        foreach (Vector2Int d in order)
            if (Walkable(pos + d)) return d;
        return Vector2Int.zero;
    }

    void BuildDistanceToLoop()
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);
        distToLoop = new int[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                distToLoop[r, c] = int.MaxValue;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        foreach (Vector2Int p in loopCells)
        {
            if (distToLoop[p.y, p.x] == 0) continue;
            distToLoop[p.y, p.x] = 0;
            queue.Enqueue(p);
        }

        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            foreach (Vector2Int d in Dirs)
            {
                Vector2Int n = c + d;
                if (!Walkable(n) || distToLoop[n.y, n.x] != int.MaxValue) continue;
                distToLoop[n.y, n.x] = distToLoop[c.y, c.x] + 1;
                queue.Enqueue(n);
            }
        }
    }

    bool InBounds(Vector2Int p) =>
        p.y >= 0 && p.y < map.GetLength(0) && p.x >= 0 && p.x < map.GetLength(1);

    bool Walkable(Vector2Int p)
    {
        if (!InBounds(p) || spawnCells.Contains(p)) return false;
        int v = map[p.y, p.x];
        return v == 0 || v == 5 || v == 6;
    }

    int SqrDistance(Vector2Int a, Vector2Int b)
    {
        int dx = a.x - b.x;
        int dy = a.y - b.y;
        return dx * dx + dy * dy;
    }

    Vector3 CellToWorld(Vector2Int p) => level.CellToWorld(p.y, p.x);

    float TileSize => level ? level.tileSize : 1f;

    float PacSpeed
    {
        get
        {
            PacStudentController pac = GameManager.Instance ? GameManager.Instance.pacStudent : null;
            return pac ? pac.tilesPerSecond : 5f;
        }
    }

    float NormalSpeed => PacSpeed * normalSpeedRatio;
    float SlowSpeed => NormalSpeed * slowSpeedRatio;
    float CurrentSpeed => State == GhostState.Normal ? NormalSpeed : SlowSpeed;
}