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

public class TheOneScript : MonoBehaviour
{
    public static TheOneScript ONE;
    public static Plane FloorPlane = new Plane(Vector3.up, 0);


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
    public Material PlayerMat;

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

    // -- end UI variables

    public static Camera Cam;


    void Start()
    {
        // setup
        ONE = this;
        Cam = CreateNewCamera();
        CreateNewLight();
        CreateUI();

        PlayerMat = new Material(Shader.Find("Standard"));

        // spawn level
        NewGame();


    }

    #region Scene setup
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
        var light = (new GameObject()).AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.eulerAngles = new Vector3(50, -30, 0);
        light.color = new Color(1, 0.95f, 0.84f, 1f);
        light.name = "Light";
        return light;
    }

    #endregion

    #region UI

    private void CreateUI()
    {
        _font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

        // add event system
        (new GameObject()).AddComponent<UnityEngine.EventSystems.EventSystem>().name = "EventSystem";
        // create canvas
        var canv = (new GameObject()).AddComponent<Canvas>();
        canv.name = "Canvas";
        canv.renderMode = RenderMode.ScreenSpaceOverlay;
        canv.gameObject.AddComponent<CanvasScaler>();
        canv.gameObject.AddComponent<CanvasRenderer>();
        // add text labels
        _txtLevel = AddtextLabel(canv.transform, -40, "LEVEL: 0");
        _txtEnemiesLeft = AddtextLabel(canv.transform, -70, "ENEMIES LEFT: 0");
    }

    private Text AddtextLabel(Transform canv, float height, string text)
    {
        var txt = (new GameObject()).AddComponent<Text>();
        txt.transform.SetParent(canv.transform);
        txt.font = _font;
        txt.fontStyle = FontStyle.Bold;
        txt.text = text;
        txt.rectTransform.anchoredPosition = new Vector2(40, height);

        txt.rectTransform.pivot = Vector2.up;
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;

        return txt;
    }


    #endregion

    void Update()
    {
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
        // Reset stats
        CurrentLevel = 0;
        _txtLevel.text = "Level " + 0;

        // Spawn level
        SpawnLevel();

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
            SpawnEnemies();
            _txtLevel.text = "Level " + CurrentLevel;
        }
        else
        {
            // game over!
            GameOver(true);
        }
    }

    private void SpawnLevel()
    {
        if (_floor == null)
        {
            _floor = GameObject.CreatePrimitive(PrimitiveType.Plane).transform;
            _floor.name = "Floor";
        }

        _floor.localScale = Vector3.one * _levels[CurrentLevel].MapSize;
    }

    private void SpawnEnemies()
    {
        for (int i = 0; i < _levels[CurrentLevel].AmountOfEnemies; i++)
        {
            Enemies.Add(new ChargeEnemy(new Vector3(4 - i, 0, 4f), _enemyStartStats));
        }

        _txtEnemiesLeft.text =_levels[CurrentLevel].AmountOfEnemies + " enemies left";
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

            yield return delay;
        }
    }

    private void GameOver(bool won)
    {
        IsPlaying = false;
        // temp just start a new game
        NewGame();
    }

    #region Level helper objects

    [System.Serializable]
    public struct LevelSettings
    {
        public float MapSize;
        public int AmountOfEnemies;
        public float MaxWeightToContinue;
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
            Trans.GetComponent<Renderer>().material = ONE.PlayerMat;
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

            if (Trans.position.y < 0)
            {
                Die();
            }
        }
    }



    [System.Serializable]
    public class Player : Agent
    {
        public float Weight = 0;

        public Player(Vector3 pos, AgentStartStats stats) : base(pos, stats)
        {
            Trans.name = "Player";
        }

        public override void Movement()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            input = input.normalized * MovementSpeed * Time.deltaTime;
            Rigid.MovePosition(Trans.position + input);

            if (Input.GetKeyDown(KeyCode.Space) && FloorPlane.GetDistanceToPoint(Trans.position) < 0.51f)
            {
                Rigid.AddForce(Vector3.up * 6, ForceMode.Impulse);
            }
        }

        public void Attack()
        {
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
                        Debug.Log("Hit " + hit.collider.gameObject.name, hit.collider.gameObject) ;

                        var enemy = ONE.Enemies.First(x => x.Trans == hit.collider.transform);
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
                    Rigid.AddForce(Trans.forward * 20, ForceMode.Impulse);
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
}
