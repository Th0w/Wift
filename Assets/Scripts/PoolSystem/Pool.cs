﻿using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class Pool : MonoBehaviour
{
    private int quantity;
    private Poolable prefab;

    private Poolable[] cached;
    private Queue<Poolable> inactives;
    private List<Poolable> actives;
    private Subject<Poolable> onSpawn, onRecycle;
    [SerializeField]
    private string _name;
    public Poolable Prefab { get { return prefab; } }
    public int ActiveCount { get { return actives.Count; } }
    public int InactiveCount { get { return inactives.Count; } }
    public int totalCount { get { return ActiveCount + InactiveCount; } }
    public bool empty { get { return InactiveCount == 0; } }
    public IObservable<Poolable> OnSpawn { get { return onSpawn; } }
    public IObservable<Poolable> OnRecycle { get { return onRecycle; } }
    public Poolable[] Pooled { get { return cached; } }
    public string Name { get { return _name; } private set { _name = value; } }

    public void Init(int quantity, Poolable prefab, string name)
    {
        this.prefab = prefab;
        this.quantity = quantity;
        this.name = string.Format("Pool_{0}_{1}", prefab.name, name);
        this.Name = name;
        inactives = new Queue<Poolable>(quantity);
        actives = new List<Poolable>(quantity);
        cached = new Poolable[quantity];

        int i;
        Poolable p;
        for (i = 0; i < quantity; ++i)
        {
            p = cached[i] = Instantiate(prefab).Init(this);
            p.name = string.Format("{0}#{1}", p.name, i);
            inactives.Enqueue(p);
            p.transform.SetParent(transform);
        }
        onSpawn = new Subject<Poolable>();
        onRecycle = new Subject<Poolable>();
    }

    /// <summary>
    /// Recycle all active game objects from this pools
    /// </summary>
    internal void Recycle(bool deep = false) {
        if (deep == true) {
            cached.Where(poolable => poolable.gameObject.activeSelf)
                .ForEach(poolable => Recycle(poolable));
        } else {
            actives.ForEach(active => Recycle(active));
        }
    }

    public void Spawn(object args, bool force = false)
    {
        if (inactives == null)
        {
            Debug.Log("Pool not yet initialized. Please retry later.");
        }
        else if (inactives.Count > 0)
        {
            var obj = inactives.Dequeue();
            actives.Add(obj);
            obj.Spawn(args);
            obj.transform.SetParent(transform.parent);
            onSpawn.OnNext(obj);
        }
        else
        {
            if (force == true)
            {
                var first = actives[0];
                actives.Remove(first);
                first.Recycle();
                actives.Add(first);
                first.Spawn(args);
            }
            else
            {
//                Debug.LogError("Not enough pooled.");
            }
        }
    }

    public void Recycle(Poolable obj)
    {
        if (inactives.Contains(obj) == true)
        {
            throw new Exception("Item is already in inactives...");
        }
        else if (actives.Contains(obj) == false)
        {
            throw new Exception("Item is not active...");
        }
        else
        {
            obj.Recycle();
            actives.Remove(obj);
            inactives.Enqueue(obj);
            onRecycle.OnNext(obj);
        }
    }
}