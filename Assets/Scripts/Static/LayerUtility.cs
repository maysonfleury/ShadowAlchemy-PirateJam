using UnityEngine;

public static class LayerUtility
{
    //Creates a new LayerMask from a layer index
    public static LayerMask LayerToLayerMask(int layer)
    {
        return 1 << layer;
    }

    //Sets a specific LayerMask bit relative to the layer index
    public static void AddLayerToMask(ref LayerMask mask, int layer)
    {
        mask |= (1 << layer);
    }

    //Combines LayerMasks into a single new LayerMask
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

    //Unsets bits from a LayerMask, using another LayerMask as a reference
    public static void UnsetBitsFromMask(ref LayerMask mask, LayerMask bitsToRemove)
    {
        mask &= ~(1 << bitsToRemove);
    }

    //Returns true if any identical bits are set between both LayerMasks
    public static bool CheckMaskOverlap(LayerMask mask1, LayerMask mask2)
    {
        return (mask1 & mask2) != 0;
    }

}
