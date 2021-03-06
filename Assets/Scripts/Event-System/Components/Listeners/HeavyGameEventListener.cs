using System;
using UnityEngine;

public class HeavyGameEventListener : MonoBehaviour, IListener<HeavyGameEventData>, IComparable
{

    [SerializeField] public HeavyGameEvent target;
    [SerializeField] protected HeavyGameEventCallback callback;

    [SerializeField] protected int priority;
    public int Priority { get => this.priority; }

    void OnEnable()
    {
        this.target.Subscribe(this);
    }

    void OnDisable()
    {
        this.target.Unsubscribe(this);
    }

    public int CompareTo(object obj) {
        if(obj == null)
        {
            return 1;
        }
        HeavyGameEventListener other = obj as HeavyGameEventListener;
        if(this.Priority > other.Priority)
        {
            return -1;
        }
        return 1;
    }

    public void OnRaise(HeavyGameEventData data)
    {
    	this.callback.Invoke(data);
    }
}
