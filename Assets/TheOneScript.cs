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
    [SerializeField]
    private Player _player;

    [Header("Enemies")]
    [SerializeField] private AgentStartStats _enemyStartStats;
    [SerializeField] private List<Enemy> _enemies = new List<Enemy>();

    public static float LevelModifier = 1;

    void Start()
    {
        // spawn floor
        GameObject.CreatePrimitive(PrimitiveType.Plane);

        NewGame();
    }

    void Update()
    {
        _player.Movement();
    }

    private void NewGame()
    {
        // spawn player
        _player = new Player(Vector3.zero, _playerStartStats);

        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        for (int i = 0; i < 8; i++)
        {
            _enemies.Add(new Enemy(new Vector3(4 - i , 0, -4.5f), _enemyStartStats));
        }
    }

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
    }

    [System.Serializable]
    public class Player : Agent
    {
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

        }
    }
}
