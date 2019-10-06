/*
 * Stijn - @DV_Stijn
 * 
 * Entry for the OnScriptJam https://itch.io/jam/osj 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TheOneScript : MonoBehaviour
{
    public static TheOneScript ONE;
    public static Plane FloorPlane = new Plane(Vector3.up, 0);

    public enum GameState
    {
        Menu,
        Playing,
        BetweenRound
    }

    private bool _isPlaying = false;
    public bool IsPlaying
    {
        get { return _isPlaying; }
        set
        {
            _isPlaying = value;
        }
    }

    [Header("Player")]
    [SerializeField]
    private AgentStartStats _playerStartStats;
    [HideInInspector] public Material Mat;

    public static Player ActivePlayer;

    [Header("Enemies")]
    [SerializeField] private AgentStartStats _enemyStartStats;
    public List<Agent> Enemies = new List<Agent>();

    // -- Level variables -- 
    public static int CurrentLevel = 0;
    public static float LevelModifier
    {
        get
        {
            return 1 + CurrentLevel * 1.2f;
        }
    }
    [SerializeField] private LevelSettings[] _levels;

    private Transform _floor;
    // -- end level variables --

    // -- UI variables
    private Font _font;
    private Canvas _canvMain;
    private Text _txtLevel;
    private Text _txtEnemiesLeft;

    private GameObject _mainMenu;
    private GameObject _skillMenu;

    // -- end UI variables

    public static Camera Cam;


    void Start()
    {
        // INITIAL SETUP
        ONE = this;
        Cam = CreateNewCamera();
        CreateNewLight();
        CreateUI();

        Mat = new Material(Shader.Find("Standard"));
    }

    #region Initial setup
    /// <summary>
    /// Create a new camera for the scene
    /// </summary>
    /// <returns></returns>
    private Camera CreateNewCamera()
    {
        var cam = (new GameObject()).AddComponent<Camera>();
        cam.transform.eulerAngles = new Vector3(40, 0, 0);
        cam.transform.position = new Vector3(0, 5, -7);
        cam.gameObject.AddComponent<AudioListener>();
        cam.name = "Camera";
        return cam;
    }
    /// <summary>
    /// Created a new directional light for the scene
    /// </summary>
    /// <returns></returns>
    private Light CreateNewLight()
    {
        var light = (new GameObject("Light")).AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.eulerAngles = new Vector3(50, -30, 0);
        light.color = new Color(1, 0.95f, 0.84f, 1f);
        light.shadows = LightShadows.Soft;
        return light;
    }
    #endregion

    #region UI

    private void CreateUI()
    {
        _font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

        // add event system
        var es = (new GameObject("EventSystem")).AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.gameObject.AddComponent<StandaloneInputModule>(); 
        // create canvas
        _canvMain = (new GameObject()).AddComponent<Canvas>();
        _canvMain.name = "Canvas";
        _canvMain.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvMain.gameObject.AddComponent<CanvasScaler>();
        _canvMain.gameObject.AddComponent<CanvasRenderer>();
        // add text labels
        _txtLevel = AddtextLabel(_canvMain.transform, new Vector2(40, -40), "LEVEL: 0", Color.white);
        _txtEnemiesLeft = AddtextLabel(_canvMain.transform, new Vector2(40, -70), "ENEMIES LEFT: 0", Color.white);


        CreateStartMenu();
        CreateSkillMenu();
    }

    private void CreateStartMenu()
    {
        _mainMenu = new GameObject();
        _mainMenu.transform.SetParent(_canvMain.transform, false);

        var rectT = _mainMenu.AddComponent<RectTransform>();
        rectT.pivot = Vector2.zero;
        rectT.anchorMin = Vector2.zero;
        rectT.anchorMax = Vector2.one;
        rectT.offsetMin = rectT.offsetMax = Vector2.zero;

        _mainMenu.AddComponent<GraphicRaycaster>();

        // add Start btn
        var btn = AddButton(_mainMenu.transform, Vector2.zero, new Vector2(200, 50), "start game");
        btn.onClick.AddListener(NewGame);
    }

    private void CreateSkillMenu()
    {
        _skillMenu = new GameObject("Skill menu");
        _skillMenu.transform.SetParent(_canvMain.transform, false);

        var rectT = _skillMenu.AddComponent<RectTransform>();
        rectT.pivot = Vector2.zero;
        rectT.anchorMin = Vector2.zero;
        rectT.anchorMax = Vector2.one;
        rectT.offsetMin = rectT.offsetMax = Vector2.zero;

        _skillMenu.AddComponent<GraphicRaycaster>();

        _skillMenu.SetActive(false);
    }

    public void ShowSkillMenu()
    {
        var lbl = AddtextLabel(_skillMenu.transform, new Vector2(0, 50), "CHOOSE A SKILL TO LEAVE BEHIND", Color.white);
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.rectTransform.localPosition = new Vector2(0, 200);
        lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;

        if (ActivePlayer.CanMoveX)
        {
            AddButton(_skillMenu.transform, new Vector2(-100, 0), new Vector2(175, 50), "horizontal movement").onClick.AddListener(
                () => 
                {
                    ActivePlayer.CanMoveX = false;
                    AfterSkillRemoved();
                });
        }
        if (ActivePlayer.CanMoveZ)
        {
            AddButton(_skillMenu.transform, new Vector2(100, 0), new Vector2(175, 50), "vertical movement").onClick.AddListener(
                () =>
                {
                    ActivePlayer.CanMoveZ = false;
                    AfterSkillRemoved();
                });
        }
        if (ActivePlayer.CanShoot)
        {
            AddButton(_skillMenu.transform, new Vector2(-100, -60), new Vector2(175, 50), "shooting").onClick.AddListener(
                () =>
                {
                    ActivePlayer.CanShoot = false;
                    AfterSkillRemoved();
                });
        }
        if (ActivePlayer.CanJump)
        {
            AddButton(_skillMenu.transform, new Vector2(100, -60), new Vector2(175, 50), "jumping").onClick.AddListener(
                () =>
                {
                    ActivePlayer.CanJump = false;
                    AfterSkillRemoved();
                });
        }

        _skillMenu.SetActive(true);
    }

    private void CloseSkillMenu()
    {
        foreach(Transform t in _skillMenu.transform)
        {
            Destroy(t.gameObject);
        }
        _skillMenu.gameObject.SetActive(false);
    }

    private Button AddButton(Transform parent, Vector2 pos, Vector2 size, string txt)
    {
        var img = (new GameObject("Button: " + txt)).gameObject.AddComponent<Image>();
        var btn = img.gameObject.AddComponent<Button>();

        btn.transform.SetParent(parent, false);
        img.rectTransform.anchoredPosition = pos;

        img.rectTransform.sizeDelta = size;

        var lbl = AddtextLabel(btn.transform, Vector2.zero, txt, Color.black);
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.rectTransform.localPosition = Vector2.zero;
        lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;

        lbl.rectTransform.pivot = Vector2.zero;

        return btn;
    }

    private Text AddtextLabel(Transform canv, Vector2 pos, string text, Color c)
    {
        var txt = (new GameObject()).AddComponent<Text>();
        txt.transform.SetParent(canv.transform, false);
        txt.font = _font;
        txt.fontStyle = FontStyle.Bold;
        txt.text = text;
        txt.rectTransform.anchoredPosition = pos;
        txt.color = c;

        txt.rectTransform.pivot = Vector2.up;
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;

        return txt;
    }


    #endregion

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (!_isPlaying) return;

        ActivePlayer.Movement();
        ActivePlayer.Attack();

        foreach (var agent in Enemies)
        {
            agent.Movement();
        }
    }

    private void FixedUpdate()
    {
        if (!_isPlaying) return;

        CameraMovement();
    }

    private void NewGame()
    {
        // close menu
        _mainMenu.SetActive(false);

        // Reset stats
        CurrentLevel = 0;
        _txtLevel.text = "Level " + 0;

        // Spawn level
        UpdateMap();

        // Spawn player
        ActivePlayer = new Player(Vector3.zero, _playerStartStats);

        // Spawn enemies
        SpawnEnemies();

        // set is playing
        IsPlaying = true;

        // start routines
        StartCoroutine(CheckForDroppedAgentsRoutine()); // checks for out of bounds agents

    }

    /// <summary>
    /// Call when all enemies have been killed
    /// </summary>
    private void LevelCompleted()
    {
        if (CurrentLevel < _levels.Length - 1)
        {
            CurrentLevel++;
            UpdateMap();

            // reset player pos
            ActivePlayer.Trans.position = new Vector3(0,0.75f,0);

            _txtLevel.text = "Level " + CurrentLevel;

            ShowSkillMenu();
            IsPlaying = false;
        }
        else
        {
            // game over!
            GameOver(true);
        }
    }

    public void AfterSkillRemoved()
    {
        IsPlaying = true;
        SpawnEnemies();
        CloseSkillMenu();
        StartCoroutine(CheckForDroppedAgentsRoutine()); // checks for out of bounds agents

    }

    private void UpdateMap()
    {
        // todo: make floor a circle

        if (_floor == null)
        {
            _floor = GameObject.CreatePrimitive(PrimitiveType.Plane).transform;
            _floor.GetComponent<MeshFilter>().mesh = _floor.GetComponent<MeshCollider>().sharedMesh = CircleMesh();
            _floor.name = "Floor";
        }

        _floor.localScale = Vector3.one * _levels[CurrentLevel].MapSize;
    }

    private void SpawnEnemies()
    {
        // todo: get total enemies to spawn
        // spawn them on a circle around player
        var anglePerEnemy = Mathf.PI * 2f / 10;

        foreach (var e in _levels[CurrentLevel].Wave)
        {
            for (int i = 0; i < e.Amount; i++)
            {
                var angle = i * anglePerEnemy;
                var pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)); // new Vector3(4 - i, 0, 4f)
                Enemies.Add(NewAgent(e.EnemyType, pos * (_levels[CurrentLevel].MapSize - 1), e.BasicStats));
            }
        }

        _txtEnemiesLeft.text = Enemies.Count + " enemies left";
    }

    public void AgentDied(Agent e)
    {
        if (e is Player)
        {
            // Game over
            GameOver(false);
        }
        else
        {
            Enemies.Remove(e);
            _txtEnemiesLeft.text = Enemies.Count + " enemies left";
            if (Enemies.Count <= 0)
            {
                // level completed;
                LevelCompleted();
            }
        }
    }

    private void RemoveAllRemainingEnemies()
    {
        foreach (var enemy in Enemies)
        {
            Destroy(enemy.Trans.gameObject);
        }
        Enemies.Clear();
    }

    /// <summary>
    /// Camera movement logic. Call each fixed update
    /// </summary>
    private void CameraMovement()
    {
        Vector3 offset = new Vector3(0, 5, -7);
        Vector3 newPos = ActivePlayer.Trans.position + offset;
        Vector3 smooth = Vector3.Lerp(Cam.transform.position, newPos, 0.25f);
        Cam.transform.position = smooth;
    }

    /// <summary>
    /// Check if any agent has fallen of the platform and needs to die
    /// </summary>
    private IEnumerator CheckForDroppedAgentsRoutine()
    {
        var delay = new WaitForSeconds(2);

        while (IsPlaying)
        {
            // check player
            ActivePlayer.CheckIfFallenOfPlatform();
            // check all enemies
            for (int i = Enemies.Count - 1; i >= 0; i--)
                Enemies[i].CheckIfFallenOfPlatform();
            Debug.Log("check");
            yield return delay;
        }
    }

    private void GameOver(bool won)
    {
        IsPlaying = false;
        RemoveAllRemainingEnemies();
        _mainMenu.SetActive(true);
    }

    #region Level helper objects

    [System.Serializable]
    public struct LevelSettings
    {
        public float MapSize;
        public List<EnemySettingsForLevel> Wave;
    }
    /// <summary>
    /// So this is a seperate object just so I can enter the correct values in the inspector without a dictionary..
    /// </summary>
    [System.Serializable]
    public struct EnemySettingsForLevel
    {
        public AgentType EnemyType;
        public int Amount;
        public AgentStartStats BasicStats;
    }

    #endregion

    #region Agent helper objects

    public enum AgentType
    {
        Enemy,
        ChargeEnemy
    }

    public static Agent NewAgent(AgentType t, Vector3 pos, AgentStartStats stats)
    {
        Agent a;
        switch(t)
        {
            case AgentType.Enemy:
                a = new Enemy(pos, stats);
                break;

            case AgentType.ChargeEnemy:
                a = new ChargeEnemy(pos, stats);
                break;
            default:
                a = null;
                break;
        }
        return a;
    }

    [System.Serializable]
    public struct AgentStartStats
    {
        public float StartHealth;
        public float StartSpeed;
        public float Weight;
        public Color Col;
    }

    [System.Serializable]
    public abstract class Agent
    {
        public float MaxHealth;
        public float CurrentHealth;
        public float MovementSpeed;

        public Transform Trans;
        public Rigidbody Rigid;

        public Agent(Vector3 pos, AgentStartStats stats)
        {
            MaxHealth = CurrentHealth = stats.StartHealth;
            MovementSpeed = stats.StartSpeed;

            Draw(pos);

            Trans.GetComponent<Rigidbody>().mass = stats.Weight;
            Trans.GetComponent<Renderer>().material = ONE.Mat;
            Trans.GetComponent<Renderer>().material.color = stats.Col;
        }

        public virtual void Draw(Vector3 pos)
        {
            Trans = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            Trans.SetParent(Trans.transform);
            Trans.localPosition = pos + new Vector3(0, 0.5f, 0);
            Rigid = Trans.gameObject.AddComponent<Rigidbody>();
        }

        public abstract void Movement();

        protected virtual void Die()
        {
            ONE.AgentDied(this);
            Trans.GetComponent<Collider>().enabled = false;
            Destroy(Trans.gameObject, 2);
        }

        public virtual void GetHit(float damage)
        {
            if(CurrentHealth - damage <= 0)
            {
                CurrentHealth = 0;
                Die();
            }
            else
            {
                CurrentHealth -= damage;
            }
        }

        public void CheckIfFallenOfPlatform()
        {
            if (Trans == null) return;

            if (Trans.position.y < 0 || Trans.position.y > 20) // > 20 check added as a bugfix
            {
                Die();
            }
        }
    }



    [System.Serializable]
    public class Player : Agent
    {
        public bool CanMoveX, CanMoveZ, CanShoot, CanJump;

        public Player(Vector3 pos, AgentStartStats stats) : base(pos, stats)
        {
            Trans.name = "Player";
            CanMoveX = CanMoveZ = CanShoot = CanJump = true;
        }

        public override void Movement()
        {
            var x = CanMoveX ? Input.GetAxisRaw("Horizontal") : 0;
            var z = CanMoveZ ? Input.GetAxisRaw("Vertical") : 0;
            var input = new Vector3(x, 0, z);
            input = input.normalized * MovementSpeed * Time.deltaTime;
            Rigid.MovePosition(Trans.position + input);

            if (CanJump && Input.GetKeyDown(KeyCode.Space) && FloorPlane.GetDistanceToPoint(Trans.position) < 0.51f)
            {
                Rigid.AddForce(Vector3.up * 6, ForceMode.Impulse);
            }
        }

        public void Attack()
        {
            if (!CanShoot) return;

            if(Input.GetMouseButton(0))
            {
                // raycast from center of player towards mouse
                Ray r = Cam.ScreenPointToRay(Input.mousePosition);
                float enter;

                if (FloorPlane.Raycast(r, out enter))
                {
                    var pos = r.GetPoint(enter) + Vector3.up * 0.5f;
                    var dir = (pos - Trans.position).normalized;

                    RaycastHit hit;
                    if(Physics.Raycast(Trans.position, dir, out hit, 10))
                    {
                        //Debug.Log("Hit " + hit.collider.gameObject.name, hit.collider.gameObject) ;

                        var enemy = ONE.Enemies.FirstOrDefault(x => x.Trans == hit.collider.transform);
                        if (enemy == default) return;
                        // apply damage
                        enemy.GetHit(1);
                        // apply knockback
                        enemy.Trans.GetComponent<Rigidbody>().AddForce(dir * 10);
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class Enemy : Agent
    {
        public Enemy(Vector3 pos, AgentStartStats stats) : base (pos, stats)
        {
            Trans.name = "enemy";
            MaxHealth = CurrentHealth *= LevelModifier;
        }

        public override void GetHit(float damage)
        {
            base.GetHit(damage);

            ONE.StartCoroutine(HitAnimation());
        }

        public override void Movement()
        {
            var dir = (ActivePlayer.Trans.position - Trans.position).normalized;
            dir = dir.normalized * MovementSpeed * Time.deltaTime;
            Rigid.MovePosition(Trans.position + dir);
        }

        private IEnumerator HitAnimation()
        {
            var delay = new WaitForSeconds(0.05f);
            Trans.localScale = new Vector3(0.75f, 1.25f, 0.75f);
            yield return delay;
            Trans.localScale = Vector3.one;
        }
    }

    public class ChargeEnemy : Agent
    {
        private float _chargePercentage = 0;
        private bool _isChargeing = false;

        public ChargeEnemy(Vector3 pos, AgentStartStats stats):base (pos, stats)
        {
            Trans.name = "charge enemy";
            MaxHealth = CurrentHealth *= LevelModifier;
        }

        public override void Movement()
        {
            if(_isChargeing)
            {
                _chargePercentage += Time.deltaTime * 20;
                if(_chargePercentage >= 100)
                {
                    _isChargeing = false;
                    Rigid.AddForce(Trans.forward * 30, ForceMode.Impulse);
                }

                Trans.LookAt(ActivePlayer.Trans);
            }
            else
            {
                _chargePercentage -= Time.deltaTime * 20;
                if (_chargePercentage <= 0)
                {
                    _isChargeing = true;
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// Creates a basic circle mesh
    /// </summary>
    /// <returns></returns>
    public Mesh CircleMesh()
    {
        Mesh m = new Mesh();
        int segments = 32;

        // vertices
        int vertCount = segments + 2;
        int indCount = segments * 3;
        var segmentAngle = Mathf.PI * 2f / segments;
        var angle = 0f;
        var verts = new List<Vector3>(vertCount);
        var normals = new List<Vector3>(vertCount);
        var indices = new int[indCount];
        verts.Add(Vector3.zero);
        normals.Add(Vector3.up);
        for (int i = 1; i < vertCount; ++i)
        {
            verts.Add(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            normals.Add(Vector3.up);

            angle -= segmentAngle;
            if(i > 1)
            {
                var j = (i - 2) * 3;
                indices[j + 0] = 0;
                indices[j + 1] = i - 1;
                indices[j + 2] = i;
            }
        }
        
        m.SetVertices(verts);
        m.SetNormals(normals);
        m.SetIndices(indices, MeshTopology.Triangles, 0);
        m.RecalculateBounds();

        return m;
    }
}
