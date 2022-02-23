using System;
using System.Collections.Generic;
using DefaultEcs;

namespace revghost.Ecs;

public class OrderElement
{
    public List<Entity> Dependencies = new();

    public void Clear()
    {
        Dependencies.Clear();
    }
}