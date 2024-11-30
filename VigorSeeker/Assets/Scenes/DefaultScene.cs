using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DefaultScene : MonoBehaviour
{
    /// <summary>[
    /// 選択中のブロック
    /// </summary>
    public Block focusedBlock;
    /// <summary>
    /// 接続対象のブロック(1つ前の操作で選択していたブロック)
    /// </summary>
    public Block previousBlock;
    public string message;
    public bool isVisible;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("✅DefaultScene.cs is calling");
    }

}
