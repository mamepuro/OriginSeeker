using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
//using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Specialized;
using Unity.VisualScripting;
using Vector3 = UnityEngine.Vector3;
using UnityEngine.TestTools;
public enum ConnectDirection
{
    Left,
    UpperRight,
    UpperLeft,
    LowerRight,
    LowerLeft,
    UP,

}

/// <summary>
/// 頂点名(背側を正面とする)
/// </summary>

public class VertexName
{
    public const int RightPocket = 0; //0
    public const int RightEye = 1; // 1
    public const int RightLeg = 2; // 2
    public const int LeftPocket = 3; // 3
    public const int LeftEye = 4; // 4
    public const int LeftLeg = 5; // 5

}

[ExecuteAlways]
public class Block : MonoBehaviour
{
    [SerializeField] public Mesh mesh;
    [SerializeField] List<Vector3> v;
    [SerializeField] public int ID;
    /// <summary>
    /// 新しく頂点を追加するための一時的な頂点リスト
    /// </summary>
    [SerializeField] Vector3[] _tmpVertices;
    [SerializeField] public List<Spring> _springs;
    [SerializeField] public List<MassPoint> _massPoints;
    [SerializeField] public bool _isFixed = false;
    [SerializeField] public bool _isAnimatable = true;
    [SerializeField] public bool _isJoiningPrimitive = false;
    [SerializeField] int _unionID = -1;
    /// <summary>
    /// シーンマネージャーへの参照
    /// </summary>
    [SerializeField]
    public
    DefaultScene defaultScene;
    bool initial = true;
    float initTime = 0;
    const int _leftLegIndex = 2;
    const int _rightLegIndex = 5;
    /// <summary>
    /// ブロックの頂点間に貼るバネの初期インデックス(四角形面は対角線上にも張っている)
    /// </summary>
    readonly int[,] _initialSpringIndex = {
        { VertexName.RightPocket, VertexName.RightEye },
        { VertexName.RightEye, VertexName.RightLeg },
        { VertexName.RightLeg, VertexName.RightPocket },
        { VertexName.RightEye, VertexName.LeftEye },
        { VertexName.RightPocket, VertexName.LeftPocket},
        { VertexName.LeftPocket, VertexName.LeftEye },
        { VertexName.LeftEye, VertexName.LeftLeg },
        { VertexName.LeftLeg, VertexName.LeftPocket },
        { VertexName.RightPocket, VertexName.LeftEye },
        { VertexName.RightEye, VertexName.LeftPocket },
    };
    /// <summary>
    /// ブロックの足間に貼るバネの初期インデックス
    /// </summary>
    readonly int[,] _legSpring = { { VertexName.RightLeg, VertexName.LeftLeg } };
    /// <summary>
    /// 左足を挿入しているブロック
    /// </summary>
    [SerializeField] public Block _leftLegInsertedBlock;

    /// <summary>
    /// 右足を挿入しているブロック
    /// </summary>
    [SerializeField] public Block _rightLegInsertedBlock;

    /// <summary>
    /// 左ポケットに足を挿入しているブロック
    /// </summary>
    [SerializeField] public List<Block> _leftPocketInsertingBlock;

    /// <summary>
    /// 右ポケットに足を挿入しているブロック
    /// </summary>
    [SerializeField] public List<Block> _rightPocketInsertingBlock;

    /// <summary>
    /// 左足を挿入しているブロック
    /// </summary>
    [SerializeField] public Block _leftLegInsertingBlock;

    /// <summary>
    /// 右足を挿入しているブロック
    /// </summary>
    [SerializeField] public Block _rightLegInsertingBlock;

    /// <summary>
    /// 左足を挿入しているブロック
    /// </summary>
    [SerializeField] public int _leftLegInsertingBlockID = -1;

    /// <summary>
    /// 右足を挿入しているブロック
    /// </summary>
    [SerializeField] public int _rightLegInsertingBlockID = -1;
    /// <summary>
    /// 右ポケットに脚を挿入しているブロックのうち、最も最下層にあるブロック
    /// </summary>
    [SerializeField] public Block _rootRightPocketBlock = null;
    /// <summary>
    /// 左ポケットに脚を挿入しているブロックのうち、最も最下層にあるブロック
    /// </summary>
    [SerializeField] public Block _rootLeftPocketBlock = null;
    [SerializeField] public int _rootRightPocketBlockID = -1;
    [SerializeField] public int _rootLeftPocketBlockID = -1;

    [SerializeField] public int _rootRightPocketBlockVertexName = -1;
    [SerializeField] public int _rootLeftPocketBlockVertexName = -1;
    [SerializeField] public float _margin = 0.08533333333f;

    const float _dampingConstant = 0;
    const float _springConstant = 10.0f;
    [SerializeField] public float _springConstantLeg = 10.0f;

    const float _restLength = 0.1f;
    bool _isDebug = false;

    public static float blockVallaySize = 4.454382f - 4.378539f;
    public static float margin = 4.378539f - 3.99421f;

    public void OnEnable()
    {
        _leftPocketInsertingBlock = new List<Block>();
        _rightPocketInsertingBlock = new List<Block>();
        //頂点数は6固定
        _tmpVertices = new Vector3[6];
    }

    /// <summary>
    /// 毎フレームレンダリングする
    /// </summary>
    private void OnRenderObject()
    {
        if (!Application.isPlaying)
        {

            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();

        }
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdateVertices();
    }
    public void OnDestroy()
    {
        Debug.Log("Block is destroyed");
        CreateButtonUi._blocks.Remove(this);
    }
    public void SetVertices()
    {
        if (this.mesh != null)
        {
            mesh = GetComponent<MeshFilter>().sharedMesh;
            v = new List<Vector3>();
            foreach (Vector3 v3 in mesh.vertices)
            {
                v.Add(transform.TransformPoint(v3));
            }
            //Debug.Log("Info: Block awaked. ID is " + ID);
            Initiate();
        }
    }
    /// <summary>
    /// ブロックを挿入時のモデルに変換する
    /// </summary>
    public void TransformInsertionModel()
    {
        Debug.Log("TransformInsertionModel is called");
        Debug.Log("On Space key is pressed");
        int i = 0;
        foreach (var vertex in mesh.vertices)
        {
            _tmpVertices[i] = vertex;
            i++;
        }
        //testbending
        _tmpVertices[VertexName.RightLeg] = new Vector3(_tmpVertices[VertexName.RightPocket].x, _tmpVertices[VertexName.RightLeg].y, _tmpVertices[VertexName.RightLeg].z);
        _tmpVertices[VertexName.LeftLeg] = new Vector3(_tmpVertices[VertexName.LeftPocket].x, _tmpVertices[VertexName.LeftLeg].y, _tmpVertices[VertexName.LeftLeg].z);
        mesh.SetVertices(_tmpVertices);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        if (_massPoints.Count != 0)
        {
            _massPoints[VertexName.RightLeg]._position = new Vector3(_massPoints[VertexName.RightPocket]._position.x, _massPoints[VertexName.RightLeg]._position.y, _massPoints[VertexName.RightLeg]._position.z);
            _massPoints[VertexName.LeftLeg]._position = new Vector3(_massPoints[VertexName.LeftPocket]._position.x, _massPoints[VertexName.LeftLeg]._position.y, _massPoints[VertexName.LeftLeg]._position.z);
            foreach (var spring in _springs)
            {
                //バネの張り直し
                if (spring._massPointIndexes.Contains(2) && spring._massPointIndexes.Contains(5))
                {
                    spring._springLength = Vector3.Distance(_massPoints[VertexName.RightLeg]._position, _massPoints[VertexName.LeftLeg]._position);
                }
            }
        }
    }
    public void OnSpaceKeyPress()
    {
        initTime = Time.time;
        _isDebug = !_isDebug;
        Debug.Log("On Space key is pressed");
        int i = 0;
        foreach (var vertex in mesh.vertices)
        {
            _tmpVertices[i] = vertex;
            i++;
        }
        //testbending
        _tmpVertices[VertexName.RightLeg] = new Vector3(_tmpVertices[VertexName.RightPocket].x, _tmpVertices[VertexName.RightLeg].y, _tmpVertices[VertexName.RightLeg].z);
        _massPoints[VertexName.RightLeg]._position = new Vector3(_massPoints[VertexName.RightPocket]._position.x, _massPoints[VertexName.RightLeg]._position.y, _massPoints[VertexName.RightLeg]._position.z);
        _tmpVertices[VertexName.LeftLeg] = new Vector3(_tmpVertices[VertexName.LeftPocket].x, _tmpVertices[VertexName.LeftLeg].y, _tmpVertices[VertexName.LeftLeg].z);
        _massPoints[VertexName.LeftLeg]._position = new Vector3(_massPoints[VertexName.LeftPocket]._position.x, _massPoints[VertexName.LeftLeg]._position.y, _massPoints[VertexName.LeftLeg]._position.z);
        mesh.SetVertices(_tmpVertices);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        //Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        //UpdateVertices();
    }
    public void OnUpKeyPress()
    {

    }
    void UpdateVertices()
    {
        if (_massPoints.Count == 0)
        {
            //Debug.Log("massPoints.Count is 0");
            v.Clear();
            foreach (Vector3 v3 in mesh.vertices)
            {
                v.Add(transform.TransformPoint(v3));
            }
        }
        else if (_isAnimatable)
        {
            v.Clear();
            int i = 0;
            if (_leftPocketInsertingBlock.Count != 0
            && _rightPocketInsertingBlock.Count == 0)
            {
                //Debug.Log("leftPocketInsertingBlock.Count is " + _leftPocketInsertingBlock.Count + " ID is " + this.ID);
                foreach (var m in _massPoints)
                {
                    if ((i == 3 || i == 4))
                    {
                        //挿入ブロックのポケットから脚に向かうベクトル　* _margin分をポケットの頂点座標に固定する
                        var pos =
                        (_leftPocketInsertingBlock[0]._massPoints[VertexName.RightLeg]._position - _leftPocketInsertingBlock[0]._massPoints[VertexName.RightPocket]._position).normalized * _margin
                        + _leftPocketInsertingBlock[0]._massPoints[i - 3]._position;
                        _massPoints[i]._position = pos;
                        v.Add(pos);
                        //ワールド座標からローカル座標に変換する
                        _tmpVertices[i] = transform.InverseTransformPoint(pos);
                        i++;
                    }

                    else
                    {
                        v.Add(m._position);
                        //ワールド座標からローカル座標に変換する
                        _tmpVertices[i] = transform.InverseTransformPoint(m._position);
                        i++;
                    }
                }
            }
            else if (_leftPocketInsertingBlock.Count == 0
            && _rightPocketInsertingBlock.Count != 0)
            {
                //Debug.Log("leftPocketInsertingBlock.Count is " + _leftPocketInsertingBlock.Count + " ID is " + this.ID);
                foreach (var m in _massPoints)
                {
                    if ((i == 0 || i == 1))
                    {
                        //ポケットから脚に向かうベクトル　* _margin分をポケットの頂点座標に固定する
                        var pos = (_rightPocketInsertingBlock[0]._massPoints[VertexName.LeftLeg]._position - _rightPocketInsertingBlock[0]._massPoints[VertexName.LeftPocket]._position).normalized * _margin
                        + _rightPocketInsertingBlock[0]._massPoints[i + 3]._position;
                        _massPoints[i]._position = pos;
                        v.Add(pos);
                        //ワールド座標からローカル座標に変換する
                        _tmpVertices[i] = transform.InverseTransformPoint(pos);
                        i++;
                    }

                    else
                    {
                        v.Add(m._position);
                        //ワールド座標からローカル座標に変換する
                        _tmpVertices[i] = transform.InverseTransformPoint(m._position);
                        i++;
                    }
                }
            }
            else if (_leftPocketInsertingBlock.Count != 0
            && _rightPocketInsertingBlock.Count != 0)
            {
                foreach (var m in _massPoints)
                {
                    if ((i == 0 || i == 1))
                    {
                        //ポケットから脚に向かうベクトル　* _margin分をポケットの頂点座標に固定する
                        var pos = (_rightPocketInsertingBlock[0]._massPoints[VertexName.LeftLeg]._position - _rightPocketInsertingBlock[0]._massPoints[VertexName.LeftPocket]._position).normalized * _margin
                        + _rightPocketInsertingBlock[0]._massPoints[i + 3]._position;
                        _massPoints[i]._position = pos;
                        v.Add(pos);
                        //ワールド座標からローカル座標に変換する
                        _tmpVertices[i] = transform.InverseTransformPoint(pos);
                        i++;
                    }
                    else if ((i == 3 || i == 4))
                    {
                        //ポケットから脚に向かうベクトル　* _margin分をポケットの頂点座標に固定する
                        var pos = (_leftPocketInsertingBlock[0]._massPoints[VertexName.RightLeg]._position - _leftPocketInsertingBlock[0]._massPoints[VertexName.RightPocket]._position).normalized * _margin + _leftPocketInsertingBlock[0]._massPoints[i - 3]._position;
                        _massPoints[i]._position = pos;
                        v.Add(pos);
                        //ワールド座標からローカル座標に変換する
                        _tmpVertices[i] = transform.InverseTransformPoint(pos);
                        i++;
                    }
                    else
                    {
                        v.Add(m._position);
                        //ワールド座標からローカル座標に変換する
                        _tmpVertices[i] = transform.InverseTransformPoint(m._position);
                        i++;
                    }
                }
            }
            else
            {
                bool isFished = true;
                int step = 0;
                foreach (var m in _massPoints)
                {

                    v.Add(m._position);

                    if (_isDebug)
                    {
                        // Debug.Log("move is " + m.move);
                        if (m.move >= 0.00005 || m.step < 1500)
                        {
                            //Debug.Log("move is " + m.move + "step is " + step);

                            isFished = false;
                        }
                        if (Time.time - initTime > 0.1)
                        {
                            if (step == 0)
                            {
                                Debug.Break();
                            }
                        }
                    }

                    //ワールド座標からローカル座標に変換する
                    _tmpVertices[i] = transform.InverseTransformPoint(m._position);
                    i++;
                    step = m.step;
                }
                if (_isDebug)
                {
                    if (isFished)
                    {
                        if (initial)
                        {
                            Debug.Log("Assert step is " + step);
                            Debug.Log("Time is " + (Time.time - initTime));
                            initial = false;
                        }
                    }
                }


            }
            mesh.SetVertices(_tmpVertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
        }
    }
    void OnChanged()
    {
        UpdateMassPointPosition();
    }
    void Initiate()
    {
        //リストの初期化
        _springs = new List<Spring>();
        _massPoints = new List<MassPoint>();
        //Debug.Log(v.Count);
        for (int i = 0; i < v.Count; i++)
        {
            var massPoint = gameObject.AddComponent<MassPoint>();
            massPoint.SetMassSpring(30.0f, Vector3.zero, i, v[i], this);
            _massPoints.Add(massPoint);
        }
        for (int i = 0; i < _initialSpringIndex.GetLength(0); i++)
        {
            var spring = gameObject.AddComponent<Spring>();
            var massPoint1 = _massPoints[_initialSpringIndex[i, 0]];
            var massPoint2 = _massPoints[_initialSpringIndex[i, 1]];
            //TODO: distanceは遅いのでmagintudeを使う
            var initialLength = Vector3.Distance(massPoint1._position, massPoint2._position);
            spring.SetSpring(massPoint1, massPoint2,
            _springConstant, springLength: initialLength, 20.0f, 1.0f, springType: SpringType.Leg);
            _springs.Add(spring);
            massPoint1.AddSpring(spring);
            massPoint2.AddSpring(spring);
        }
        for (int i = 0; i < _legSpring.GetLength(0); i++)
        {
            var spring = gameObject.AddComponent<Spring>();
            var massPoint1 = _massPoints[_legSpring[i, 0]];
            var massPoint2 = _massPoints[_legSpring[i, 1]];
            var initialLength = Vector3.Distance(massPoint1._position, massPoint2._position);
            spring.SetSpring(massPoint1, massPoint2,
            _springConstantLeg, springLength: initialLength, 20.0f, 1.0f, springType: SpringType.Leg);
            _springs.Add(spring);
            massPoint1.AddSpring(spring);
            massPoint2.AddSpring(spring);
        }
    }
    public void UpdateValleySize(float diff)
    {
        int i = 0;
        foreach (var vertex in mesh.vertices)
        {
            _tmpVertices[i] = vertex;
            i++;
        }
        //testbending
        var extra = diff / 2;
        _tmpVertices[VertexName.RightPocket] = new Vector3(_tmpVertices[VertexName.RightPocket].x - extra, _tmpVertices[VertexName.RightPocket].y, _tmpVertices[VertexName.RightPocket].z);
        _massPoints[VertexName.RightPocket]._position = new Vector3(_massPoints[VertexName.RightPocket]._position.x - extra, _massPoints[VertexName.RightPocket]._position.y, _massPoints[VertexName.RightPocket]._position.z);
        _tmpVertices[VertexName.RightEye] = new Vector3(_tmpVertices[VertexName.RightEye].x - extra, _tmpVertices[VertexName.RightEye].y, _tmpVertices[VertexName.RightEye].z);
        _massPoints[VertexName.RightEye]._position = new Vector3(_massPoints[VertexName.RightEye]._position.x - extra, _massPoints[VertexName.RightEye]._position.y, _massPoints[VertexName.RightEye]._position.z);
        _tmpVertices[VertexName.RightLeg] = new Vector3(_tmpVertices[VertexName.RightLeg].x - extra, _tmpVertices[VertexName.RightLeg].y, _tmpVertices[VertexName.RightLeg].z);
        _massPoints[VertexName.RightLeg]._position = new Vector3(_massPoints[VertexName.RightLeg]._position.x - extra, _massPoints[VertexName.RightLeg]._position.y, _massPoints[VertexName.RightLeg]._position.z);
        _tmpVertices[VertexName.LeftPocket] = new Vector3(_tmpVertices[VertexName.LeftPocket].x + extra, _tmpVertices[VertexName.LeftPocket].y, _tmpVertices[VertexName.LeftPocket].z);
        _massPoints[VertexName.LeftPocket]._position = new Vector3(_massPoints[VertexName.LeftPocket]._position.x + extra, _massPoints[VertexName.LeftPocket]._position.y, _massPoints[VertexName.LeftPocket]._position.z);
        _tmpVertices[VertexName.LeftEye] = new Vector3(_tmpVertices[VertexName.LeftEye].x + extra, _tmpVertices[VertexName.LeftEye].y, _tmpVertices[VertexName.LeftEye].z);
        _massPoints[VertexName.LeftEye]._position = new Vector3(_massPoints[VertexName.LeftEye]._position.x + extra, _massPoints[VertexName.LeftEye]._position.y, _massPoints[VertexName.LeftEye]._position.z);
        _tmpVertices[VertexName.LeftLeg] = new Vector3(_tmpVertices[VertexName.LeftLeg].x + extra, _tmpVertices[VertexName.LeftLeg].y, _tmpVertices[VertexName.LeftLeg].z);
        _massPoints[VertexName.LeftLeg]._position = new Vector3(_massPoints[VertexName.LeftLeg]._position.x + extra, _massPoints[VertexName.LeftLeg]._position.y, _massPoints[VertexName.LeftLeg]._position.z);
        mesh.SetVertices(_tmpVertices);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        foreach (var spring in _springs)
        {
            //バネの張り直し
            spring._springLength = Vector3.Distance(_massPoints[spring._massPointIndexes[0]]._position, _massPoints[spring._massPointIndexes[1]]._position);
        }
        //Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        //UpdateVertices();
    }
    public void UpdateMassPointPosition()
    {
        int i = 0;
        v.Clear();
        foreach (Vector3 v3 in mesh.vertices)
        {
            v.Add(transform.TransformPoint(v3));
        }
        foreach (var vertex in v)
        {
            _massPoints[i]._position = vertex;
            i++;
        }
        int j = 9;
    }
    public void ReSpring(Vector3[] vertices, ConnectDirection whichSpring, Block refBlock)
    {
        _massPoints.Clear();
        _springs.Clear();
        switch (whichSpring)
        {
            case ConnectDirection.UpperRight:
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (i == 2)
                    {
                        _massPoints.Add(refBlock._massPoints[VertexName.LeftLeg]);
                    }

                    else
                    {
                        var massPoint = gameObject.AddComponent<MassPoint>();
                        massPoint.SetMassSpring(30.0f, Vector3.zero, i, transform.TransformPoint(vertices[i]), this);
                        _massPoints.Add(massPoint);
                        if (i == 0 || i == 1)
                        {
                            massPoint._isFixed = true;
                        }
                    }

                }
                for (int i = 0; i < _initialSpringIndex.GetLength(0); i++)
                {
                    var spring = gameObject.AddComponent<Spring>();
                    var massPoint1 = _massPoints[_initialSpringIndex[i, 0]];
                    var massPoint2 = _massPoints[_initialSpringIndex[i, 1]];
                    //TODO: distanceは遅いのでmagintudeを使う
                    var initialLength = Vector3.Distance(massPoint1._position, massPoint2._position);
                    spring.SetSpring(massPoint1, massPoint2,
                    _springConstant, springLength: initialLength, 20.0f, 1.0f, springType: SpringType.Leg);
                    _springs.Add(spring);
                    massPoint1.AddSpring(spring);
                    massPoint2.AddSpring(spring);
                }
                for (int i = 0; i < _legSpring.GetLength(0); i++)
                {
                    var spring = gameObject.AddComponent<Spring>();
                    var massPoint1 = _massPoints[_legSpring[i, 0]];
                    var massPoint2 = _massPoints[_legSpring[i, 1]];
                    var initialLength = Vector3.Distance(massPoint1._position, massPoint2._position);
                    spring.SetSpring(massPoint1, massPoint2,
                    _springConstantLeg, springLength: initialLength, 20.0f, 1.0f, springType: SpringType.Leg);
                    _springs.Add(spring);
                    massPoint1.AddSpring(spring);
                    massPoint2.AddSpring(spring);
                }
                foreach (var m in _massPoints)
                {
                    v.Add(m._position);
                    //ワールド座標からローカル座標に変換する
                    _tmpVertices[m._index] = transform.InverseTransformPoint(m._position);
                }
                mesh.SetVertices(_tmpVertices);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                break;
            case ConnectDirection.UpperLeft:
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (i == 5)
                    {

                        _massPoints.Add(refBlock._massPoints[VertexName.RightLeg]);
                    }
                    else
                    {
                        var massPoint = gameObject.AddComponent<MassPoint>();
                        massPoint.SetMassSpring(30.0f, Vector3.zero, i, transform.TransformPoint(vertices[i]), this);
                        _massPoints.Add(massPoint);
                        if (i == 3 || i == 4)
                        {
                            massPoint._isFixed = true;
                        }
                    }
                }
                for (int i = 0; i < _initialSpringIndex.GetLength(0); i++)
                {
                    var spring = gameObject.AddComponent<Spring>();
                    var massPoint1 = _massPoints[_initialSpringIndex[i, 0]];
                    var massPoint2 = _massPoints[_initialSpringIndex[i, 1]];
                    //TODO: distanceは遅いのでmagintudeを使う
                    var initialLength = Vector3.Distance(massPoint1._position, massPoint2._position);
                    spring.SetSpring(massPoint1, massPoint2,
                    _springConstant, springLength: initialLength, 20.0f, 1.0f, springType: SpringType.Leg);
                    _springs.Add(spring);
                    massPoint1.AddSpring(spring);
                    massPoint2.AddSpring(spring);
                }
                for (int i = 0; i < _legSpring.GetLength(0); i++)
                {
                    var spring = gameObject.AddComponent<Spring>();
                    var massPoint1 = _massPoints[_legSpring[i, 0]];
                    var massPoint2 = _massPoints[_legSpring[i, 1]];
                    var initialLength = Vector3.Distance(massPoint1._position, massPoint2._position);
                    spring.SetSpring(massPoint1, massPoint2,
                    _springConstantLeg, springLength: initialLength, 20.0f, 1.0f, springType: SpringType.Leg);
                    _springs.Add(spring);
                    massPoint1.AddSpring(spring);
                    massPoint2.AddSpring(spring);
                }
                break;
            case ConnectDirection.LowerRight:
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (i == 5)
                    {

                        _massPoints.Add(refBlock._massPoints[VertexName.RightLeg]);
                    }
                    else
                    {
                        var massPoint = gameObject.AddComponent<MassPoint>();
                        massPoint.SetMassSpring(30.0f, Vector3.zero, i, transform.TransformPoint(vertices[i]), this);
                        _massPoints.Add(massPoint);
                        if (i == 3 || i == 4)
                        {
                            massPoint._isFixed = true;
                        }
                    }
                }
                for (int i = 0; i < _initialSpringIndex.GetLength(0); i++)
                {
                    var spring = gameObject.AddComponent<Spring>();
                    var massPoint1 = _massPoints[_initialSpringIndex[i, 0]];
                    var massPoint2 = _massPoints[_initialSpringIndex[i, 1]];
                    //TODO: distanceは遅いのでmagintudeを使う
                    var initialLength = Vector3.Distance(massPoint1._position, massPoint2._position);
                    spring.SetSpring(massPoint1, massPoint2,
                    _springConstant, springLength: initialLength, 20.0f, 1.0f, springType: SpringType.Leg);
                    _springs.Add(spring);
                    massPoint1.AddSpring(spring);
                    massPoint2.AddSpring(spring);
                }
                for (int i = 0; i < _legSpring.GetLength(0); i++)
                {
                    var spring = gameObject.AddComponent<Spring>();
                    var massPoint1 = _massPoints[_legSpring[i, 0]];
                    var massPoint2 = _massPoints[_legSpring[i, 1]];
                    var initialLength = Vector3.Distance(massPoint1._position, massPoint2._position);
                    spring.SetSpring(massPoint1, massPoint2,
                    _springConstantLeg, springLength: initialLength, 20.0f, 1.0f, springType: SpringType.Leg);
                    _springs.Add(spring);
                    massPoint1.AddSpring(spring);
                    massPoint2.AddSpring(spring);
                }
                break;
            case ConnectDirection.LowerLeft:
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (i == 2)
                    {
                        _massPoints.Add(refBlock._massPoints[VertexName.LeftLeg]);
                    }

                    else
                    {
                        var massPoint = gameObject.AddComponent<MassPoint>();
                        massPoint.SetMassSpring(30.0f, Vector3.zero, i, transform.TransformPoint(vertices[i]), this);
                        _massPoints.Add(massPoint);
                        if (i == 0 || i == 1)
                        {
                            massPoint._isFixed = true;
                        }
                    }

                }
                for (int i = 0; i < _initialSpringIndex.GetLength(0); i++)
                {
                    var spring = gameObject.AddComponent<Spring>();
                    var massPoint1 = _massPoints[_initialSpringIndex[i, 0]];
                    var massPoint2 = _massPoints[_initialSpringIndex[i, 1]];
                    //TODO: distanceは遅いのでmagintudeを使う
                    var initialLength = Vector3.Distance(massPoint1._position, massPoint2._position);
                    spring.SetSpring(massPoint1, massPoint2,
                    _springConstant, springLength: initialLength, 20.0f, 1.0f, springType: SpringType.Leg);
                    _springs.Add(spring);
                    massPoint1.AddSpring(spring);
                    massPoint2.AddSpring(spring);
                }
                for (int i = 0; i < _legSpring.GetLength(0); i++)
                {
                    var spring = gameObject.AddComponent<Spring>();
                    var massPoint1 = _massPoints[_legSpring[i, 0]];
                    var massPoint2 = _massPoints[_legSpring[i, 1]];
                    var initialLength = Vector3.Distance(massPoint1._position, massPoint2._position);
                    spring.SetSpring(massPoint1, massPoint2,
                    _springConstantLeg, springLength: initialLength, 20.0f, 1.0f, springType: SpringType.Leg);
                    _springs.Add(spring);
                    massPoint1.AddSpring(spring);
                    massPoint2.AddSpring(spring);
                }
                foreach (var m in _massPoints)
                {
                    v.Add(m._position);
                    //ワールド座標からローカル座標に変換する
                    _tmpVertices[m._index] = transform.InverseTransformPoint(m._position);
                }
                mesh.SetVertices(_tmpVertices);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                break;
            default:
                break;
        }

    }

    public void UpdatePreviousBlock(Block focused, ConnectDirection connectDirection)
    {
        if (connectDirection == ConnectDirection.UpperRight
        || connectDirection == ConnectDirection.LowerLeft)
        {
            this._rightLegInsertingBlock = focused;
        }
        else if (connectDirection == ConnectDirection.UpperLeft
        || connectDirection == ConnectDirection.LowerRight)
        {
            this._leftLegInsertingBlock = focused;
        }
        this._isFixed = false;
        int i = 0;
        foreach (var vertex in mesh.vertices)
        {
            _tmpVertices[i] = vertex;
            i++;
        }
        mesh.SetVertices(_tmpVertices);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        ReSpring(_tmpVertices, connectDirection, focused);

    }
    /// <summary>
    /// FocusedブロックにPreviousブロックを挿入する
    /// </summary>
    public void ConnectFocusedBlockWithPreviousBlock(Block previous, ConnectDirection connectDirection)
    {
        //コード共通化のため、操作対象のブロックの頂点インデックスを格納する変数を用意
        int thisBlockLegIndex = -1;
        int previousBlockLegIndex = -1;
        int thisBlockPocketIndex = -1;
        int previousBlockPocketIndex = -1;
        int thisBlockEyeIndex = -1;
        int previousBlockEyeIndex = -1;

        int thisAnotherBlockLegIndex = -1;
        int previousAnotherBlockLegIndex = -1;
        int thisAnothreBlockPocketIndex = -1;
        int previousAnotherBlockPocketIndex = -1;
        int thisAnotherBlockEyeIndex = -1;
        int previousAnotherBlockEyeIndex = -1;
        //左に接続する場合と右にクロス積の向きが逆になるため、補正用の変数を用意(右に接続する場合は1)
        float corssDirection = 1;
        //thisBlockの移動方向
        var moveVector = new Vector3(0, 0, 0);
        switch (connectDirection)
        {
            case ConnectDirection.UpperRight:
                //頂点情報の設定 
                thisBlockLegIndex = VertexName.LeftLeg;
                previousBlockLegIndex = VertexName.RightLeg;
                thisBlockPocketIndex = VertexName.LeftPocket;
                previousBlockPocketIndex = VertexName.RightPocket;
                thisBlockEyeIndex = VertexName.LeftEye;
                previousBlockEyeIndex = VertexName.RightEye;

                thisAnotherBlockLegIndex = VertexName.RightLeg;
                previousAnotherBlockLegIndex = VertexName.LeftLeg;
                thisAnothreBlockPocketIndex = VertexName.RightPocket;
                previousAnotherBlockPocketIndex = VertexName.LeftPocket;
                thisAnotherBlockEyeIndex = VertexName.RightEye;
                previousAnotherBlockEyeIndex = VertexName.LeftEye;

                //接続情報の設定・更新
                this._leftPocketInsertingBlock.Add(previous);
                previous._rightLegInsertingBlock = this;
                previous._rightLegInsertingBlockID = this.ID;

                //previousの右ポケットに脚を挿入しているブロックがある場合はrootの情報を移譲する
                if (previous._rootRightPocketBlock != null)
                {
                    this._rootLeftPocketBlock = previous._rootRightPocketBlock;
                    this._rootLeftPocketBlockID = previous._rootRightPocketBlock.ID;
                    this._rootLeftPocketBlockVertexName = previous._rootRightPocketBlockVertexName;
                }
                //挿入しているブロックがない場合、previousがrootである
                else
                {
                    this._rootLeftPocketBlock = previous;
                    this._rootLeftPocketBlockID = previous.ID;
                    //previousの右脚を挿入するので右脚の頂点を登録する
                    this._rootLeftPocketBlockVertexName = VertexName.RightLeg;
                }

                //移動方向の設定
                moveVector = new Vector3(-blockVallaySize, _margin, 0);
                break;
            case ConnectDirection.UpperLeft:
                //頂点情報の設定 
                thisBlockLegIndex = VertexName.RightLeg;
                previousBlockLegIndex = VertexName.LeftLeg;
                thisBlockPocketIndex = VertexName.RightPocket;
                previousBlockPocketIndex = VertexName.LeftPocket;
                thisBlockEyeIndex = VertexName.RightEye;
                previousBlockEyeIndex = VertexName.LeftEye;

                thisAnotherBlockLegIndex = VertexName.LeftLeg;
                previousAnotherBlockLegIndex = VertexName.RightLeg;
                thisAnothreBlockPocketIndex = VertexName.LeftPocket;
                previousAnotherBlockPocketIndex = VertexName.RightPocket;
                thisAnotherBlockEyeIndex = VertexName.LeftEye;
                previousAnotherBlockEyeIndex = VertexName.RightEye;

                //接続情報の設定・更新
                this._rightPocketInsertingBlock.Add(previous);
                previous._leftLegInsertingBlock = this;
                previous._leftLegInsertingBlockID = this.ID;

                //previousの左ポケットに脚を挿入しているブロックがある場合は,このブロックの右ポケットに、左ポケットのrootの情報を移譲する
                if (previous._rootLeftPocketBlock != null)
                {
                    this._rootRightPocketBlock = previous._rootLeftPocketBlock;
                    this._rootRightPocketBlockID = previous._rootLeftPocketBlock.ID;
                    this._rootRightPocketBlockVertexName = previous._rootLeftPocketBlockVertexName;
                }
                //挿入しているブロックがない場合、previousがrootである
                else
                {
                    this._rootRightPocketBlock = previous;
                    this._rootRightPocketBlockID = previous.ID;
                    //previousの左脚を挿入するので右脚の頂点を登録する
                    this._rootRightPocketBlockVertexName = VertexName.LeftLeg;
                }

                //移動方向の設定
                moveVector = new Vector3(blockVallaySize, _margin, 0);
                corssDirection = -1;
                break;
            case ConnectDirection.LowerRight:
                //頂点情報の設定 
                thisBlockLegIndex = VertexName.LeftLeg;
                previousBlockLegIndex = VertexName.RightLeg;
                thisBlockPocketIndex = VertexName.LeftPocket;
                previousBlockPocketIndex = VertexName.RightPocket;
                thisBlockEyeIndex = VertexName.LeftEye;
                previousBlockEyeIndex = VertexName.RightEye;

                thisAnotherBlockLegIndex = VertexName.RightLeg;
                previousAnotherBlockLegIndex = VertexName.LeftLeg;
                thisAnothreBlockPocketIndex = VertexName.RightPocket;
                previousAnotherBlockPocketIndex = VertexName.LeftPocket;
                thisAnotherBlockEyeIndex = VertexName.RightEye;
                previousAnotherBlockEyeIndex = VertexName.LeftEye;

                //接続情報の設定・更新
                this._leftLegInsertingBlock = previous;
                this._leftLegInsertingBlockID = previous.ID;
                previous._rightPocketInsertingBlock.Add(this);

                //previousの右ポケットに脚を挿入しているブロックがある場合はこのブロックがrootである
                if (previous._rootRightPocketBlock != null)
                {
                }
                //previousの右ポケットに脚を挿入しているブロックがある場合はこのブロックがrootである
                else
                {
                    previous._rootRightPocketBlock = this;
                    previous._rootRightPocketBlockID = this.ID;
                    previous._rootRightPocketBlockVertexName = VertexName.LeftLeg;
                }

                //移動方向の設定
                moveVector = new Vector3(-blockVallaySize, -_margin, 0);
                break;
            case ConnectDirection.LowerLeft:
                //頂点情報の設定 
                thisBlockLegIndex = VertexName.RightLeg;
                previousBlockLegIndex = VertexName.LeftLeg;
                thisBlockPocketIndex = VertexName.RightPocket;
                previousBlockPocketIndex = VertexName.LeftPocket;
                thisBlockEyeIndex = VertexName.RightEye;
                previousBlockEyeIndex = VertexName.LeftEye;

                thisAnotherBlockLegIndex = VertexName.LeftLeg;
                previousAnotherBlockLegIndex = VertexName.RightLeg;
                thisAnothreBlockPocketIndex = VertexName.LeftPocket;
                previousAnotherBlockPocketIndex = VertexName.RightPocket;
                thisAnotherBlockEyeIndex = VertexName.LeftEye;
                previousAnotherBlockEyeIndex = VertexName.RightEye;

                //接続情報の設定・更新
                this._leftLegInsertingBlock = previous;
                this._leftLegInsertingBlockID = previous.ID;
                previous._leftPocketInsertingBlock.Add(this);

                //previousの右ポケットに脚を挿入しているブロックがある場合はこのブロックがrootである
                if (previous._rootLeftPocketBlock != null)
                {
                }
                //previousの右ポケットに脚を挿入しているブロックがある場合はこのブロックがrootである
                else
                {
                    previous._rootLeftPocketBlock = this;
                    previous._rootLeftPocketBlockID = this.ID;
                    previous._rootRightPocketBlockVertexName = VertexName.RightLeg;
                }

                //移動方向の設定
                moveVector = new Vector3(blockVallaySize, -_margin, 0);
                corssDirection = -1;
                break;
        }
        if (connectDirection == ConnectDirection.UpperRight
        || connectDirection == ConnectDirection.UpperLeft)
        {
            this._isFixed = false;
            if (this._rightPocketInsertingBlock.Count != 0
            && this._leftPocketInsertingBlock.Count != 0)
            {
            }
            this.transform.position = previous.transform.position + moveVector;
            UpdateMassPointPosition();
            int i = 0;
            foreach (var vertex in mesh.vertices)
            {
                _tmpVertices[i] = vertex;
                i++;
            }
            //_tmpVertices[VertexName.RightLeg] = new Vector3(_tmpVertices[VertexName.RightPocket].x, _tmpVertices[VertexName.RightLeg].y, _tmpVertices[VertexName.RightLeg].z);
            var LegLength = Vector3.Distance(
                _massPoints[thisBlockPocketIndex]._position,
                _massPoints[thisBlockLegIndex]._position);
            var LegLengthLocal = Vector3.Distance(
                _tmpVertices[thisBlockPocketIndex],
                _tmpVertices[thisBlockLegIndex]);
            //連結面に直交するベクトルを外積で求める
            var cross = corssDirection * -Vector3.Cross(
                _massPoints[thisAnothreBlockPocketIndex]._position - _massPoints[thisBlockPocketIndex]._position,
                _massPoints[thisBlockEyeIndex]._position - _massPoints[thisBlockPocketIndex]._position);
            var crossLocal = corssDirection * -Vector3.Cross(
                _tmpVertices[thisAnothreBlockPocketIndex] - _tmpVertices[thisBlockPocketIndex],
                _tmpVertices[thisBlockEyeIndex] - _tmpVertices[thisBlockPocketIndex]);
            if (defaultScene.previousBlock._leftLegInsertedBlock != null)
            {
                cross = previous._massPoints[VertexName.RightLeg]._position - previous._massPoints[VertexName.RightPocket]._position;
                crossLocal = previous._tmpVertices[VertexName.RightLeg] - previous._tmpVertices[VertexName.RightPocket];
                //左足を含む面に平行に右脚の面を伸ばす
                // 脚を曲げる
                _tmpVertices[thisBlockPocketIndex] =
                (crossLocal).normalized * _margin + previous._tmpVertices[thisBlockPocketIndex];

                _tmpVertices[thisBlockLegIndex] =
                 (crossLocal).normalized * LegLengthLocal + _tmpVertices[thisBlockPocketIndex];

                _massPoints[thisBlockPocketIndex]._position =
                (cross).normalized * _margin + previous._massPoints[thisBlockPocketIndex]._position;
                _massPoints[thisBlockLegIndex]._position =
                (cross).normalized * LegLength + _massPoints[thisBlockPocketIndex]._position;
                //_massPoints[VertexName.LeftLeg]._position = new Vector3(_massPoints[VertexName.LeftPocket]._position.x, _massPoints[VertexName.LeftLeg]._position.y, _massPoints[VertexName.LeftLeg]._position.z);
            }

            else
            {
                _tmpVertices[thisBlockLegIndex] =
                crossLocal.normalized * LegLengthLocal + _tmpVertices[thisBlockPocketIndex];
                //_massPoints[VertexName.RightLeg]._position = new Vector3(_massPoints[VertexName.RightPocket]._position.x, _massPoints[VertexName.RightLeg]._position.y, _massPoints[VertexName.RightLeg]._position.z);
                _massPoints[thisBlockLegIndex]._position =
                cross.normalized * LegLength + _massPoints[thisBlockPocketIndex]._position;
            }

            //左ポケット部分は固定点とする
            // _massPoints[VertexName.LeftPocket]._isFixed = true;
            // _massPoints[VertexName.LeftEye]._isFixed = true;
            mesh.SetVertices(_tmpVertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            previous.UpdatePreviousBlock(this, connectDirection);
            //TODO: 天井からつるすバネを張る
            _massPoints[thisBlockPocketIndex]._position =
            (previous._massPoints[previousBlockLegIndex]._position - previous._massPoints[previousBlockPocketIndex]._position).normalized * _margin
            + previous._massPoints[previousBlockLegIndex]._position;

            _massPoints[thisBlockEyeIndex]._position =
            (previous._massPoints[previousBlockLegIndex]._position - previous._massPoints[previousBlockEyeIndex]._position).normalized * _margin
            + previous._massPoints[previousBlockEyeIndex]._position;
            i = 0;
            int spring1 = 0;
            int spring2 = 0;
            foreach (var spring in _springs)
            {
                if (spring._massPointIndexes.Contains(thisBlockLegIndex)
                && spring._massPointIndexes.Contains(thisBlockPocketIndex))
                {
                    spring1 = i;
                }
                if (spring._massPointIndexes.Contains(thisBlockEyeIndex)
                && spring._massPointIndexes.Contains(thisBlockLegIndex))
                {
                    spring2 = i;
                }
                i++;
            }
            _springs.RemoveAt(spring1);
            _springs.RemoveAt(spring2);
        }
        else
        {
            this._isFixed = false;
            if (this._rightPocketInsertingBlock.Count != 0
            && this._leftPocketInsertingBlock.Count != 0)
            {
            }
            this.transform.position = previous.transform.position + moveVector;
            UpdateMassPointPosition();
            int i = 0;
            foreach (var vertex in previous.mesh.vertices)
            {
                previous._tmpVertices[i] = vertex;
                i++;
            }
            //_tmpVertices[VertexName.RightLeg] = new Vector3(_tmpVertices[VertexName.RightPocket].x, _tmpVertices[VertexName.RightLeg].y, _tmpVertices[VertexName.RightLeg].z);
            //previous側が被挿入ブロックになるので、Upperとは処理を逆にする
            var LegLength = Vector3.Distance(
                previous._massPoints[previousBlockPocketIndex]._position,
                previous._massPoints[previousBlockLegIndex]._position);
            var LegLengthLocal = Vector3.Distance(
                previous._tmpVertices[previousBlockPocketIndex],
                previous._tmpVertices[previousBlockLegIndex]);
            //連結面に直交するベクトルを外積で求める
            var cross = corssDirection * -Vector3.Cross(
                previous._massPoints[previousAnotherBlockPocketIndex]._position - previous._massPoints[previousBlockPocketIndex]._position,
                previous._massPoints[previousBlockEyeIndex]._position - previous._massPoints[previousBlockPocketIndex]._position);
            var crossLocal = corssDirection * -Vector3.Cross(
                previous._tmpVertices[previousAnotherBlockPocketIndex] - previous._tmpVertices[previousBlockPocketIndex],
                previous._tmpVertices[previousBlockEyeIndex] - previous._tmpVertices[previousBlockPocketIndex]);

            //被挿入ブロック(previous)の脚の座標を更新する
            previous._tmpVertices[previousBlockLegIndex] =
            crossLocal.normalized * LegLengthLocal + previous._tmpVertices[previousBlockPocketIndex];
            //_massPoints[VertexName.RightLeg]._position = new Vector3(_massPoints[VertexName.RightPocket]._position.x, _massPoints[VertexName.RightLeg]._position.y, _massPoints[VertexName.RightLeg]._position.z);
            previous._massPoints[previousBlockLegIndex]._position =
            cross.normalized * LegLength + previous._massPoints[previousBlockPocketIndex]._position;

            previous.mesh.SetVertices(previous._tmpVertices);
            previous.mesh.RecalculateBounds();
            previous.mesh.RecalculateNormals();
            previous.mesh.RecalculateTangents();
            //挿入ブロック(focused)の更新を行う
            UpdatePreviousBlock(focused: previous, connectDirection);
            previous._massPoints[previousBlockPocketIndex]._position =
            (_massPoints[thisBlockLegIndex]._position - _massPoints[thisBlockPocketIndex]._position).normalized * _margin
            + _massPoints[thisBlockPocketIndex]._position;

            previous._massPoints[previousBlockEyeIndex]._position =
            (_massPoints[thisBlockLegIndex]._position - _massPoints[thisBlockPocketIndex]._position).normalized * _margin
            + _massPoints[thisBlockEyeIndex]._position;

            i = 0;
            int spring1 = 0;
            int spring2 = 0;
            foreach (var spring in previous._springs)
            {
                if (spring._massPointIndexes.Contains(previousBlockPocketIndex)
                && spring._massPointIndexes.Contains(previousBlockLegIndex))
                {
                    spring1 = i;
                }
                if (spring._massPointIndexes.Contains(previousBlockEyeIndex)
                && spring._massPointIndexes.Contains(previousBlockLegIndex))
                {
                    spring2 = i;
                }
                i++;
            }
            previous._springs.RemoveAt(spring1);
            previous._springs.RemoveAt(spring2);
        }
    }
}
