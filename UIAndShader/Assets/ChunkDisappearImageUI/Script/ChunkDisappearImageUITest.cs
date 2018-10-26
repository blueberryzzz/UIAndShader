using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ChunkDisappearImageUITest : MonoBehaviour {

    public ChunkDisappearImageUI ImageInstance;
    public Button StartButton;
    public Button ResetButton;
    // Use this for initialization
    void Start () {
        StartButton.onClick.AddListener(() => { ImageInstance.StartDisappear(); });
        ResetButton.onClick.AddListener(() => { ImageInstance.ResetImage(); });
    }
}
