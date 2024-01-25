using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMemento
{
    public int Id
    {
        get;
    }
    public void Restore();
}
