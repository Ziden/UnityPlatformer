using System;
using System.Collections.Generic;
using UnityEngine;

public class StateManager<Type>
{
    private HashSet<Type> States = new HashSet<Type>();

    private HashSet<Type> wasAdded = new HashSet<Type>();
    private HashSet<Type> wasRemoved = new HashSet<Type>();

    public void Add(Type state)
    {
        //Debug.Log("STATE ADD " + state);
        States.Add(state);
        wasAdded.Add(state);
    }

    public HashSet<Type> GetAll()
    {
        return States;
    }

    public bool WasAdded(Type state)
    {
        return wasAdded.Contains(state);
    }

    public bool BeenModified()
    {
        return wasAdded.Count + wasRemoved.Count > 0;
    }

    public bool WasRemoved(Type state)
    {
        return wasRemoved.Contains(state);
    }

    public bool Has(Type state)
    {
        return States.Contains(state);
    }

    public void Remove(Type state)
    {

        if(States.Remove(state))
            wasRemoved.Add(state);
    }

    public void ClearHistory()
    {
        wasRemoved.Clear();
        wasAdded.Clear();
    }
}
