using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Rigidbody2D))]
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

    [Header("Collision Feedback")]
    public AudioSource sfxAudio;
    public AudioClip wallBumpClip;
    public AudioClip cherryClip;
    public AudioClip deathSoundClip;
    public AudioClip ghostEatClip;
    public ParticleSystem wallBumpPrefab;
    public ParticleSystem deathParticlePrefab;

    [Header("Animation Clips")]
    public AnimationClip upClip;
    public AnimationClip leftClip;
    public AnimationClip downClip;
    public AnimationClip rightClip;
    public AnimationClip deathClip;
    public float fallbackDeathDuration = 1.5f;

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
    private bool wasBlocked;
    private bool controlEnabled = true;

    public void Init(LevelGenerator generator, int row, int col)
    {
        levelGenerator = generator;
        startRow = row;
        startCol = col;
    }

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();
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
        if (!controlEnabled) return;

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
        if (lastInput != KeyCode.None && CanMove(KeyToDir(lastInput)))
        {
            currentInput = lastInput;
            Move(KeyToDir(currentInput));
        }
        else if (currentInput != KeyCode.None && CanMove(KeyToDir(currentInput)))
        {
            Move(KeyToDir(currentInput));
        }
        else if (currentInput != KeyCode.None && !wasBlocked)
        {
            wasBlocked = true;
            OnWallBump(KeyToDir(currentInput));
        }
    }

    bool CanMove(Vector2Int dir)
    {
        return IsWalkable(gridPos + dir) || IsTunnelExit(dir);
    }

    bool IsTunnelExit(Vector2Int dir)
    {
        if (dir.y != 0 || dir.x == 0) return false;
        Vector2Int next = gridPos + dir;
        int r = Row(next);
        int c = Col(next);
        if (r < 0 || r >= map.GetLength(0)) return false;
        if (c >= 0 && c < map.GetLength(1)) return false;
        Vector2Int opposite = new Vector2Int(dir.x < 0 ? map.GetLength(1) - 1 : 0, gridPos.y);
        return IsWalkable(opposite);
    }

    void Move(Vector2Int dir)
    {
        if (IsTunnelExit(dir))
        {
            Teleport(dir);
            if (!IsWalkable(gridPos + dir)) return;
        }
        StartLerp(dir);
    }

    void Teleport(Vector2Int dir)
    {
        gridPos = new Vector2Int(dir.x < 0 ? map.GetLength(1) - 1 : 0, gridPos.y);
        transform.position = GridToWorld(gridPos);
    }

    void StartLerp(Vector2Int dir)
    {
        Vector2Int next = gridPos + dir;

        lerpStart = transform.position;
        lerpEnd = GridToWorld(next);
        lerpStartTime = Time.time;
        lerpDuration = 1f / tilesPerSecond;
        isLerping = true;
        wasBlocked = false;

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

    void OnWallBump(Vector2Int dir)
    {
        Vector3 here = GridToWorld(gridPos);
        Vector3 point = Vector3.Lerp(here, GridToWorld(gridPos + dir), 0.5f);

        if (wallBumpPrefab)
        {
            ParticleSystem ps = Instantiate(wallBumpPrefab, point, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }

        PlaySfx(wallBumpClip);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!controlEnabled) return;

        switch (other.tag)
        {
            case "Pellet":
                Destroy(other.gameObject);
                GameManager.Instance.AddScore(10);
                break;

            case "PowerPill":
                Destroy(other.gameObject);
                GameManager.Instance.AddScore(50);
                GameManager.Instance.ActivatePowerPill();
                break;

            case "Cherry":
                Destroy(other.gameObject);
                GameManager.Instance.AddScore(100);
                PlaySfx(cherryClip);
                break;

            case "Ghost":
                GhostController ghost = other.GetComponent<GhostController>();
                if (ghost == null) break;
                if (ghost.State == GhostState.Normal)
                {
                    GameManager.Instance.PlayerDie();
                }
                else if (ghost.State == GhostState.Scared || ghost.State == GhostState.Recovering)
                {
                    PlaySfx(ghostEatClip);
                    GameManager.Instance.EatGhost(ghost);
                }
                break;
        }
    }

    public IEnumerator DeathSequence()
    {
        controlEnabled = false;
        isLerping = false;

        if (dust) dust.Stop();
        if (moveAudio) moveAudio.Stop();

        float duration = deathClip != null ? deathClip.length : fallbackDeathDuration;

        if (deathParticlePrefab)
        {
            ParticleSystem ps = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, duration + ps.main.startLifetime.constantMax);
        }

        PlaySfx(deathSoundClip);
        PlayClip(deathClip, 1);

        yield return new WaitForSeconds(duration);

        gridPos = new Vector2Int(startCol, -startRow);
        transform.position = GridToWorld(gridPos);
        lastInput = KeyCode.None;
        currentInput = KeyCode.None;
        wasBlocked = false;

        facing = Vector2Int.zero;
        SetFacing(Vector2Int.right);
        if (currentClip.IsValid()) currentClip.SetSpeed(0);

        controlEnabled = true;
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfxAudio && clip) sfxAudio.PlayOneShot(clip);
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
        PlayClip(clip, speed);
    }

    void PlayClip(AnimationClip clip, double speed)
    {
        if (!graph.IsValid() || clip == null) return;
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