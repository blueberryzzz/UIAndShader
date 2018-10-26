using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ChunkDisappearImageTest : MonoBehaviour {

    public ChunkDisappearImage ImageInstance;
    public Button StartButton;
    public Button ResetButton;
    // Use this for initialization
    void Start () {
        StartButton.onClick.AddListener(() => { ImageInstance.StartDisappear(); });
        ResetButton.onClick.AddListener(() => { ImageInstance.ResetImage(); });
    }
}
