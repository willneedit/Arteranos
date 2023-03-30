using Arteranos.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListView : UIBehaviour, IList<UIBehaviour>
{
    private RectTransform ContentBox = null;
    private Scrollbar VerticalScrollbar = null;

    public int Count => ContentBox.childCount;

    public bool IsReadOnly => false;

    public UIBehaviour this[int index]
    {
        get => ContentBox.GetChild(index).GetComponent<UIBehaviour>();
        set
        {
            RemoveAt(index);
            Insert(index, value);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        RectTransform vp = GetComponent<ScrollRect>().viewport;
        Debug.Log(vp.transform.name);
        ContentBox = vp.GetChild(0).GetComponent<RectTransform>();
        Debug.Log(ContentBox.transform.name);

        VerticalScrollbar = GetComponent<ScrollRect>().verticalScrollbar;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        foreach(UIBehaviour item in this)
            item.enabled = true;

        VerticalScrollbar.interactable = true;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        foreach(UIBehaviour item in this)
            item.enabled = false;

        VerticalScrollbar.interactable = false;
    }

    public int IndexOf(UIBehaviour item) => throw new System.NotImplementedException();
    public void Insert(int index, UIBehaviour item)
    {
        item.transform.SetParent(ContentBox, false);
        item.transform.SetSiblingIndex(index);
    }

    public void RemoveAt(int index) => Destroy(ContentBox.GetChild(index).gameObject);
    public void Add(UIBehaviour item) => item.transform.SetParent(ContentBox, false);
    public void Clear()
    {
        while(ContentBox.childCount > 0) Destroy(ContentBox.GetChild(0).gameObject);
    }

    public bool Contains(UIBehaviour item) => throw new System.NotImplementedException();
    public void CopyTo(UIBehaviour[] array, int arrayIndex) => throw new System.NotImplementedException();
    public bool Remove(UIBehaviour item) => throw new System.NotImplementedException();
    public IEnumerator<UIBehaviour> GetEnumerator()
    {
        for(int i = 0, c = ContentBox.childCount; i < c; i++)
            yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
