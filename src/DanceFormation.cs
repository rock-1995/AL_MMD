using System.Numerics;

namespace ALMMD;

internal static class DanceFormation
{
    // AL's native character is about 16 world units tall (1.6 metres).
    public const float WorldUnitsPerMetre = 10f;
    public static int MotionIndex(int slot, int motionCount) => slot % motionCount;
    public static Vector3 WorldOffset(int slot, int dancerCount, int motionCount) =>
        Offset(slot, dancerCount, motionCount) * WorldUnitsPerMetre;

    public static Vector3 Offset(int slot, int dancerCount, int motionCount)
    {
        int group = slot / motionCount;
        if (group == 0) return Vector3.Zero;
        int backGroups = (dancerCount + motionCount - 1) / motionCount - 1;
        return new Vector3((group - 1 - (backGroups - 1) * .5f) * 2f, 0, -2f);
    }
}
