using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.TextCore;
using UnityEngine.UIElements;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;

[ExecuteAlways]
public class MassPoint : MonoBehaviour
{
    /// <summary>
    /// この質点が属するブロック
    /// </summary>
    [SerializeField] public Block block;
    [SerializeField] public float _mass;
    [SerializeField] public Vector3 _velocity;
    [SerializeField] public int _index;
    [SerializeField] public Vector3 _position;
    [SerializeField] public Vector3 _force;
    [SerializeField] public Vector3 _acc;
    [SerializeField] public float move;
    [SerializeField] public int step;
    /// <summary>
    /// 質点が他の面に合体しているかどうか
    /// (注:このフラグは、被挿入ブロックの被挿入面のポケット及び目の質点のみtrueになる)
    /// </summary>
    [SerializeField] public bool _isMerged = false;
    //[SerializeField] public Vector3 _gravity = new Vector3(0, -9.8f, 0);
    /// <summary>
    /// 質点の固定フラグ
    /// </summary>
    [SerializeField] public bool _isFixed = false;
    /// <summary>
    /// この質点に接続されているばね
    /// </summary>
    [SerializeField] List<Spring> _springs;
    // Start is called before the first frame update
    void OnEnable()
    {
        //Debug.Log("Info: MassPoint awaked.");
        _springs = new List<Spring>();
    }
    void Start()
    {
        //Debug.Log("Info: MassPoint start.");
    }
    public void SetMassSpring(float mass, Vector3 velocity, int index, Vector3 position, Block block)
    {
        _mass = mass;
        _velocity = velocity;
        _index = index;
        _position = position;
        _force = Vector3.zero;
        _acc = Vector3.zero;
        this.block = block;
        // if (this._index == 2)
        // {
        //     _isFixed = true;
        // }
        this.block = block;
        step = 0;
    }
    public void AddSpring(Spring spring)
    {
        _springs.Add(spring);
    }
    /// <summary>
    /// 質点に働く力を計算する
    /// </summary>
    /// <returns></returns>
    public Vector3 CalcForce()
    {
        Vector3 force = Vector3.zero;
        int index = this._index;
        if (_isFixed)
        {
            return force;
        }
        //Debug.Log("calc force");
        foreach (Spring spring in _springs)
        {
            force += spring.GetForce(this);
        }
        return force;
    }
    // Update is called once per frame
    void Update()
    {
        if (true/*block._isFixed && !_isFixed && block._isAnimatable*/)
        {
            float dt = 0.01f;
            Vector3 acc = CalcForce() / _mass;
            _force = CalcForce();
            _acc = acc;
            //Debug.Log("f= " + _force);
            _velocity += (acc * dt);
            move = (_velocity * dt).magnitude;
            _position = _position + _velocity * dt;
            step++;
        }

    }
}
