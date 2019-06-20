using System;
using System.Collections.Generic;

public class ComponentHolder<Comp>
{
    protected List<Comp> components;

    public void SetComponentList(List<Comp> comps)
    {
        components = new List<Comp>(comps);
    }

    public T GetCComponent<T>() where T : Comp
    {
        return (T)components.Find(x => x.GetType() == typeof(T));
    }

    public bool HasCComponent<T>() where T : Comp
    {
        return components.Find(x => x.GetType() == typeof(T)) != null;
    }

    public T AddCComponent<T>(params object[] p) where T : Comp
    {
        T t = (T)Activator.CreateInstance(typeof(T), p);
        components.Add(t);

        return t;
    }

    public void RemoveCComponent<T>() where T : Comp
    {
        if (components.Find(x => x.GetType() == typeof(T)) != null)
        {
            components.Remove(components.Find(x => x.GetType() == typeof(T)));
        }
    }

    public Comp[] MyComponents()
    {
        return components.ToArray();
    }
}
