using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAdventure.Models;
public class FenceObject : RenderableGameObject
{
    public FenceObject(SpriteSheet spriteSheet, (int X, int Y) position)
        : base(spriteSheet, position) { }
}

