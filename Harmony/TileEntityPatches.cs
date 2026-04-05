using HarmonyLib;

namespace Teleporters.Harmony
{
    [HarmonyPatch(typeof(TileEntity))]
    [HarmonyPatch("Instantiate")]
    public class TileEntityInstantiatePatch
    {
        private const int TILE_ENTITY_TYPE_ID = 210;

        public static bool Prefix(ref TileEntity __result, TileEntityType type, Chunk _chunk)
        {
            if ((int)type == TILE_ENTITY_TYPE_ID)
            {
                __result = new TileEntityTeleporter(_chunk);
                return false;
            }
            return true;
        }
    }
}
