using Vintagestory.API.Client;
using Vintagestory.API.Common;
using HarmonyLib;
using Vintagestory.GameContent.Mechanics;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using System;


namespace HammerTime {

public class HammerTimeModSystem : ModSystem {
    ICoreClientAPI _capi;

    //public void log(string msg) {
    //    _capi.Logger.Debug(msg);
    //    _capi.ShowChatMessage(msg);
    //}

    //private void Scan() {
    //    var player = _capi.World.Player;
    //    var bs = player.CurrentBlockSelection;

    //    if (bs == null) {
    //        return;
    //    }

    //    var be = _capi.World.BlockAccessor.GetBlockEntity(bs.Position);
    //    log($"looking at: {bs.Position} | BE: {be?.GetType().Name ?? "none"}");
    //}

    private bool LookAtAnvil() {
        var player = _capi.World.Player;
        var bs = player.CurrentBlockSelection;

        if (bs == null) {
            return false;
        }

        var be = _capi.World.BlockAccessor.GetBlockEntity(bs.Position);
        return be?.GetType() == typeof(BlockEntityAnvil);
    }

    private BlockPos FindNearbyHelveHammer(BlockPos anvilPos) {
        int dist = 3;
        foreach (var facing in BlockFacing.HORIZONTALS) {
            var checkPos = anvilPos.AddCopy(facing.Normali.X * dist, 0, facing.Normali.Z * dist);
            log($"check for {checkPos}");
            if (_capi.World.BlockAccessor.GetBlockEntity(checkPos) is BEHelveHammer helve) {
                log("Helv?");
                return checkPos;
            }
        }
        return null;
    }

    private void OnTick(float dt) {
        //Scan();
        // Clear ghost on all helve hammers first

        if (_capi.World.Player.InventoryManager.ActiveTool != EnumTool.Hammer) {
            // clear map
            GhostState.Clear();
            return;
        }

        if (!LookAtAnvil()) {
            GhostState.Clear();
            return;
        }

        BlockSelection bs = _capi.World.Player.CurrentBlockSelection;
        BlockPos pos = FindNearbyHelveHammer(bs.Position);
        log($"{pos}");
        if (pos != null) {
            GhostState.Set(pos, true);
        } else {
            GhostState.Clear();
        }
    }

    public override void StartClientSide(ICoreClientAPI api) {
        _capi = api;
        Mod.Logger.Notification("HammerTime starting");
        var harmony = new Harmony("HammerTime");
        harmony.PatchAll();
        api.Event.RegisterGameTickListener(OnTick, 200);
    }

    /// <summary>
    /// Simple registry of which positions are in "ghost" (transparent) mode.
    /// </summary>
    public static class GhostState {
        // Using a concurrent-safe approach since rendering can be on another thread
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<BlockPos, bool> ghostPositions =
            new();

        public static void Set(BlockPos pos, bool ghost) {
            if (ghost) {
                ghostPositions[pos] = true;
            } else {
                ghostPositions.TryRemove(pos, out _);
            }
        }

        public static bool IsGhost(BlockPos pos) => ghostPositions.ContainsKey(pos);

        public static void Clear() => ghostPositions.Clear();
    }
}
}
