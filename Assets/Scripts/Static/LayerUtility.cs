using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LayerUtility
{

    public static T SetAllBits<T>() where T : Enum
    {
        int newValue = 0;

        foreach (T flag in Enum.GetValues(typeof(T)))
        {
            int flagValue = Convert.ToInt32(flag);
            newValue |= flagValue;
        }

        return (T)Enum.ToObject(typeof(T), newValue);
    }

    public static LayerMask LayerToLayerMask(int layer)
    {
        return 1 << layer;
    }

    public static LayerMask CombineMasks(LayerMask mask1, LayerMask mask2)
    {
        return mask1 | mask2;
    }

    public static LayerMask CombineMasks(params LayerMask[] masks)
    {
        LayerMask combinedMask = 0;

        foreach (LayerMask mask in masks)
        {
            combinedMask |= mask;
        }

        return combinedMask;
    }

    public static void RemoveMaskFromMask(ref LayerMask mask, LayerMask layerToRemove)
    {
        mask &= ~(1 << layerToRemove);
    }

    public static bool CheckMaskOverlap(LayerMask mask1, LayerMask mask2)
    {
        return (mask1 & mask2) != 0;
    }

}
