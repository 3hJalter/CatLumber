using UnityEngine;

namespace _Game.Camera
{
    public enum ECameraType
    {
        None = -1,
        MainMenuCamera = 0,
        InGameCamera = 1,
        WorldMapCamera = 2,
        ShopCamera = 3,
        CutsceneCamera = 4,
        ZoomOutCamera = 5,
    }

    public static class CUtilities
    {
        public const string DEFAULT_LAYER = "Default";
        public const string TRANSPARENT_FX_LAYER = "TransparentFX";
        public const string IGNORE_RAYCAST_LAYER = "Ignore Raycast";
        public const string WATER_LAYER = "Water";
        public const string UI_LAYER = "UI";

        public static void SetCullingMask(UnityEngine.Camera camera, string layerName, bool isOn)
        {
            if (isOn) AddCullingMask(camera, layerName);
            else RemoveCullingMask(camera, layerName);
        }

        public static void SetCullingMaskEverything(UnityEngine.Camera camera)
        {
            camera.cullingMask = -1;
        }

        public static void SetCullingMaskNothing(UnityEngine.Camera camera)
        {
            camera.cullingMask = 0;
        }

        private static void AddCullingMask(UnityEngine.Camera camera, string layerName)
        {
            camera.cullingMask |= 1 << LayerMask.NameToLayer(layerName);
        }

        private static void RemoveCullingMask(UnityEngine.Camera camera, string layerName)
        {
            camera.cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));
        }

        // // cullingMask everything
        // mainCamera.cullingMask = -1;
        // // cullingMask nothing
        // mainCamera.cullingMask = 0;
        // mainCamera.cullingMask = 1 << LayerMask.NameToLayer("Default");
        // // cullingMask everything except Ignore Raycast
        // mainCamera.cullingMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        // // culling Ignore Raycast and Water
        // mainCamera.cullingMask = (1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("Water"));
        // // culling everything except Ignore Raycast and Water
        // mainCamera.cullingMask = ~((1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("Water")));
        // // remove Ignore Raycast from culling
        // mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        // // add Ignore Raycast to culling
        // mainCamera.cullingMask |= (1 << LayerMask.NameToLayer("Ignore Raycast"));
    }
}
