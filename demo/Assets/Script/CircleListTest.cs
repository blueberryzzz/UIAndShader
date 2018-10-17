using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CircleListTest : MonoBehaviour {

    //列表实例
    public CircleList CircleListInstance;
    //item对象
    public RectTransform Item;
    //Sprite列表，用于初始化item
    public SpriteList SpriteListInstance;
    //添加按钮
    public Button AddItemBtn;
    //删除按钮
    public Button RemoveItemBtn;

	// Use this for initialization
	void Start () {
        AddItemBtn.onClick.AddListener(AddOneItem);
        RemoveItemBtn.onClick.AddListener(RemoveOneItem);
    }

    //添加一个item
    public void AddOneItem()
    {
        var itemInstance = Instantiate(Item);
        itemInstance.GetComponent<Image>().sprite = SpriteListInstance.spriteContent[CircleListInstance.ItemTransformList.Count % SpriteListInstance.spriteContent.Count];
        CircleListInstance.AddItem(itemInstance);
    }

    //移除一个item
    public void RemoveOneItem()
    {
        if (CircleListInstance.ItemTransformList.Count == 0) return;
        CircleListInstance.RemoveItem(CircleListInstance.ItemTransformList[0]);
    }
}
