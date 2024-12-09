using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


[InitializeOnLoad]
public static class CreateButtonUi
{
    public static int ID = 0;
    public static Block _block;
    public static List<Block> _blocks;
    public static DefaultScene defaultScene;
    public static float _margin = 0.2f;
    public static float blockVallaySize = 4.454382f - 4.378539f;

    public static float margin = 4.378539f - 3.99421f;
    public static int rowSize = 35;
    public static bool isDebug = true;
    public static float _marginB = 0.08533333333f;


    static CreateButtonUi()
    {
        SceneView.duringSceneGui += OnGui;
        SceneView.duringSceneGui += SceneViewOnDuringSceneGui;
        var o = GameObject.Find("DefaultScene");
        if (o != null)
        {
            defaultScene = o.GetComponent<DefaultScene>();
        }
        else

        {
            defaultScene = new DefaultScene();
        }
        //ヒエラルキーで選択されたオブジェクトが変更されたときに呼び出される関数を登録
        Selection.selectionChanged += () =>
        {
            // Debug.Log("selection changed");
            // Debug.Log("selection.activeGameObject is " + Selection.gameObjects.Length);
            if (Selection.activeGameObject != null)
            {
                var x = Selection.activeGameObject.GetComponent<Transform>();

            }


            if (defaultScene.focusedBlock == null
            && Selection.activeGameObject != null)
            {
                if (Selection.activeGameObject.GetComponent<Block>() != null)
                {
                    //Debug.Log("selectedBlock is not null");
                    //Debug.Log("selection.activeGameObject is " + Selection.gameObjects.Length);
                    defaultScene.focusedBlock = Selection.activeGameObject.GetComponent<Block>();
                    if (defaultScene.previousBlock != null)
                    {
                        defaultScene.previousBlock._isAnimatable = true;
                    }
                    defaultScene.focusedBlock._isAnimatable = false;
                }

            }
            else if (defaultScene.focusedBlock != null && Selection.activeGameObject != null)
            {
                if (Selection.activeObject.GetComponent<Block>() != null)
                {
                    //Debug.Log("swapping selectedBlock and connectedBlock");
                    //旧選択対象を接続対象に設定
                    if (defaultScene.previousBlock != null)
                    {
                        defaultScene.previousBlock._isAnimatable = true;
                    }
                    defaultScene.previousBlock = defaultScene.focusedBlock;
                    defaultScene.focusedBlock = Selection.activeGameObject.GetComponent<Block>();
                    defaultScene.focusedBlock._isAnimatable = false;
                }
            }
            else if (Selection.gameObjects.Length == 0)
            {
                //Debug.Log("Yes");
                defaultScene.focusedBlock._isAnimatable = true;
                defaultScene.previousBlock._isAnimatable = true;
                defaultScene.focusedBlock = null;
                defaultScene.previousBlock = null;

            }
        };
        _blocks = new List<Block>();
    }

    private static void OnGui(SceneView sceneView)
    {
        Handles.BeginGUI();
        //if (_block == null)
        //{
        //    _block = LoadDataTable();
        //}
        // ������ UI��`�悷�鏈�����L�q
        ShowButtons(sceneView.position.size);
        ShowInfoPanel();
        Handles.EndGUI();
    }
    private static void ShowInfoPanel()
    {
        var rect = new Rect(10, 10, 400, 120);
        GUI.Box(rect, "current info");
        GUI.Label(new Rect(20, 30, 180, 20), "slecting block id: " + defaultScene.focusedBlock?.ID);
        GUI.Label(new Rect(20, 50, 180, 20), "connected block id: " + defaultScene.previousBlock?.ID);
        //GUI.Label(new Rect(20, 70, 180, 20), "spring force" + defaultScene.selectedBlock?._massPoints[2].CalcForce());
        GUI.Label(new Rect(20, 90, 180, 20), "Message " + defaultScene?.isVisible);

    }

    /// <summary>
    /// シーンビューのイベントを監視する
    /// シーンビューでのキーイベントはここで登録する
    /// </summary>
    /// <param name="obj"></param>
    private static void SceneViewOnDuringSceneGui(SceneView obj)
    {
        var ev = Event.current;
        if (ev.type == EventType.KeyDown)
        {
            if (ev.keyCode == KeyCode.Space)
            {
                //Debug.Log("Space key is pressed");
                foreach (var block in _blocks)
                {
                    //Debug.Log("[foreach] block id: " + block.ID);
                    block.OnSpaceKeyPress();
                }
            }
            /*
            【How to use】
            背側を前，目側を裏と定義する(従って，背側の右ポケットが右ポケットとなる)
            */
            /*
            ←(テンキー4) : [wip]選択中のブロックを左上に接続
            →(テンキー7) : 選択中のブロックを左上に接続(選択中ブロックの右ポケットに接続対象ブロック左脚を挿入)
            →(テンキー9) : 選択中のブロックを右上に接続(選択中ブロックの左ポケットに接続対象ブロック右脚を挿入)
            ↑(テンキー8) : 選択中のブロックを縦に連結
            A(テンキー1) : 選択中のブロックを左下に接続
            */
            if (defaultScene.focusedBlock != null
            && defaultScene.previousBlock != null)
            {
                if (ev.keyCode == KeyCode.UpArrow)
                {
                    //Debug.Log("Up arrow key is pressed");
                    defaultScene.focusedBlock.OnUpKeyPress();
                    //defaultScene.connectedBlock.OnUpKeyPress();
                }
                if (ev.keyCode == KeyCode.Keypad9 || ev.keyCode == KeyCode.Alpha9)
                {
                    //Debug.Log("Right arrow key is pressed");
                    defaultScene.focusedBlock.ConnectFocusedBlockWithPreviousBlock(
                        defaultScene.previousBlock,
                        ConnectDirection.UpperRight);
                    //defaultScene.connectedBlock.OnRightKeyPress();
                }
                if (ev.keyCode == KeyCode.Keypad7 || ev.keyCode == KeyCode.Alpha7)
                {
                    defaultScene.focusedBlock.ConnectFocusedBlockWithPreviousBlock(
                        defaultScene.previousBlock,
                        ConnectDirection.UpperLeft);
                }
                if (ev.keyCode == KeyCode.Keypad1 || ev.keyCode == KeyCode.Alpha1)
                {
                    defaultScene.previousBlock.ConnectFocusedBlockWithPreviousBlock(
                        defaultScene.focusedBlock,
                        ConnectDirection.UpperLeft);
                }
                if (ev.keyCode == KeyCode.Keypad3 || ev.keyCode == KeyCode.Alpha3)
                {
                    defaultScene.previousBlock.ConnectFocusedBlockWithPreviousBlock(
                        defaultScene.focusedBlock,
                        ConnectDirection.UpperRight);
                }
            }
        }
    }

    /// <summary>
    /// set button
    /// </summary>
    #region: ボタンの表示
    private static void ShowButtons(Vector2 sceneSize)
    {
        var count = 1;
        var buttonSize = 90;
        foreach (var i in Enumerable.Range(0, count))
        {
            //var block = new Block();
            // ボタンサイズ
            var rect = new Rect(
              sceneSize.x / 2 - buttonSize * count / 2 + buttonSize * i,
              sceneSize.y - 60,
              buttonSize,
              40);
            var rect2 = new Rect(
              sceneSize.x / 2 - buttonSize * (count) / 2 + buttonSize * (i + 1),
              sceneSize.y - 60,
              buttonSize,
              40);
            var rect3 = new Rect(
            sceneSize.x / 2 - buttonSize * (count) / 2 + buttonSize * (i + 2),
            sceneSize.y - 60,
            buttonSize,
            40);
            var rect4 = new Rect(
            sceneSize.x / 2 - buttonSize * (count) / 2 + buttonSize * (i + 3),
            sceneSize.y - 60,
            buttonSize,
            40);
            var rect5 = new Rect(
            sceneSize.x / 2 - buttonSize * (count) / 2 + buttonSize * (i + 4),
            sceneSize.y - 60,
            buttonSize,
            40);

            if (GUI.Button(rect, "ブロックを追加"))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/blockv1.prefab");
                if (prefab != null)
                {
                    var obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    var vertices = CreateMeshVertices(ReadObjFile("block"));
                    var triangles = CreateTriangles(ReadObjFile("block"));
                    Selection.activeObject = obj;
                    obj.name = "block:ID " + ID;
                    Undo.RegisterCreatedObjectUndo(obj, "create object");
                    //Debug.Log("Info: gameObject Added. ID is " + ID);
                    //blockプレハブにアタッチされているblock.csにアクセスする
                    var block = obj.GetComponent<Block>();
                    var meshfilter = obj.GetComponent<MeshFilter>();
                    var mesh = new Mesh();
                    mesh.SetVertices(vertices);
                    mesh.SetTriangles(triangles, 0);
                    mesh.RecalculateNormals();
                    //mesh.SetNormals();
                    meshfilter.mesh = mesh;
                    block.mesh = mesh;
                    if (_blocks.Count != 0)
                    {
                        obj.transform.position = _blocks[_blocks.Count - 1].transform.position + new Vector3(margin * 5, 0, 0);
                    }
                    block.SetVertices();
                    block.defaultScene = defaultScene;
                    block.ID = ID;
                    ID++;
                    _blocks.Add(block);
                }
            }
            if (GUI.Button(rect2, "cylinder"))
            {

                if (Selection.gameObjects.Length == 1
                && Selection.activeGameObject.GetComponent<ProBuilderShape>() != null)
                {
                    var shape = Selection.activeGameObject.GetComponents<ProBuilderShape>();
                    var c_Transform = Selection.activeGameObject.transform;
                    var row = GetRow(shape[0].m_Size);
                    var column = GetColumn(shape[0].m_Size);
                    if (row == -1 || column == -1)
                    {
                        //Debug.Log("Error: row or column is not correct");
                        defaultScene.message = "Error: row or column is not correct";
                        Debug.Log("Hint: row is " + row + " column is " + column);
                    }
                    else
                    {
                        //Debug.Log("Hint: row is " + row + " column is " + column);
                        //defaultScene.message = "row: " + row + " column: " + column;
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/blockv1.prefab");
                        var parent = new GameObject("parent");
                        parent.transform.position = c_Transform.position;
                        //var scale = AdjustScale(shape[0].m_Size, ref column);
                        if (prefab != null)
                        {
                            for (int c = 0; c < column; c++)
                            {
                                var col = new GameObject("column" + c);
                                col.transform.parent = parent.transform;
                                for (int r = 0; r < rowSize; r++)
                                {
                                    var obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                                    obj.transform.parent = col.transform;
                                    var vertices = CreateMeshVertices(ReadObjFile("block"));
                                    var triangles = CreateTriangles(ReadObjFile("block"));
                                    Selection.activeObject = obj;
                                    obj.name = "block:ID " + ID;
                                    Undo.RegisterCreatedObjectUndo(obj, "create object");
                                    // Debug.Log("Info: gameObject Added. ID is " + ID);
                                    //blockプレハブにアタッチされているblock.csにアクセスする
                                    var block = obj.GetComponent<Block>();
                                    var meshfilter = obj.GetComponent<MeshFilter>();
                                    var mesh = new Mesh();
                                    mesh.SetVertices(vertices);
                                    mesh.SetTriangles(triangles, 0);
                                    //mesh.SetNormals();
                                    meshfilter.mesh = mesh;
                                    block.mesh = mesh;
                                    block.SetVertices();
                                    block.ID = ID;
                                    block.defaultScene = defaultScene;
                                    block._isFixed = true;
                                    block._isAnimatable = false;
                                    block._isJoiningPrimitive = true;
                                    //block.transform.Rotate(0, 180, 0);
                                    ID++;
                                    _blocks.Add(block);
                                    var newX = (shape[0].m_Size.x * (shape[0].m_Size.y - c * _margin)) / (shape[0].m_Size.y);
                                    Vector3 newSize = new Vector3(shape[0].m_Size.x, shape[0].m_Size.y, shape[0].m_Size.x);
                                    var size = ChangeBlockVallySize(newSize, block);
                                    block.TransformInsertionModel();
                                    var radius = size * rowSize * 2 / (2 * Mathf.PI);
                                    //Debug.Log("H:radius is " + radius + "column is " + c);
                                    if (radius >= 2.0)
                                    {
                                        //radius = radius - 2.0f;
                                        //Debug.Log("radius is " + radius);

                                        block.transform.position = new Vector3(
                                            c_Transform.position.x,
                                            c_Transform.position.y - shape[0].m_Size.y / 2 + c * _margin,
                                            c_Transform.position.z - radius);
                                        //block.transform.RotateAround(block.transform.position, Vector3.up, 180.0f);
                                        if (c % 2 == 0)
                                        {
                                            block.transform.RotateAround(c_Transform.position, Vector3.up, 360.0f / (float)rowSize * (float)r);
                                            //Debug.Log("rotate around " + 360 / row * r);
                                        }
                                        else
                                        {
                                            block.transform.RotateAround(c_Transform.position, Vector3.up, 360.0f / (float)rowSize * (float)r + 180.0f / (float)rowSize);
                                            //Debug.Log("rotate around " + 360 / row * r);
                                        }
                                        //block.transform.RotateAround(block.transform.position, Vector3.up, 180);
                                    }
                                    else
                                    {
                                        radius = 0.0f;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (GUI.Button(rect3, "pipe"))
            {
                Debug.Log("convert to block");
                if (Selection.gameObjects.Length == 1
                && Selection.activeGameObject.GetComponent<ProBuilderShape>() != null)
                {
                    var shape = Selection.activeGameObject.GetComponents<ProBuilderShape>();
                    var c_Transform = Selection.activeGameObject.transform;
                    var row = GetRow(shape[0].m_Size);
                    var column = GetColumn(shape[0].m_Size);
                    if (row == -1 || column == -1)
                    {
                        Debug.Log("Error: row or column is not correct");
                        defaultScene.message = "Error: row or column is not correct";
                    }
                    else
                    {
                        defaultScene.message = "row: " + row + " column: " + column;
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/blockv1.prefab");
                        var parent = new GameObject("parent");
                        parent.transform.position = c_Transform.position;
                        if (prefab != null)
                        {
                            for (int c = 0; c < column; c++)
                            {
                                for (int r = 0; r < rowSize; r++)
                                {
                                    var obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                                    obj.transform.parent = parent.transform;
                                    var vertices = CreateMeshVertices(ReadObjFile("block"));
                                    var triangles = CreateTriangles(ReadObjFile("block"));
                                    Selection.activeObject = obj;
                                    obj.name = "block:ID " + ID;
                                    Undo.RegisterCreatedObjectUndo(obj, "create object");
                                    // Debug.Log("Info: gameObject Added. ID is " + ID);
                                    //blockプレハブにアタッチされているblock.csにアクセスする
                                    var block = obj.GetComponent<Block>();
                                    var meshfilter = obj.GetComponent<MeshFilter>();
                                    var mesh = new Mesh();
                                    mesh.SetVertices(vertices);
                                    mesh.SetTriangles(triangles, 0);
                                    //mesh.SetNormals();
                                    meshfilter.mesh = mesh;
                                    block.mesh = mesh;
                                    block.SetVertices();
                                    block.ID = ID;
                                    block.defaultScene = defaultScene;
                                    block._isFixed = true;
                                    block._isAnimatable = false;
                                    block._isJoiningPrimitive = true;
                                    ID++;
                                    _blocks.Add(block);
                                    var size = ChangeBlockVallySize(shape[0].m_Size, block);
                                    block.TransformInsertionModel();
                                    var radius = size * rowSize * 2 / (2 * Mathf.PI);
                                    if (shape[0].m_Size.x < 4.0)
                                    {
                                        radius = blockVallaySize * row * 2 / (2 * Mathf.PI);
                                    }
                                    if (radius >= 2.0)
                                    {
                                        radius = radius - 2.0f;
                                    }
                                    else
                                    {
                                        radius = 0.0f;
                                    }
                                    block.transform.position = new Vector3(c_Transform.position.x, c_Transform.position.y - shape[0].m_Size.y / 2 + c * _margin, c_Transform.position.z + radius);
                                    if (c % 2 == 0)
                                    {
                                        block.transform.RotateAround(c_Transform.position, Vector3.up, 360.0f / (float)rowSize * (float)r);
                                        //Debug.Log("rotate around " + 360 / row * r);
                                    }
                                    else
                                    {
                                        block.transform.RotateAround(c_Transform.position, Vector3.up, 360.0f / (float)rowSize * (float)r + 180.0f / (float)rowSize);
                                        //Debug.Log("rotate around " + 360 / row * r);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (GUI.Button(rect4, "sphere"))
            {
                if (Selection.gameObjects.Length == 1
                && Selection.activeGameObject.GetComponent<ProBuilderShape>() != null)
                {
                    var shape = Selection.activeGameObject.GetComponents<ProBuilderShape>();
                    var c_Transform = Selection.activeGameObject.transform;
                    var row = GetRow(shape[0].m_Size);
                    //列数は6固定
                    var column = 2;
                    //columnを求める
                    while (true)
                    {
                        float LegLength = 2.0f;
                        float cos = 0;
                        for (int j = 1; j < column; j++)
                        {
                            cos += Mathf.Cos(MathF.Abs(-MathF.PI / 4 + MathF.PI * 7 / (12 * (column - 1)) * (j - 1)));
                        }
                        float height = LegLength * Mathf.Sin(MathF.PI / 4)
                                       + LegLength * Mathf.Cos(MathF.PI / 3)
                                       + margin * cos;

                        if (height > shape[0].m_Size.y || column > 30)
                        {
                            break;
                        }
                        column++;
                    }
                    if (row == -1 || column == -1)
                    {
                        Debug.Log("Error: row or column is not correct");
                        defaultScene.message = "Error: row or column is not correct";
                        Debug.Log("Hint: row is " + row + " column is " + column);
                    }
                    else
                    {
                        Debug.Log("Hint: row is " + row + " column is " + column);
                        //defaultScene.message = "row: " + row + " column: " + column;
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/blockv1.prefab");
                        var parent = new GameObject("parent");
                        parent.transform.position = c_Transform.position;
                        var scale = AdjustScale(shape[0].m_Size, ref column);
                        if (prefab != null)
                        {
                            for (int c = 0; c < column; c++)
                            {
                                var col = new GameObject("column" + c);
                                col.transform.parent = parent.transform;
                                for (int r = 0; r < rowSize; r++)
                                {
                                    var obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

                                    //var pivot = new GameObject("pivot");
                                    //pivot.transform.parent = col.transform;
                                    var vertices = CreateMeshVertices(ReadObjFile("block"));
                                    var triangles = CreateTriangles(ReadObjFile("block"));
                                    Selection.activeObject = obj;
                                    obj.name = "block:ID " + ID;
                                    Undo.RegisterCreatedObjectUndo(obj, "create object");
                                    //blockプレハブにアタッチされているblock.csにアクセスする
                                    var block = obj.GetComponent<Block>();
                                    var meshfilter = obj.GetComponent<MeshFilter>();
                                    var mesh = new Mesh();
                                    mesh.SetVertices(vertices);
                                    mesh.SetTriangles(triangles, 0);
                                    meshfilter.mesh = mesh;
                                    block.mesh = mesh;
                                    block.SetVertices();
                                    block.ID = ID;
                                    block.defaultScene = defaultScene;
                                    block._isFixed = true;
                                    block._isAnimatable = false;
                                    block._isJoiningPrimitive = true;
                                    //block.transform.Rotate(0, 180, 0);
                                    ID++;
                                    _blocks.Add(block);
                                    //pivot の位置を設定

                                    Vector3 newSize = new Vector3(shape[0].m_Size.x, shape[0].m_Size.y, shape[0].m_Size.x);
                                    var size = ChangeBlockVallySize(newSize, block);
                                    block.TransformInsertionModel();
                                    var radius = size * rowSize * 2 / (2 * Mathf.PI);
                                    if (shape[0].m_Size.x <= 4.0)
                                    {
                                        radius = blockVallaySize * row * 2 / (2 * Mathf.PI);
                                        Debug.Log("radius is " + radius);
                                    }
                                    //Debug.Log("H:radius is " + radius + "column is " + c);
                                    if (radius >= 2.0)
                                    {
                                        //radius = radius - 2.0f;
                                        Debug.Log("radius is " + radius);

                                        block.transform.position = new Vector3(
                                            c_Transform.position.x,
                                            c_Transform.position.y - shape[0].m_Size.x / 2 * (1 / Mathf.Sqrt(2)),
                                            c_Transform.position.z - shape[0].m_Size.x / 2 * (1 / Mathf.Sqrt(2)) - (1 / Mathf.Sqrt(2)));
                                        //block.transform.RotateAround(block.transform.position, Vector3.up, 180.0f);
                                        if (c % 2 == 0)
                                        {
                                            block.transform.RotateAround(c_Transform.position, block.transform.up, 360.0f / (float)rowSize * (float)r);
                                            //Debug.Log("rotate around " + 360 / row * r);
                                        }
                                        else
                                        {
                                            //block.transform.RotateAround(c_Transform.position, block.transform.up, 360.0f / (float)rowSize * (float)r + 180.0f / (float)rowSize);
                                            block.transform.RotateAround(c_Transform.position, block.transform.up, 360.0f / (float)rowSize * (float)r);
                                            //Debug.Log("rotate around " + 360 / row * r);
                                        }
                                        block.UpdateVertices();
                                        //Debug.Log("pivot is " + block.v[VertexName.RightPocket]);
                                        //transform.rightはローカル座標系の右方向を示す(Vector3.rightはグローバル座標系の右方向を示すので注意)
                                        block.transform.RotateAround(block.v[VertexName.RightPocket], block.transform.right, -45);
                                        block.UpdateVertices();
                                        block.column = c;
                                        block.row = r;
                                        float degree = (45 + 30) / column;
                                        if (c > 0)
                                        {
                                            block.UpdateVertices();
                                            Vector3 moveVector = (_blocks[block.ID - 35].v[VertexName.RightLeg] - _blocks[block.ID - 35].v[VertexName.RightPocket]).normalized;
                                            block.transform.position = _blocks[block.ID - 35].transform.position
                                                                     + moveVector * margin;
                                            block.transform.rotation = _blocks[block.ID - 35].transform.rotation;
                                            block.UpdateVertices();
                                            block.transform.RotateAround(block.v[VertexName.LeftPocket], block.transform.right, degree);
                                            block.UpdateVertices();
                                        }

                                        //pivot.transform.position = block.transform.TransformPoint(block.v[VertexName.RightPocket]);
                                        obj.transform.parent = col.transform;
                                    }

                                    else
                                    {
                                        radius = 0.0f;
                                    }

                                    block.UpdateVertices();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion
    #region メッシュデータ作成
    public static string[] ReadObjFile(string fileName)
    {
        string texts = (Resources.Load(fileName, typeof(TextAsset)) as TextAsset).text;
        string[] lines = texts.Split('\n');
        return lines;
    }
    public static List<Vector3> CreateMeshVertices(string[] lines)
    {
        var vertices = new List<Vector3>();
        foreach (var line in lines)
        {
            if (line.StartsWith("v"))
            {
                var v = line.Split(' ');
                if (v[0] == "v")
                {
                    vertices.Add(new Vector3(float.Parse(v[1]), float.Parse(v[2]), float.Parse(v[3])));
                }
            }
        }

        return vertices;
    }
    /// <summary>
    /// 三角形のインデックスを格納する
    /// </summary>
    /// <param name="lines">objファイルの中身</param>
    /// <returns>三角形インデックスのリスト</returns>
    public static List<int> CreateTriangles(string[] lines)
    {
        var triangles = new List<int>();
        foreach (var line in lines)
        {
            if (line.StartsWith("f"))
            {
                var f = line.Split(' ');
                if (f[0] == "f" && f.Length == 4)
                {
                    var f1 = f[1].Split('/');
                    var f2 = f[2].Split('/');
                    var f3 = f[3].Split('/');
                    //unityの仕様上、triangleのインデックスを時計周りで格納する必要がある
                    //objファイルは反時計回りでインデックスが格納される
                    triangles.Add(int.Parse(f3[0]) - 1);
                    triangles.Add(int.Parse(f2[0]) - 1);
                    triangles.Add(int.Parse(f1[0]) - 1);
                }
            }
        }
        return triangles;
    }
    #endregion

    public static int GetRow(Vector3 size)
    {
        float blockVallaySize = 4.454382f - 4.378539f;
        //直径がブロックのサイズを下回る場合は何もしない
        if (size.x <= 2.0)
        {
            return -1;
        }
        //真円と仮定して作成する
        float circumference = size.x * Mathf.PI;
        int row = (int)(circumference / blockVallaySize);
        //一列あたりの個数が奇数の場合は偶数にする
        if (row % 2 == 1)
        {
            row++;
        }
        row = row / 2;
        return row;
    }
    public static int GetColumn(Vector3 size)
    {
        //ブロックの背の高さ
        float blockBackSize = 3.039667f - 1.119001f;
        if (size.y <= blockBackSize)
        {
            return -1;
        }
        float height = size.y;
        //+1は一番下のブロックの分
        int column = (int)((height - blockBackSize) / _margin) + 1;
        return column;
    }


    /// <summary>
    /// ブロックのスケールを調整する(案1)
    /// </summary>
    /// <param name="size">サイズ</param>
    /// <param name="column">カラム</param>
    /// <returns></returns>
    public static float AdjustScale(Vector3 cylinderSize, ref int column)
    {
        //Debug.Log("init column is " + column);
        float size = (cylinderSize.x * Mathf.PI / (rowSize * 2));
        //Debug.Log("size is " + size + "blockVallaySize is " + blockVallaySize);
        size = size / blockVallaySize;
        float height = cylinderSize.y;
        float blockBackSize = (3.039667f - 1.119001f);
        if (height <= blockBackSize)
        {
            return -1;
        }
        column = (int)((height - size) / _margin) + 1;
        //Debug.Log("after column is " + column);
        //Debug.Log("size is " + size);
        return size;
    }


    public static float ChangeBlockVallySize(Vector3 cylinderSize, Block block)
    {
        float size = (cylinderSize.x * Mathf.PI / (rowSize * 2));
        //Debug.Log("size is " + size + "blockVallaySize is " + blockVallaySize); ;
        //Debug.Log("after size is " + size + "blockVallaySize is " + blockVallaySize);
        if (size >= 0.5)
        {
            //Debug.Log("!!!!!!!!!!CAUTION!!!!!!!!!! size is too big");
        }
        float diff = size - blockVallaySize;
        if (diff > 0.0)
        {

        }
        // Debug.Log("diff is " + diff);
        block.UpdateValleySize(diff);
        return size;
    }
}


