using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.Intrinsics.X86;

namespace MonoGameLibrary;

public static class MyMath
{
    public static float Clamp(float thisValue, float minValue, float maxValue)
    {
        if (thisValue > maxValue) return maxValue;
        else if (thisValue < minValue) return minValue;
        return thisValue;
    }

    public static int Clamp(int thisValue, int minValue, int maxValue)
    {
        if (thisValue > maxValue) return maxValue;
        else if (thisValue < minValue) return minValue;
        return thisValue;
    }
}