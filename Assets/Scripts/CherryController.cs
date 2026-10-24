using System.Collections;
using UnityEngine;

public class CherryController : MonoBehaviour
{
    public GameObject cherryPrefab;
    public Transform levelCenter;
    public float speed = 3f;
    public float spawnDelay = 5f;
    public int sortingOrder = 100;

    private Camera cam;
    private GameObject currentCherry;

    void Start()
    {
        cam = Camera.main;
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnDelay);
            yield return MoveCherry();
        }
    }

    IEnumerator MoveCherry()
    {
        Vector3 center = levelCenter.position;
        center.z = 0f;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 camPos = cam.transform.position; camPos.z = 0f;
        float radius = Mathf.Sqrt(halfW * halfW + halfH * halfH)
                       + Vector3.Distance(center, camPos) + 1f;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        Vector3 start = center + dir * radius;
        Vector3 end = center - dir * radius;

        currentCherry = Instantiate(cherryPrefab, start, Quaternion.identity);

        SpriteRenderer sr = currentCherry.GetComponent<SpriteRenderer>();
        if (sr)
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            sr.sortingOrder = sortingOrder;
        }

        float duration = (end - start).magnitude / speed;
        float t0 = Time.time;

        while (currentCherry != null)
        {
            float t = (Time.time - t0) / duration;
            if (t >= 1f)
            {
                Destroy(currentCherry);
                break;
            }
            currentCherry.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }
}
