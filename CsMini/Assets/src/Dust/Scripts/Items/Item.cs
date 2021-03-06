using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : ScriptableObject
{
    [Header("Description")]
    public string nameWeapon;
    public string description;
    public Sprite icon;

    public virtual void use()
    {
        Debug.Log(name + " use");
    }
}
