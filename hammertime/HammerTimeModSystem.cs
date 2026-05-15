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
    BlockPos anvilPos;
    float sqrt_anvil_dist = 1.6f * 1.6f;

    public void log(string msg) {
        _capi.Logger.Debug(msg);
        _capi.ShowChatMessage(msg);
    }

    private bool IsLookingAtAnvil(BlockPos pos) {
        var be = _capi.World.BlockAccessor.GetBlockEntity(pos);
        return be?.GetType() == typeof(BlockEntityAnvil);
    }

    private bool AnvilDistOk() {
        var pos = _capi.World.Player.Entity.Pos.AsBlockPos;
        return pos.DistanceSqTo(anvilPos.X, anvilPos.Y, anvilPos.Z) < sqrt_anvil_dist;
    }

    private bool shouldTriggerMod() {
        var player = _capi.World.Player;
        var bs = player.CurrentBlockSelection;

        if (anvilPos != null) {
            if (AnvilDistOk()) {
                return true;
            }
            anvilPos = null;
        }

        if (bs == null) {
            return false;
        }

        if (!IsLookingAtAnvil(bs.Position)) {
            return false;
        }

        anvilPos = bs.Position;
        return AnvilDistOk();
    }

    private bool FindNearbyHelveHammers(BlockPos anvilPos, Action<BlockPos> callback) {
        bool found = false;
        int dist = 3;
        foreach (var facing in BlockFacing.HORIZONTALS) {
            var checkPos = anvilPos.AddCopy(facing.Normali.X * dist, 0, facing.Normali.Z * dist);
            if (_capi.World.BlockAccessor.GetBlockEntity(checkPos) is BEHelveHammer) {
                callback(checkPos);
                found = true;
            }
        }
        return found;
    }

    private void OnTick(float dt) {
        if (_capi.World.Player.InventoryManager.ActiveTool != EnumTool.Hammer ||!shouldTriggerMod()) {
            GhostState.Clear();
            return;
        }

        if (!FindNearbyHelveHammers(anvilPos, pos => GhostState.Set(pos, true))) {
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
