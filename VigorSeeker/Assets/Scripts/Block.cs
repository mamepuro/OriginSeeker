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

public enum ConnectType
{
    Left_SelectToConnected, //セレクトしてるものをコネクトに接続
    Right_SelectToConnected,
    Left_ConnectedToSelect,//コネクトからセレクトしているものに接続
    Right_ConnectedToSelect,
    Top, //未使用
    Bottom //未使用

}

/// <summary>
/// 頂点名(背側を正面とする)
/// </summary>

public class VertexName
{
    public const int LeftPocket = 0; //0
    public const int LeftEye = 1; // 1
    public const int LeftLeg = 2; // 2
    public const int RightPocket = 3; // 3
    public const int RightEye = 4; // 4
    public const int RightLeg = 5; // 5

}


public enum which
{
    Left,
    Right,
    Top, //縦に連結

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
    { VertexName.LeftPocket, VertexName.LeftEye },
    { VertexName.LeftEye, VertexName.LeftLeg },
    { VertexName.LeftLeg, VertexName.LeftPocket },
    { VertexName.LeftEye, VertexName.RightEye },
    { VertexName.LeftPocket, VertexName.RightPocket },
    { VertexName.RightPocket, VertexName.RightEye },
    { VertexName.RightEye, VertexName.RightLeg },
    { VertexName.RightLeg, VertexName.RightPocket },
    { VertexName.LeftPocket, VertexName.RightEye },
    { VertexName.LeftEye, VertexName.RightPocket }
    };
    //{ 2, 0 }, { 1, 4 }, { 0, 3 }, { 3, 4 }, { 4, 5 }, { 5, 3 }, { 0, 4 }, { 1, 3 } };
    /// <summary>
    /// ブロックの足間に貼るバネの初期インデックス
    /// </summary>
    readonly int[,] _legSpring = { { VertexName.LeftLeg, VertexName.RightLeg } };
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
    [SerializeField] public float _margin = 0.08533333333f;

    const float _mass = 30.0f;
    const float _dampingConstant = 20.0f;
    const float _springConstant = 10.0f;
    [SerializeField] public float _springConstantLeg = 1.0f;

    bool _isDebug = false;//
    /// <summary>
    /// プリミティブを構成するブロックかどうか
    /// </summary>
    public bool _isJoiningPrimitive = false;

    //収束までにかかったステップ数
    [SerializeField]
    public int _step = 0;


    public static float blockVallaySize = 4.454382f - 4.378539f;
    public static float margin = 4.378539f - 3.99421f;

    public void OnEnable()
    {
        // mesh = GetComponent<MeshFilter>().sharedMesh;
        // v = new List<Vector3>();
        // foreach (Vector3 v3 in mesh.vertices)
        // {
        //     v.Add(transform.TransformPoint(v3));
        // }
        // Debug.Log("Info: Block awaked. ID is " + ID);
        // // Debug.Log("Now" + DateTime.Now);
        // var triangle = mesh.triangles;
        // Debug.Log("triangle.Length is " + triangle.Length);
        // foreach (var x in triangle)
        // {
        //     Debug.Log("triangle is " + x);
        // }
        _leftPocketInsertingBlock = new List<Block>();
        _rightPocketInsertingBlock = new List<Block>();
        //頂点数は6固定
        _tmpVertices = new Vector3[6];
    }
    void OnDrawGizmos()
    {
        if (defaultScene != null)
        {
            if (defaultScene.isVisible)
            {

                // // //Debug.Log("spring count is " + _springs.Count);
                // Gizmos.color = Color.red;
                // //var LegLength = Vector3.Distance(_massPoints[0]._position, _massPoints[2]._position);
                // var LegLengthLocal = Vector3.Distance(_tmpVertices[0], _tmpVertices[2]);
                // //連結面に直交するベクトルを外積で求める
                // //var cross = Vector3.Cross(_massPoints[3]._position - _massPoints[0]._position, _massPoints[1]._position - _massPoints[0]._position);
                // var crossLocal = Vector3.Cross(_tmpVertices[3] - _tmpVertices[0], _tmpVertices[1] - _tmpVertices[0]);
                // // 脚を曲げる
                // _tmpVertices[2] = crossLocal.normalized * LegLengthLocal + _tmpVertices[0];
                // Gizmos.DrawLine(transform.TransformPoint(_tmpVertices[0]), transform.TransformPoint(_tmpVertices[2]));
                if (this._leftLegInsertedBlock == null && this._rightLegInsertedBlock == null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(transform.TransformPoint(_tmpVertices[2]), this._leftLegInsertedBlock.transform.TransformPoint(_tmpVertices[2] + new Vector3(0, 0.2f, 0.0f)));
                    Gizmos.DrawLine(transform.TransformPoint(_tmpVertices[5]), this._rightLegInsertedBlock.transform.TransformPoint(_tmpVertices[5] + new Vector3(0, 0.2f, 0.0f)));
                }
                if (this._leftPocketInsertingBlock.Count == 0
                && this._rightPocketInsertingBlock.Count == 0)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(transform.TransformPoint(_tmpVertices[0]), this._leftPocketInsertingBlock[0].transform.TransformPoint(_tmpVertices[0] + new Vector3(0, -0.2f, 0.0f)));
                    Gizmos.DrawLine(transform.TransformPoint(_tmpVertices[3]), this._leftPocketInsertingBlock[0].transform.TransformPoint(_tmpVertices[3] + new Vector3(0, -0.2f, 0.0f)));
                }

                foreach (var spring in _springs)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(spring._leftMassPoint._position, spring._rightMassPoint._position);
                }
            }
        }
    }

    /// <summary>
    /// Editモードで毎フレームレンダリングするための処理
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
        //Debug.Log("TransformInsertionModel is called");
        //Debug.Log("On Space key is pressed");
        int i = 0;
        foreach (var vertex in mesh.vertices)
        {
            _tmpVertices[i] = vertex;
            i++;
        }
        //testbending
        _tmpVertices[2] = new Vector3(_tmpVertices[0].x, _tmpVertices[2].y, _tmpVertices[2].z);
        _tmpVertices[5] = new Vector3(_tmpVertices[3].x, _tmpVertices[5].y, _tmpVertices[5].z);
        mesh.SetVertices(_tmpVertices);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        if (_massPoints.Count != 0)
        {
            _massPoints[2]._position = new Vector3(_massPoints[0]._position.x, _massPoints[2]._position.y, _massPoints[2]._position.z);
            _massPoints[5]._position = new Vector3(_massPoints[3]._position.x, _massPoints[5]._position.y, _massPoints[5]._position.z);
            foreach (var spring in _springs)
            {
                //バネの張り直し
                if (spring._massPointIndexes.Contains(2) && spring._massPointIndexes.Contains(5))
                {
                    spring._springLength = Vector3.Distance(_massPoints[2]._position, _massPoints[5]._position);
                }
            }
        }
    }
    public void OnSpaceKeyPress()
    {
        initTime = Time.time;
        _isDebug = !_isDebug;
        //Debug.Log("On Space key is pressed");
        int i = 0;
        foreach (var vertex in mesh.vertices)
        {
            _tmpVertices[i] = vertex;
            i++;
        }
        //testbending
        _tmpVertices[2] = new Vector3(_tmpVertices[0].x, _tmpVertices[2].y, _tmpVertices[2].z);
        _massPoints[2]._position = new Vector3(_massPoints[0]._position.x, _massPoints[2]._position.y, _massPoints[2]._position.z);
        _tmpVertices[5] = new Vector3(_tmpVertices[3].x, _tmpVertices[5].y, _tmpVertices[5].z);
        _massPoints[5]._position = new Vector3(_massPoints[3]._position.x, _massPoints[5]._position.y, _massPoints[5]._position.z);
        mesh.SetVertices(_tmpVertices);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        //Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        //UpdateVertices();
    }
    public void OnLeftKeyPress()
    {
        //TODO: 書き換える
        if (defaultScene.selectedBlock == this)
        {

            //Debug.Log("selectedBlock is this " + this.ID);
            //defaultScene.connectedBlock._leftLegInsertedBlock = this;
            this._rightPocketInsertingBlock.Add(defaultScene.connectedBlock);
            this._isFixed = false;
            if (this._rightPocketInsertingBlock.Count != 0
            && this._leftPocketInsertingBlock.Count != 0)
            {
                //閉じた状態で安定させる
                //TODO: ここは要検討
                //this._isFixed = true;
            }
            this.transform.position = this.transform.position + new Vector3(blockVallaySize, _margin, 0);
            UpdateMassPointPosition();
            int i = 0;
            foreach (var vertex in mesh.vertices)
            {
                _tmpVertices[i] = vertex;
                i++;
            }
            _tmpVertices[2] = new Vector3(_tmpVertices[0].x, _tmpVertices[2].y, _tmpVertices[2].z);
            //_tmpVertices[5] = new Vector3(_tmpVertices[3].x, _tmpVertices[5].y, _tmpVertices[5].z);
            _massPoints[2]._position = new Vector3(_massPoints[0]._position.x, _massPoints[2]._position.y, _massPoints[2]._position.z);
            //_massPoints[5]._position = new Vector3(_massPoints[3]._position.x, _massPoints[5]._position.y, _massPoints[5]._position.z);
            mesh.SetVertices(_tmpVertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            //TODO: 天井からつるすバネを張る
        }
        if (defaultScene.connectedBlock == this)
        {
            //Debug.Log("connectedBlock is this " + this.ID);
            this._leftLegInsertedBlock = defaultScene.selectedBlock;
            this._isFixed = false;
            if (this._rightPocketInsertingBlock.Count != 0
            && this._leftPocketInsertingBlock.Count != 0)
            {
                //閉じた状態で安定させる
                //TODO: ここは要検討
                //this._isFixed = true;
            }
            //this.transform.position = this.transform.position + new Vector3(-blockVallaySize, _margin, 0);
            UpdateMassPointPosition();
            int i = 0;
            foreach (var vertex in mesh.vertices)
            {
                _tmpVertices[i] = vertex;
                i++;
            }
            //_tmpVertices[2] = new Vector3(_tmpVertices[0].x, _tmpVertices[2].y, _tmpVertices[2].z);
            _tmpVertices[5] = new Vector3(_tmpVertices[3].x, _tmpVertices[5].y, _tmpVertices[5].z);
            //_massPoints[2]._position = new Vector3(_massPoints[0]._position.x, _massPoints[2]._position.y, _massPoints[2]._position.z);
            _massPoints[5]._position = new Vector3(_massPoints[3]._position.x, _massPoints[5]._position.y, _massPoints[5]._position.z);
            mesh.SetVertices(_tmpVertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
        }
    }
    //右上に接続
    public void OnRightKeyPress()
    {
        if (defaultScene.selectedBlock == this)
        {

            //Debug.Log("selectedBlock is this " + this.ID);
            //defaultScene.connectedBlock._leftLegInsertedBlock = this;
            this._leftPocketInsertingBlock.Add(defaultScene.connectedBlock);
            this._isFixed = false;
            if (this._rightPocketInsertingBlock.Count != 0
            && this._leftPocketInsertingBlock.Count != 0)
            {
                //閉じた状態で安定させる
                //TODO: ここは要検討
                //this._isFixed = true;
            }
            this.transform.position = defaultScene.connectedBlock.transform.position + new Vector3(-blockVallaySize, _margin, 0);
            UpdateMassPointPosition();
            int i = 0;
            foreach (var vertex in mesh.vertices)
            {
                _tmpVertices[i] = vertex;
                i++;
            }
            //_tmpVertices[2] = new Vector3(_tmpVertices[0].x, _tmpVertices[2].y, _tmpVertices[2].z);
            var LegLength = Vector3.Distance(_massPoints[3]._position, _massPoints[5]._position);
            var LegLengthLocal = Vector3.Distance(_tmpVertices[3], _tmpVertices[5]);
            //連結面に直交するベクトルを外積で求める
            var cross = -Vector3.Cross(_massPoints[0]._position - _massPoints[3]._position, _massPoints[4]._position - _massPoints[3]._position);
            var crossLocal = -Vector3.Cross(_tmpVertices[0] - _tmpVertices[3], _tmpVertices[4] - _tmpVertices[3]);
            if (defaultScene.connectedBlock._leftLegInsertedBlock != null)
            {
                cross = defaultScene.connectedBlock._massPoints[2]._position - defaultScene.connectedBlock._massPoints[0]._position;
                crossLocal = defaultScene.connectedBlock._tmpVertices[2] - defaultScene.connectedBlock._tmpVertices[0];
                //左足を含む面に平行に右脚の面を伸ばす
                // 脚を曲げる
                _tmpVertices[3] = (crossLocal).normalized * _margin + defaultScene.connectedBlock._tmpVertices[3];
                _tmpVertices[5] = (crossLocal).normalized * LegLengthLocal + _tmpVertices[3];
                _massPoints[3]._position = (cross).normalized * _margin + defaultScene.connectedBlock._massPoints[3]._position;
                _massPoints[5]._position = (cross).normalized * LegLength + _massPoints[3]._position;
                //_massPoints[5]._position = new Vector3(_massPoints[3]._position.x, _massPoints[5]._position.y, _massPoints[5]._position.z);
            }
            else
            {
                _tmpVertices[5] = crossLocal.normalized * LegLengthLocal + _tmpVertices[3];
                //_massPoints[2]._position = new Vector3(_massPoints[0]._position.x, _massPoints[2]._position.y, _massPoints[2]._position.z);
                _massPoints[5]._position = cross.normalized * LegLength + _massPoints[3]._position;
            }

            //左ポケット部分は固定点とする
            // _massPoints[3]._isFixed = true;
            // _massPoints[4]._isFixed = true;
            mesh.SetVertices(_tmpVertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            defaultScene.connectedBlock.OnRightKeyPress();
            //TODO: 天井からつるすバネを張る
            _massPoints[3]._position = (_leftPocketInsertingBlock[0]._massPoints[2]._position - _leftPocketInsertingBlock[0]._massPoints[0]._position).normalized * _margin + _leftPocketInsertingBlock[0]._massPoints[0]._position;
            _massPoints[4]._position = (_leftPocketInsertingBlock[0]._massPoints[2]._position - _leftPocketInsertingBlock[0]._massPoints[0]._position).normalized * _margin + _leftPocketInsertingBlock[0]._massPoints[1]._position;
            //TODO: バネを治す暫定的対応をちゃんと直す
            //Debug.Log("springs " + _springs.Count);
            i = 0;
            int spring1 = 0;
            int spring2 = 0;
            foreach (var spring in _springs)
            {
                if (spring._massPointIndexes[0] == 5 && spring._massPointIndexes[1] == 3)
                {
                    //Debug.Log("Unko!! 5 3");
                    spring1 = i;
                }
                if (spring._massPointIndexes[0] == 4 && spring._massPointIndexes[1] == 5)
                {
                    //Debug.Log("Unko!! 4 5");
                    spring2 = i;
                }
                i++;
            }

            _springs.RemoveAt(spring1);
            _springs.RemoveAt(spring2);
            //Debug.Log("springs " + _springs.Count);
            foreach (var spring in _springs)
            {
                // Debug.Log("unko is " + spring._massPointIndexes.Count + "[0]" + spring._massPointIndexes[0] + " 1 " + spring._massPointIndexes[1]);
            }
        }
        if (defaultScene.connectedBlock == this)
        {
            //Debug.Log("connectedBlock is this " + this.ID);
            this._leftLegInsertedBlock = defaultScene.selectedBlock;
            this._isFixed = false;
            if (this._rightPocketInsertingBlock.Count != 0
            && this._leftPocketInsertingBlock.Count != 0)
            {
                //閉じた状態で安定させる
                //TODO: ここは要検討
                //this._isFixed = true;
            }
            //this.transform.position = this.transform.position + new Vector3(-blockVallaySize, _margin, 0);
            int i = 0;
            foreach (var vertex in mesh.vertices)
            {

                _tmpVertices[i] = vertex;
                i++;
            }
            //以下の2行はバネを張り直す関係上いらない
            //_tmpVertices[2] = new Vector3(_tmpVertices[0].x, _tmpVertices[2].y, _tmpVertices[2].z);
            //_massPoints[2]._position = new Vector3(_massPoints[0]._position.x, _massPoints[2]._position.y, _massPoints[2]._position.z);
            mesh.SetVertices(_tmpVertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            ReSpring(_tmpVertices, which.Right, defaultScene.selectedBlock);
        }
    }

    //右下に接続
    public void OnAKeyPress()
    {
        //Debug.Log("On A key is pressed");
        if (defaultScene.selectedBlock == this)
        {
            //Debug.Log("connectedBlock is this " + this.ID);
            this._leftLegInsertedBlock = defaultScene.selectedBlock;
            this._isFixed = false;
            if (this._rightPocketInsertingBlock.Count != 0
            && this._leftPocketInsertingBlock.Count != 0)
            {
                //閉じた状態で安定させる
                //TODO: ここは要検討
                //this._isFixed = true;

            }
            this.transform.position = defaultScene.connectedBlock.transform.position
            + new Vector3(-(defaultScene.connectedBlock._massPoints[3]._position.x - defaultScene.connectedBlock._massPoints[0]._position.x) / 2, -_margin, 0);
            UpdateMassPointPosition();
            int i = 0;
            foreach (var vertex in mesh.vertices)
            {
                // if (i == 2)
                // {
                //     Debug.Log("test vertex is " + vertex + " d" + defaultScene.selectedBlock.mesh.vertices[5]);
                //     _tmpVertices[i] = defaultScene.selectedBlock.mesh.vertices[5];
                // }
                _tmpVertices[i] = vertex;
                i++;
            }
            //以下の2行はバネを張り直す関係上いらない
            //_tmpVertices[2] = new Vector3(_tmpVertices[0].x, _tmpVertices[2].y, _tmpVertices[2].z);
            //_massPoints[2]._position = new Vector3(_massPoints[0]._position.x, _massPoints[2]._position.y, _massPoints[2]._position.z);
            mesh.SetVertices(_tmpVertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            ReSpring(_tmpVertices, which.Left, defaultScene.connectedBlock);
        }

        if (defaultScene.connectedBlock == this)
        {
            //Debug.Log("selectedBlock is this " + this.ID);
            //defaultScene.connectedBlock._leftLegInsertedBlock = this;
            this._rightPocketInsertingBlock.Add(defaultScene.selectedBlock);
            this._isFixed = false;
            if (this._rightPocketInsertingBlock.Count != 0
            && this._leftPocketInsertingBlock.Count != 0)
            {
                //閉じた状態で安定させる
                //TODO: ここは要検討
                //this._isFixed = true;
            }

            //this.transform.position = defaultScene.connectedBlock.transform.position + new Vector3(-blockVallaySize, _margin, 0);
            UpdateMassPointPosition();
            int i = 0;
            foreach (var vertex in mesh.vertices)
            {
                _tmpVertices[i] = vertex;
                i++;
            }
            //左足を含む面に平行に右脚の面を伸ばす
            var LegLength = Vector3.Distance(_massPoints[0]._position, _massPoints[2]._position);
            var LegLengthLocal = Vector3.Distance(_tmpVertices[0], _tmpVertices[2]);
            //左足を含む面に平行に右脚の面を伸ばす
            // 脚を曲げる
            _tmpVertices[2] = (_tmpVertices[5] - _tmpVertices[3]).normalized * LegLengthLocal + _tmpVertices[0];
            //_tmpVertices[5] = new Vector3(_tmpVertices[3].x, _tmpVertices[5].y, _tmpVertices[5].z);
            _massPoints[2]._position = (_massPoints[5]._position - _massPoints[3]._position).normalized * LegLength + _massPoints[0]._position;
            //_massPoints[5]._position = new Vector3(_massPoints[3]._position.x, _massPoints[5]._position.y, _massPoints[5]._position.z);
            //右ポケット部分は固定点とする
            // _massPoints[0]._isFixed = true;
            // _massPoints[1]._isFixed = true;

            mesh.SetVertices(_tmpVertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            defaultScene.selectedBlock.OnAKeyPress();
            //TODO: 天井からつるすバネを張る
            _massPoints[0]._position = (_rightPocketInsertingBlock[0]._massPoints[5]._position - _rightPocketInsertingBlock[0]._massPoints[3]._position).normalized * _margin + _rightPocketInsertingBlock[0]._massPoints[3]._position;
            _massPoints[1]._position = (_rightPocketInsertingBlock[0]._massPoints[5]._position - _rightPocketInsertingBlock[0]._massPoints[3]._position).normalized * _margin + _rightPocketInsertingBlock[0]._massPoints[4]._position;
            //TODO: バネを治す暫定的対応をちゃんと直す
            i = 0;
            int spring1 = 0;
            int spring2 = 0;
            foreach (var spring in _springs)
            {
                if (spring._massPointIndexes[0] == 2 && spring._massPointIndexes[1] == 0)
                {
                    //Debug.Log("2 0");
                    spring1 = i;
                }
                if (spring._massPointIndexes[0] == 0 && spring._massPointIndexes[1] == 1)
                {
                    //Debug.Log("0 1");
                    spring2 = i;
                }
                i++;
            }
            _springs.RemoveAt(spring1);
            _springs.RemoveAt(spring2);
        }
    }

    /// <summary>
    /// 上に連結する場合の処理
    /// </summary>
    public void OnUpKeyPress()
    {
        if (defaultScene.selectedBlock == this)
        {

            //Debug.Log("selectedBlock is this " + this.ID);
            //defaultScene.connectedBlock._leftLegInsertedBlock = this;
            this._leftPocketInsertingBlock.Add(defaultScene.connectedBlock);
            this._isFixed = false;
            if (this._rightPocketInsertingBlock.Count != 0
            && this._leftPocketInsertingBlock.Count != 0)
            {
                //閉じた状態で安定させる
                //TODO: ここは要検討
                //this._isFixed = true;
            }
            this.transform.position = defaultScene.connectedBlock.transform.position + new Vector3(0, _margin, 0);
            UpdateMassPointPosition();
            int i = 0;
            foreach (var vertex in mesh.vertices)
            {
                _tmpVertices[i] = vertex;
                i++;
            }
            //_tmpVertices[2] = new Vector3(_tmpVertices[0].x, _tmpVertices[2].y, _tmpVertices[2].z);
            var LegLength = Vector3.Distance(_massPoints[3]._position, _massPoints[5]._position);
            var LegLengthLocal = Vector3.Distance(_tmpVertices[3], _tmpVertices[5]); //ローカル座標系での脚の長さ
            //連結面に直交するベクトルを外積で求める
            var cross = -Vector3.Cross(_massPoints[0]._position - _massPoints[3]._position, _massPoints[4]._position - _massPoints[3]._position);
            var crossLocal = -Vector3.Cross(_tmpVertices[0] - _tmpVertices[3], _tmpVertices[4] - _tmpVertices[3]);
            if (defaultScene.connectedBlock._leftLegInsertedBlock != null)
            {
                cross = defaultScene.connectedBlock._massPoints[2]._position - defaultScene.connectedBlock._massPoints[0]._position;
                crossLocal = defaultScene.connectedBlock._tmpVertices[2] - defaultScene.connectedBlock._tmpVertices[0];
                //左足を含む面に平行に右脚の面を伸ばす
                // 脚を曲げる
                _tmpVertices[3] = (crossLocal).normalized * _margin + defaultScene.connectedBlock._tmpVertices[3];
                _tmpVertices[5] = (crossLocal).normalized * LegLengthLocal + _tmpVertices[3];
                _massPoints[3]._position = (cross).normalized * _margin + defaultScene.connectedBlock._massPoints[3]._position;
                _massPoints[5]._position = (cross).normalized * LegLength + _massPoints[3]._position;
                //_massPoints[5]._position = new Vector3(_massPoints[3]._position.x, _massPoints[5]._position.y, _massPoints[5]._position.z);
            }
            else
            {
                _tmpVertices[5] = crossLocal.normalized * LegLengthLocal + _tmpVertices[3];
                //_massPoints[2]._position = new Vector3(_massPoints[0]._position.x, _massPoints[2]._position.y, _massPoints[2]._position.z);
                _massPoints[5]._position = cross.normalized * LegLength + _massPoints[3]._position;
            }

            //左ポケット部分は固定点とする
            // _massPoints[3]._isFixed = true;
            // _massPoints[4]._isFixed = true;
            mesh.SetVertices(_tmpVertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            defaultScene.connectedBlock.OnRightKeyPress();
            //TODO: 天井からつるすバネを張る
            _massPoints[3]._position = (_leftPocketInsertingBlock[0]._massPoints[2]._position - _leftPocketInsertingBlock[0]._massPoints[0]._position).normalized * _margin + _leftPocketInsertingBlock[0]._massPoints[0]._position;
            _massPoints[4]._position = (_leftPocketInsertingBlock[0]._massPoints[2]._position - _leftPocketInsertingBlock[0]._massPoints[0]._position).normalized * _margin + _leftPocketInsertingBlock[0]._massPoints[1]._position;
            //TODO: バネを治す暫定的対応をちゃんと直す
            //Debug.Log("springs " + _springs.Count);
            i = 0;
            int spring1 = 0;
            int spring2 = 0;
            foreach (var spring in _springs)
            {
                if (spring._massPointIndexes[0] == 5 && spring._massPointIndexes[1] == 3)
                {
                    //Debug.Log("Unko!! 5 3");
                    spring1 = i;
                }
                if (spring._massPointIndexes[0] == 4 && spring._massPointIndexes[1] == 5)
                {
                    //Debug.Log("Unko!! 4 5");
                    spring2 = i;
                }
                i++;
            }

            _springs.RemoveAt(spring1);
            _springs.RemoveAt(spring2);
            //Debug.Log("springs " + _springs.Count);
            foreach (var spring in _springs)
            {
                // Debug.Log("unko is " + spring._massPointIndexes.Count + "[0]" + spring._massPointIndexes[0] + " 1 " + spring._massPointIndexes[1]);
            }
        }
        if (defaultScene.connectedBlock == this)
        {
            //Debug.Log("connectedBlock is this " + this.ID);
            this._leftLegInsertedBlock = defaultScene.selectedBlock;
            this._isFixed = false;
            if (this._rightPocketInsertingBlock.Count != 0
            && this._leftPocketInsertingBlock.Count != 0)
            {
                //閉じた状態で安定させる
                //TODO: ここは要検討
                //this._isFixed = true;
            }
            //this.transform.position = this.transform.position + new Vector3(-blockVallaySize, _margin, 0);
            int i = 0;
            foreach (var vertex in mesh.vertices)
            {

                _tmpVertices[i] = vertex;
                i++;
            }
            //以下の2行はバネを張り直す関係上いらない
            //_tmpVertices[2] = new Vector3(_tmpVertices[0].x, _tmpVertices[2].y, _tmpVertices[2].z);
            //_massPoints[2]._position = new Vector3(_massPoints[0]._position.x, _massPoints[2]._position.y, _massPoints[2]._position.z);
            mesh.SetVertices(_tmpVertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            ReSpring(_tmpVertices, which.Right, defaultScene.selectedBlock);
        }
    }
    void UpdateVertices()
    {
        if (_massPoints.Count == 0)
        {
            //質点数が0の場合はメッシュの頂点座標を取得する
            v.Clear();
            foreach (Vector3 v3 in mesh.vertices)
            {
                v.Add(transform.TransformPoint(v3));
            }
        }
        else if (_isAnimatable && !_isJoiningPrimitive)
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
                        //ポケットから脚に向かうベクトル　* _margin分をポケットの頂点座標に固定する
                        var pos = (_leftPocketInsertingBlock[0]._massPoints[2]._position - _leftPocketInsertingBlock[0]._massPoints[0]._position).normalized * _margin + _leftPocketInsertingBlock[0]._massPoints[i - 3]._position;
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
                        var pos = (_rightPocketInsertingBlock[0]._massPoints[5]._position - _rightPocketInsertingBlock[0]._massPoints[3]._position).normalized * _margin
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
                        var pos = (_rightPocketInsertingBlock[0]._massPoints[5]._position - _rightPocketInsertingBlock[0]._massPoints[3]._position).normalized * _margin
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
                        var pos = (_leftPocketInsertingBlock[0]._massPoints[2]._position - _leftPocketInsertingBlock[0]._massPoints[0]._position).normalized * _margin + _leftPocketInsertingBlock[0]._massPoints[i - 3]._position;
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

                    if (true)//isDebug
                    {
                        // Debug.Log("move is " + m.move);
                        if (m.move >= 0.00005)
                        {
                            //Debug.Log("move is " + m.move + "step is " + step);

                            isFished = false;
                        }
                        // if (Time.time - initTime > 0.1)
                        // {
                        //     if (step == 0)
                        //     {
                        //         Debug.Break();
                        //     }
                        // }
                    }  //

                    //ワールド座標からローカル座標に変換する
                    _tmpVertices[i] = transform.InverseTransformPoint(m._position);
                    i++;
                    step = m.step;
                }
                if (true)//isDebug
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
            massPoint.SetMassSpring(_mass, Vector3.zero, i, v[i], this);
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
            _springConstant, springLength: initialLength, _dampingConstant, 1.0f, springType: SpringType.Leg);
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
            _springConstantLeg, springLength: initialLength, _dampingConstant, 1.0f, springType: SpringType.Leg);
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
        _tmpVertices[0] = new Vector3(_tmpVertices[0].x - extra, _tmpVertices[0].y, _tmpVertices[0].z);
        _massPoints[0]._position = new Vector3(_massPoints[0]._position.x - extra, _massPoints[0]._position.y, _massPoints[0]._position.z);
        _tmpVertices[1] = new Vector3(_tmpVertices[1].x - extra, _tmpVertices[1].y, _tmpVertices[1].z);
        _massPoints[1]._position = new Vector3(_massPoints[1]._position.x - extra, _massPoints[1]._position.y, _massPoints[1]._position.z);
        _tmpVertices[2] = new Vector3(_tmpVertices[2].x - extra, _tmpVertices[2].y, _tmpVertices[2].z);
        _massPoints[2]._position = new Vector3(_massPoints[2]._position.x - extra, _massPoints[2]._position.y, _massPoints[2]._position.z);
        _tmpVertices[3] = new Vector3(_tmpVertices[3].x + extra, _tmpVertices[3].y, _tmpVertices[3].z);
        _massPoints[3]._position = new Vector3(_massPoints[3]._position.x + extra, _massPoints[3]._position.y, _massPoints[3]._position.z);
        _tmpVertices[4] = new Vector3(_tmpVertices[4].x + extra, _tmpVertices[4].y, _tmpVertices[4].z);
        _massPoints[4]._position = new Vector3(_massPoints[4]._position.x + extra, _massPoints[4]._position.y, _massPoints[4]._position.z);
        _tmpVertices[5] = new Vector3(_tmpVertices[5].x + extra, _tmpVertices[5].y, _tmpVertices[5].z);
        _massPoints[5]._position = new Vector3(_massPoints[5]._position.x + extra, _massPoints[5]._position.y, _massPoints[5]._position.z);
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
    }
    public void ReSpring(Vector3[] vertices, which whichSpring, Block refBlock)
    {
        //質点の情報を全て初期化する
        _massPoints.Clear();
        _springs.Clear();
        switch (whichSpring)
        {
            case which.Right:
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (i == 2)
                    {
                        //左足の質点を，被挿入ブロック
                        _massPoints.Add(refBlock._massPoints[5]);
                    }

                    else
                    {
                        var massPoint = gameObject.AddComponent<MassPoint>();
                        massPoint.SetMassSpring(_mass, Vector3.zero, i, transform.TransformPoint(vertices[i]), this);
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
                    _springConstant, springLength: initialLength, _dampingConstant, 1.0f, springType: SpringType.Leg);
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
                    _springConstantLeg, springLength: initialLength, _dampingConstant, 1.0f, springType: SpringType.Leg);
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
            case which.Left:
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (i == 5)
                    {

                        _massPoints.Add(refBlock._massPoints[2]);
                    }
                    else
                    {
                        var massPoint = gameObject.AddComponent<MassPoint>();
                        massPoint.SetMassSpring(_mass, Vector3.zero, i, transform.TransformPoint(vertices[i]), this);
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
                    _springConstant, springLength: initialLength, _dampingConstant, 1.0f, springType: SpringType.Leg);
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
                    _springConstantLeg, springLength: initialLength, _dampingConstant, 1.0f, springType: SpringType.Leg);
                    _springs.Add(spring);
                    massPoint1.AddSpring(spring);
                    massPoint2.AddSpring(spring);

                }
                break;
            case which.Top:
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (i == 5)
                    {

                        _massPoints.Add(refBlock._massPoints[2]);
                    }
                    else
                    {
                        var massPoint = gameObject.AddComponent<MassPoint>();
                        massPoint.SetMassSpring(_mass, Vector3.zero, i, transform.TransformPoint(vertices[i]), this);
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
                    _springConstant, springLength: initialLength, _dampingConstant, 1.0f, springType: SpringType.Leg);
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
                    _springConstantLeg, springLength: initialLength, _dampingConstant, 1.0f, springType: SpringType.Leg);
                    _springs.Add(spring);
                    massPoint1.AddSpring(spring);
                    massPoint2.AddSpring(spring);

                }
                break;
            default:
                break;
        }

    }
}
