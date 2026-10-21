using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class PacStudentController : MonoBehaviour
{
    [Header("Grid / Level")]
    public LevelGenerator levelGenerator;

    [Header("Movement")]
    public float tilesPerSecond = 5f;

    [Header("Feedback")]
    public Animator animator;
    public AudioSource moveAudio;
    public AudioClip moveClip;
    public AudioClip eatClip;
    public ParticleSystem dust;

    [Header("Animation Clips")]
    public AnimationClip upClip;
    public AnimationClip leftClip;
    public AnimationClip downClip;
    public AnimationClip rightClip;

    private KeyCode lastInput = KeyCode.None;
    private KeyCode currentInput = KeyCode.None;

    private int[,] map;
    private Vector2Int gridPos;
    private bool isLerping;
    private Vector3 lerpStart, lerpEnd;
    private float lerpStartTime, lerpDuration;
    private int startRow = 1, startCol = 1;
    private Vector2Int facing = Vector2Int.zero;
    private PlayableGraph graph;
    private AnimationPlayableOutput animOutput;
    private AnimationClipPlayable currentClip;

    public void Init(LevelGenerator generator, int row, int col)
    {
        levelGenerator = generator;
        startRow = row;
        startCol = col;
    }

    void Start()
    {
        if (levelGenerator == null) levelGenerator = FindObjectOfType<LevelGenerator>();
        map = (int[,])levelGenerator.WholeMap.Clone();

        gridPos = new Vector2Int(startCol, -startRow);
        transform.position = GridToWorld(gridPos);

        if (animator)
        {
            graph = PlayableGraph.Create("PacStudentAnim");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            animOutput = AnimationPlayableOutput.Create(graph, "Anim", animator);
            graph.Play();
        }

        SetFacing(Vector2Int.right);
        if (currentClip.IsValid()) currentClip.SetSpeed(0);
    }

    void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }

    void Update()
    {
        GatherInput();

        if (isLerping)
        {
            float t = (Time.time - lerpStartTime) / lerpDuration;
            if (t >= 1f)
            {
                transform.position = lerpEnd;
                isLerping = false;
                OnReachedTile();
            }
            else
            {
                transform.position = Vector3.Lerp(lerpStart, lerpEnd, t);
            }
        }

        if (!isLerping)
            TryMove();

        UpdateFeedback();
    }

    void GatherInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame) lastInput = KeyCode.W;
        else if (kb.aKey.wasPressedThisFrame) lastInput = KeyCode.A;
        else if (kb.sKey.wasPressedThisFrame) lastInput = KeyCode.S;
        else if (kb.dKey.wasPressedThisFrame) lastInput = KeyCode.D;
    }

    void TryMove()
    {
        if (lastInput != KeyCode.None && IsWalkable(gridPos + KeyToDir(lastInput)))
        {
            currentInput = lastInput;
            StartLerp(KeyToDir(currentInput));
        }
        else if (currentInput != KeyCode.None && IsWalkable(gridPos + KeyToDir(currentInput)))
        {
            StartLerp(KeyToDir(currentInput));
        }
    }

    void StartLerp(Vector2Int dir)
    {
        Vector2Int next = gridPos + dir;

        lerpStart = transform.position;
        lerpEnd = GridToWorld(next);
        lerpStartTime = Time.time;
        lerpDuration = 1f / tilesPerSecond;
        isLerping = true;

        AudioClip clip = IsPellet(next) ? eatClip : moveClip;
        if (moveAudio && moveAudio.clip != clip)
        {
            moveAudio.clip = clip;
            moveAudio.Play();
        }

        gridPos = next;
        SetFacing(dir);
    }

    void OnReachedTile()
    {
        if (IsPellet(gridPos)) map[Row(gridPos), Col(gridPos)] = 0;
    }

    void UpdateFeedback()
    {
        if (currentClip.IsValid()) currentClip.SetSpeed(isLerping ? 1 : 0);

        if (moveAudio)
        {
            if (isLerping && !moveAudio.isPlaying) moveAudio.Play();
            else if (!isLerping && moveAudio.isPlaying) moveAudio.Stop();
        }

        if (dust)
        {
            if (isLerping && !dust.isPlaying) dust.Play();
            else if (!isLerping && dust.isPlaying) dust.Stop();
        }
    }

    void SetFacing(Vector2Int dir)
    {
        if (!graph.IsValid() || dir == facing) return;
        AnimationClip clip = dir == Vector2Int.up ? upClip :
                             dir == Vector2Int.left ? leftClip :
                             dir == Vector2Int.down ? downClip : rightClip;
        if (clip == null) return;
        facing = dir;

        double speed = currentClip.IsValid() ? currentClip.GetSpeed() : 1;
        if (currentClip.IsValid()) currentClip.Destroy();
        currentClip = AnimationClipPlayable.Create(graph, clip);
        currentClip.SetSpeed(speed);
        animOutput.SetSourcePlayable(currentClip);
    }

    Vector2Int KeyToDir(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W: return Vector2Int.up;
            case KeyCode.A: return Vector2Int.left;
            case KeyCode.S: return Vector2Int.down;
            case KeyCode.D: return Vector2Int.right;
        }
        return Vector2Int.zero;
    }

    int Row(Vector2Int p) => -p.y;
    int Col(Vector2Int p) => p.x;

    bool InBounds(Vector2Int p) =>
        Row(p) >= 0 && Row(p) < map.GetLength(0) && Col(p) >= 0 && Col(p) < map.GetLength(1);

    bool IsWalkable(Vector2Int p)
    {
        if (!InBounds(p)) return false;
        int v = map[Row(p), Col(p)];
        return v == 0 || v == 5 || v == 6;
    }

    bool IsPellet(Vector2Int p)
    {
        if (!InBounds(p)) return false;
        int v = map[Row(p), Col(p)];
        return v == 5 || v == 6;
    }

    Vector3 GridToWorld(Vector2Int p) => levelGenerator.CellToWorld(Row(p), Col(p));

    public Vector2Int GridPosition => new Vector2Int(Col(gridPos), Row(gridPos));
    public bool IsMoving => isLerping;
}