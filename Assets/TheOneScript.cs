/*
 * Stijn - @DV_Stijn
 * 
 * Entry for the OnScriptJam https://itch.io/jam/osj 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheOneScript : MonoBehaviour
{
    [Header("Player")]
    [SerializeField]
    private AgentStartStats _playerStartStats;

    public static Player ActivePlayer;

    [Header("Enemies")]
    [SerializeField] private AgentStartStats _enemyStartStats;
    public static List<Enemy> Enemies = new List<Enemy>();

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


    void Start()
    {
        // spawn floor
        NewGame();
    }

    void Update()
    {
        ActivePlayer.Movement();

        foreach (var agent in Enemies)
        {
            agent.Movement();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {

        }
    }

    private void NewGame()
    {
        // Reset stats
        CurrentLevel = 0;

        // Spawn level
        SpawnLevel();

        // Spawn player
        ActivePlayer = new Player(Vector3.zero, _playerStartStats);

        // Spawn enemies
        SpawnEnemies();
    }

    private void SpawnLevel()
    {
        if(_floor == null)
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
            Enemies.Add(new Enemy(new Vector3(4 - i , 0, 4f), _enemyStartStats));
        }
    }

    public static void EnemyDied(Enemy e)
    {
        Enemies.Remove(e);
        if(Enemies.Count <= 0)
        {
            // level completed;
        }
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

    [System.Serializable]
    public struct AgentStartStats
    {
        public float StartHealth;
        public float StartSpeed;

    }

    [System.Serializable]
    public class Agent
    {
        public float MaxHealth;
        public float CurrentHealth;
        public float MovementSpeed;

        public Transform MyTrans;
        public Rigidbody MyRigid;

        public Agent(Vector3 pos, AgentStartStats stats)
        {
            MaxHealth = CurrentHealth = stats.StartHealth;
            MovementSpeed = stats.StartSpeed;

            Draw(pos);
        }

        public virtual void Draw(Vector3 pos)
        {
            MyTrans = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            MyTrans.SetParent(MyTrans.transform);
            MyTrans.localPosition = pos + new Vector3(0, 0.5f, 0);
            MyRigid = MyTrans.gameObject.AddComponent<Rigidbody>();
        }

        public virtual void Movement()
        {

        }

        public virtual void Die()
        {
            
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
    }

    [System.Serializable]
    public class Player : Agent
    {
        public float Weight = 0;

        public Player(Vector3 pos, AgentStartStats stats) : base(pos, stats)
        {
            MyTrans.name = "Player";
        }

        public override void Movement()
        {
            base.Movement();

            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            input = input.normalized * MovementSpeed * Time.deltaTime;
            MyRigid.MovePosition(MyTrans.position + input);
        }
    }

    [System.Serializable]
    public class Enemy : Agent
    {
        public Enemy(Vector3 pos, AgentStartStats stats) : base (pos, stats)
        {
            MyTrans.name = "enemy";
            MaxHealth = CurrentHealth *= LevelModifier;
        }

        public override void Die()
        {
            base.Die();

            EnemyDied(this);
            MyTrans.GetComponent<Collider>().enabled = false;
            Destroy(MyTrans.gameObject, 2);
        }

        public override void Movement()
        {
            base.Movement();

            var dir = (ActivePlayer.MyTrans.position - MyTrans.position).normalized;
            dir = dir.normalized * MovementSpeed * Time.deltaTime;
            MyRigid.MovePosition(MyTrans.position + dir);
        }
    }

    #endregion
}
