using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SnakeGame : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject snakeHeadPrefab;
    public GameObject snakeBodyPrefab;
    public GameObject foodPrefab;

    [Header("UI")]
    public Text scoreText;

    [Header("Settings")]
    public int gridHalfSize = 15;
    public float moveInterval = 0.3f;
    public float minMoveInterval = 0.1f;
    public float speedIncrement = 0.005f;

    // Visual constants
    private const float BodyScale    = 0.75f;
    private const float TailScale    = BodyScale * 0.70f;
    private const float HeadBaseScale = 0.9f;
    private static readonly Color BodyColor     = new Color(0.40f, 0.20f, 1.00f, 0.70f);
    private static readonly Color FoodLumpColor  = new Color(1f, 0.15f, 0.08f, 0.70f);

    // Acceleration
    private float _holdTime   = 0f;
    private float _stepFlash  = 0f;   // 0-1, decays after each step for squash anim
    private const float AccelRampTime = 1.0f;  // seconds to reach full boost

    private Vector2Int currentDir = Vector2Int.right;
    private Vector2Int pendingDir = Vector2Int.right;

    private List<Vector2Int> positions = new List<Vector2Int>();
    private List<GameObject> segments  = new List<GameObject>();

    private class GrowthToken
    {
        public int seg;
        public int tailIdx;
    }
    private readonly List<GrowthToken> _tokens = new List<GrowthToken>();

    private GameObject food;
    private Vector2Int foodGridPos;
    private int score;
    private float stepTimer;
    private bool isDead;

    public int CurrentScore => score;
    public bool IsDead => isDead;
    public event System.Action<int> GameOver;

    void Start()
    {
        positions.Add(Vector2Int.zero);
        segments.Add(Instantiate(snakeHeadPrefab, ToWorld(Vector2Int.zero), Quaternion.identity));

        for (int i = 1; i <= 4; i++)
        {
            var p = new Vector2Int(-i, 0);
            positions.Add(p);
            segments.Add(Instantiate(snakeBodyPrefab, ToWorld(p), Quaternion.identity));
        }

        EnsureTailScale();
        SpawnFood();
        UpdateScoreUI();
    }

    void Update()
    {
        if (isDead) return;

        ReadInput();

        // Effective interval shrinks while holding a key (ease-in curve)
        float accelT = _holdTime / AccelRampTime;
        float effectiveInterval = Mathf.Lerp(moveInterval, moveInterval * 0.35f, accelT * accelT);

        stepTimer += Time.deltaTime;
        if (stepTimer >= effectiveInterval)
        {
            stepTimer = 0f;
            _stepFlash = 1f;   // trigger squash flash
            Step();
        }

        // Decay step flash
        _stepFlash = Mathf.Max(_stepFlash - Time.deltaTime * 10f, 0f);

        // Apply head squash/stretch animation
        ApplyHeadAnimation(accelT);
    }

    void ReadInput()
    {
        // Direction change — only on new key press (GetKeyDown)
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            && currentDir != Vector2Int.down)
            pendingDir = Vector2Int.up;
        else if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            && currentDir != Vector2Int.up)
            pendingDir = Vector2Int.down;
        else if ((Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            && currentDir != Vector2Int.right)
            pendingDir = Vector2Int.left;
        else if ((Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            && currentDir != Vector2Int.left)
            pendingDir = Vector2Int.right;

        // Acceleration — while ANY direction key is held
        bool held = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)  ||
                    Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ||
                    Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ||
                    Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        _holdTime = held
            ? Mathf.Min(_holdTime + Time.deltaTime, AccelRampTime)
            : Mathf.Max(_holdTime - Time.deltaTime * 3f, 0f);
    }

    // Head squash forward when accelerating; brief stretch flash on each step
    void ApplyHeadAnimation(float accelT)
    {
        if (segments.Count == 0) return;

        float flash   = _stepFlash * 0.18f;
        float stretch = 1f + accelT * 0.40f + flash;
        float squash  = 1f - accelT * 0.14f - flash * 0.5f;

        segments[0].transform.localScale = new Vector3(
            HeadBaseScale * squash,
            HeadBaseScale * squash,
            HeadBaseScale * stretch);
    }

    void Step()
    {
        currentDir = pendingDir;
        Vector2Int newHead = positions[0] + currentDir;

        if (Mathf.Abs(newHead.x) > gridHalfSize || Mathf.Abs(newHead.y) > gridHalfSize)
        {
            Die(); return;
        }

        for (int i = 0; i < positions.Count - 1; i++)
        {
            if (positions[i] == newHead) { Die(); return; }
        }

        bool ateFood = (newHead == foodGridPos);

        if (ateFood)
        {
            if (food != null) { Destroy(food); food = null; }

            positions.Insert(0, newHead);

            int tailIdx = segments.Count;
            var tailSeg = Instantiate(snakeBodyPrefab,
                ToWorld(positions[tailIdx]), Quaternion.identity);
            tailSeg.transform.localScale = Vector3.zero;
            segments.Add(tailSeg);

            _tokens.Add(new GrowthToken { seg = 1, tailIdx = tailIdx });

            score += 10;
            moveInterval = Mathf.Max(minMoveInterval, moveInterval - speedIncrement);
            SpawnFood();
            UpdateScoreUI();
        }
        else
        {
            positions.RemoveAt(positions.Count - 1);
            positions.Insert(0, newHead);
        }

        for (int i = 0; i < segments.Count; i++)
            segments[i].transform.position = ToWorld(positions[i]);

        float yaw = Mathf.Atan2(currentDir.x, currentDir.y) * Mathf.Rad2Deg;
        segments[0].transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        AdvanceTokens();
        EnsureTailScale();
    }

    void AdvanceTokens()
    {
        for (int t = _tokens.Count - 1; t >= 0; t--)
        {
            var tok = _tokens[t];

            if (tok.seg > 1 && tok.seg - 1 < segments.Count)
                RestoreBodyVisual(tok.seg - 1);

            if (tok.seg < tok.tailIdx && tok.seg < segments.Count)
            {
                ApplyLump(tok.seg);
                tok.seg++;
            }
            else
            {
                if (tok.seg < segments.Count) RestoreBodyVisual(tok.seg);
                if (tok.tailIdx < segments.Count) RevealTail(tok.tailIdx);
                _tokens.RemoveAt(t);
            }
        }
    }

    void ApplyLump(int idx)
    {
        if (idx <= 0 || idx >= segments.Count) return;
        float s = BodyScale * 1.25f;
        segments[idx].transform.localScale = new Vector3(s, s, s);
        SetSegColor(segments[idx], FoodLumpColor);
    }

    void RestoreBodyVisual(int idx)
    {
        if (idx <= 0 || idx >= segments.Count) return;
        bool isTail = (idx == segments.Count - 1);
        float sc = isTail ? TailScale : BodyScale;
        segments[idx].transform.localScale = new Vector3(sc, sc, sc);
        SetSegColor(segments[idx], BodyColor);
    }

    void RevealTail(int idx)
    {
        if (idx >= segments.Count) return;
        segments[idx].transform.localScale = new Vector3(TailScale, TailScale, TailScale);
        SetSegColor(segments[idx], BodyColor);
    }

    void SetSegColor(GameObject go, Color col)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", col);
        r.SetPropertyBlock(mpb);
    }

    void EnsureTailScale()
    {
        if (segments.Count < 2) return;
        int tail = segments.Count - 1;
        bool tokenAtTail = false;
        foreach (var tok in _tokens)
            if (tok.tailIdx == tail) { tokenAtTail = true; break; }
        if (!tokenAtTail)
            segments[tail].transform.localScale = new Vector3(TailScale, TailScale, TailScale);
    }

    void SpawnFood()
    {
        if (food != null) Destroy(food);
        Vector2Int pos;
        int tries = 0;
        do
        {
            pos = new Vector2Int(
                UnityEngine.Random.Range(-gridHalfSize, gridHalfSize + 1),
                UnityEngine.Random.Range(-gridHalfSize, gridHalfSize + 1));
            tries++;
        } while (positions.Contains(pos) && tries < 200);

        if (positions.Contains(pos))
        {
            Debug.LogWarning("SpawnFood: no empty cell after 200 tries.");
            return;
        }

        foodGridPos = pos;
        food = Instantiate(foodPrefab, ToWorld(pos), Quaternion.identity);
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        GameOver?.Invoke(score);
        if (scoreText != null) scoreText.text = $"SCORE: {score}";
    }

    void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = $"SCORE: {score}";
    }

    Vector3 ToWorld(Vector2Int g) => new Vector3(g.x, 0.5f, g.y);

    public Vector3 HeadWorldPos => positions.Count > 0 ? ToWorld(positions[0]) : Vector3.zero;
}
