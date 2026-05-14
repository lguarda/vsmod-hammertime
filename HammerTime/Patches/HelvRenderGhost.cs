using Vintagestory.API.Client;
using Vintagestory.API.Common;
using HarmonyLib;
using Vintagestory.GameContent.Mechanics;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using System;
using System.Reflection;

[HarmonyPatch(typeof(HelveHammerRenderer), "OnRenderFrame")]
public class PatchHelveHammerRenderer {

    static readonly FieldInfo _beField = AccessTools.Field(typeof(HelveHammerRenderer), "be");
    static readonly FieldInfo _meshrefField = AccessTools.Field(typeof(HelveHammerRenderer), "meshref");
    static readonly FieldInfo _posField = AccessTools.Field(typeof(HelveHammerRenderer), "pos");
    static readonly FieldInfo _apiField = AccessTools.Field(typeof(HelveHammerRenderer), "api");
    static readonly FieldInfo _modelMatField = AccessTools.Field(typeof(HelveHammerRenderer), "ModelMat");
    static MethodInfo _disableDepth;
    static MethodInfo _enableDepth;
    static bool Prefix(HelveHammerRenderer __instance, float deltaTime, EnumRenderStage stage) {
        // BASE CODE: early exit if nothing to render ===
        var be = (BEHelveHammer)_beField.GetValue(__instance);
        var meshref = (MultiTextureMeshRef)_meshrefField.GetValue(__instance);
        if (meshref == null || be.HammerStack == null) {
            return true; // let original handle it
        }

        // MOD: check if this hammer should be ghosted ===
        // if not in ghost state, run original rendering normally
        var pos = (BlockPos)_posField.GetValue(__instance);
        if (!HammerTime.HammerTimeModSystem.GhostState.IsGhost(pos)) {
            return true;
        }

        // MOD: skip shadow render stages
        // prevents the hammer from writing to the shadow/depth map
        // which would otherwise occlude objects behind it
        if ((int)stage != 1) {
            return false;
        }

        // === BASE CODE: setup render dependencies ===
        var api = (ICoreClientAPI)_apiField.GetValue(__instance);
        var modelMat = (Matrixf)_modelMatField.GetValue(__instance);
        var render = api.Render;
        var cameraPos = api.World.Player.Entity.CameraPos;

        // === BASE CODE: build the model matrix (position + rotation of hammer) ===
        render.GlDisableCullFace();
        float num = be.facing.HorizontalAngleIndex * 90;
        float num2 = (be.facing == BlockFacing.NORTH || be.facing == BlockFacing.WEST) ? -0.0625f : 1.0625f;
        modelMat.Identity()
            .Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z)
            .RotateYDeg(num)
            .Translate(num2, 25f / 32f, 0.5f)
            .RotateZ(__instance.AngleRad) // current swing angle
            .Translate(-num2, -25f / 32f, -0.5f)
            .RotateYDeg(-num);

        // === GHOST MOD: disable depth test and depth write ===
        // allows the hammer to render "through" other geometry
        // so the smiting grid pattern on the anvil is visible through the hammer
        _disableDepth ??= AccessTools.Method(render.GetType(), "GLDisableDepthTest");
        _enableDepth ??= AccessTools.Method(render.GetType(), "GLEnableDepthTest");
        _disableDepth?.Invoke(render, null);
        render.GLDepthMask(false);

        // === BASE CODE: setup and run the standard shader ===
        var obj = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
        obj.ModelMatrix = modelMat.Values;
        obj.ViewMatrix = render.CameraMatrixOriginf;
        obj.ProjectionMatrix = render.CurrentProjectionMatrix;

        // === GHOST MOD: apply premultiplied alpha transparency ===
        // RgbaTint scales the mesh color, alpha controls transparency
        // PremultipliedAlpha blend mode gives a natural semi-transparent look
        float alpha = 0.2f;
        obj.RgbaTint = new Vec4f(1f * alpha, 1f * alpha, 1f * alpha, alpha);
        render.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);

        // === BASE CODE: render the mesh ===
        render.RenderMultiTextureMesh(meshref, "tex");
        ((IShaderProgram)obj).Stop();

        // === GHOST MOD: restore GL state ===
        render.GlToggleBlend(false);
        render.GLDepthMask(true);
        _enableDepth?.Invoke(render, null);

        // === BASE CODE: update swing angle + triggers anvil hit detection ===
        __instance.AngleRad = be.Angle;

        return false; // skip original method
    }
}
