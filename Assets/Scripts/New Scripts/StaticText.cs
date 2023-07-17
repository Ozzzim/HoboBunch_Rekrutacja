using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticText : MonoBehaviour
{
    public float timeToDestroy = 1.2f;

    [SerializeField]
    TMPro.TMP_Text tmpText;

    public void SetText(string text)
    {
        tmpText.text = text;
    }

    // Update is called once per frame
    void Start()
    {
        Destroy(gameObject,timeToDestroy);
    }
}
