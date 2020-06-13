using System;
using System.Linq;
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
        return (T)components.FirstOrDefault(x => x.GetType() == typeof(T));
    }

    public T GetCComponent<T>(Predicate<Comp> p) where T : Comp
    {
        return (T)components.FirstOrDefault(x => x.GetType() == typeof(T) && p(x));
    }

    public bool TryGetCComponent<T>(out T t) where T : Comp
    {
        foreach (var comp in components)
        {
            if (comp.GetType() == typeof(T))
            {
                t = (T)comp;
                return true;
            }
        }

        t = default;
        return false;
    }

    public bool HasCComponent<T>() where T : Comp
    {
        return components.Any(x => x.GetType() == typeof(T));
    }

    public List<Comp> CComponentsOfType<T>() where T : Comp
    {
        return components.FindAll(x => x.GetType() == typeof(T));
    }

    public T AddCComponent<T>(params object[] p) where T : Comp
    {
        T t = (T)Activator.CreateInstance(typeof(T), p);
        components.Add(t);

        return t;
    }

    public void RemoveCComponent<T>() where T : Comp
    {
        if (components.Any(x => x.GetType() == typeof(T)))
        {
            components.Remove(components.FirstOrDefault(x => x.GetType() == typeof(T)));
        }
    }

    public void RemoveCComponent<T>(Predicate<Comp> p) where T : Comp
    {
        if (components.Any(x => x.GetType() == typeof(T) && p(x)))
        {
            components.Remove(components.FirstOrDefault(x => x.GetType() == typeof(T) && p(x)));
        }
    }

    public Comp[] MyComponents()
    {
        return components.ToArray();
    }
}
