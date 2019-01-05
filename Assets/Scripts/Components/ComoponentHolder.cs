using System;
using System.Collections.Generic;

public class ComponentHolder
{
    protected List<CComponent> components;

    public void SetComponentList(List<CComponent> comps)
    {
        components = new List<CComponent>(comps);
    }

    public T GetCComponent<T>() where T : CComponent
    {
        return (T)components.Find(x => x.GetType() == typeof(T));
    }

    public bool HasCComponent<T>() where T : CComponent
    {
        return components.Find(x => x.GetType() == typeof(T)) != null;
    }

    public T AddCComponent<T>(params object[] p) where T : CComponent
    {
        T t = (T)Activator.CreateInstance(typeof(T), p);
        components.Add(t);

        return t;
    }

    public void RemoveCComponent<T>() where T : CComponent
    {
        if (components.Find(x => x.GetType() == typeof(T)) != null)
        {
            components.Remove(components.Find(x => x.GetType() == typeof(T)));
        }
    }

    public CComponent[] MyComponents()
    {
        return components.ToArray();
    }
}
