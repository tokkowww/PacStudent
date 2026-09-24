using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{
    public float moveSpeed = 3f;

    // Animator
    public Animator animator;

    // Four directional animations
    public AnimationClip walkRight;
    public AnimationClip walkDown;
    public AnimationClip walkLeft;
    public AnimationClip walkUp;

    // Movement audio
    public AudioSource audioSource;
    public AudioClip movingSound;

    private Vector3 pointA = new Vector3(7.5f, 1.5f, 0f);
    private Vector3 pointB = new Vector3(18.5f, 1.5f, 0f);
    private Vector3 pointC = new Vector3(18.5f, -2.5f, 0f);
    private Vector3 pointD = new Vector3(7.5f, -2.5f, 0f);

    private Vector3 startPoint;
    private Vector3 targetPoint;

    private float journeyTime;
    private float elapsedTime;

    private int currentPoint = 0;

    void Start()
    {
        transform.position = pointA;

        startPoint = pointA;
        targetPoint = pointB;

        journeyTime = Vector3.Distance(startPoint, targetPoint) / moveSpeed;

        // Start with right animation
        PlayAnimation(walkRight);

        // Start moving audio
        audioSource.clip = movingSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        float t = elapsedTime / journeyTime;

        transform.position = Vector3.Lerp(startPoint, targetPoint, t);

        if (t >= 1f)
        {
            MoveToNextPoint();
        }
    }

    void MoveToNextPoint()
    {
        elapsedTime = 0f;

        currentPoint++;

        if (currentPoint == 1)
        {
            // B → C : Down
            startPoint = pointB;
            targetPoint = pointC;

            PlayAnimation(walkDown);
        }
        else if (currentPoint == 2)
        {
            // C → D : Left
            startPoint = pointC;
            targetPoint = pointD;

            PlayAnimation(walkLeft);
        }
        else if (currentPoint == 3)
        {
            // D → A : Up
            startPoint = pointD;
            targetPoint = pointA;

            PlayAnimation(walkUp);
        }
        else
        {
            // A → B : Right
            currentPoint = 0;

            startPoint = pointA;
            targetPoint = pointB;

            PlayAnimation(walkRight);
        }

        journeyTime = Vector3.Distance(startPoint, targetPoint) / moveSpeed;
    }

    void PlayAnimation(AnimationClip clip)
    {
        if (clip != null && animator != null)
        {
            animator.Play(clip.name);
        }
    }
}
