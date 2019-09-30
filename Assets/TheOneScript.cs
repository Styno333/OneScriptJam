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
    private Player _player;

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
        _player.Draw(Vector3.zero);
    }

    [System.Serializable]
    public class Agent
    {
        public float MaxHealth;
        public float CurrentHealth;
        public float MovementSpeed;

        public Transform MyTrans;
        public Rigidbody MyRigid;

        public virtual void Draw(Vector3 pos)
        {
            MyTrans = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            MyTrans.SetParent(MyTrans.transform);
            MyTrans.localPosition = new Vector3(0, 0.5f, 0);
            MyRigid = MyTrans.gameObject.AddComponent<Rigidbody>();
        }

        public virtual void Movement()
        {

        }
    }

    [System.Serializable]
    public class Player : Agent
    {
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

    }
}
