using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using VoogleRoute;

namespace VoogleRoute.Phone
{
    /// <summary>
    /// Top-down vanilla city-map camera rendered into the phone screen, yawed so the
    /// player heading is up. Map LOD is forced only while this camera renders.
    /// </summary>
    internal static class PhoneGpsVanillaCamera
    {
        private const string RootName = "VoogleRoute_PhoneMapCam";
        private const int TextureHeight = 768;

        private static Camera _camera;
        private static RenderTexture _texture;
        private static RawImage _target;
        private static LODGroup[] _lodGroups;
        private static bool _subscribed;
        private static bool _lodForcedThisCamera;

        internal static bool IsReady => _camera != null && _texture != null;

        internal static void Attach(RawImage target)
        {
            _target = target;
            EnsureCamera();
            if (_camera == null)
                return;

            ApplyCityMapMask();
            CacheLodGroups();
            Subscribe();
            _camera.enabled = true;
            BindTarget();
        }

        internal static void Tick(Vector3 lookAt, float headingDeg, float visibleMeters, RectTransform mapRect)
        {
            if (_camera == null)
                return;

            EnsureTextureMatches(mapRect);
            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Clamp(visibleMeters * 0.5f, 25f, 280f);
            _camera.transform.position = new Vector3(lookAt.x, lookAt.y + 180f, lookAt.z);
            _camera.transform.rotation = Quaternion.Euler(90f, headingDeg, 0f);
        }

        internal static bool TryLocalToWorld(RectTransform mapRect, Vector2 local, float groundY, out Vector3 world)
        {
            world = default;
            if (_camera == null || mapRect == null)
                return false;

            var size = mapRect.rect.size;
            if (size.x < 1f || size.y < 1f)
                return false;

            var viewport = new Vector3(
                (local.x / size.x) + 0.5f,
                (local.y / size.y) + 0.5f,
                _camera.nearClipPlane + 10f);
            var point = _camera.ViewportToWorldPoint(viewport);
            world = new Vector3(point.x, groundY, point.z);
            return true;
        }

        internal static void Release()
        {
            Unsubscribe();
            RestoreLod();
            _lodGroups = null;
            _target = null;

            if (_camera != null)
            {
                _camera.targetTexture = null;
                _camera.enabled = false;
                UnityEngine.Object.Destroy(_camera.gameObject);
                _camera = null;
            }

            ReleaseTexture();
        }

        private static void EnsureCamera()
        {
            if (_camera != null)
                return;

            var main = GameManager.GetMainCamera();
            if (main == null)
                return;

            var clone = UnityEngine.Object.Instantiate(main.gameObject);
            clone.name = RootName;
            UnityEngine.Object.DontDestroyOnLoad(clone);
            clone.tag = "Untagged";
            clone.transform.SetParent(null, true);

            foreach (var listener in clone.GetComponentsInChildren<AudioListener>(true))
                UnityEngine.Object.Destroy(listener);

            foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                    continue;
                var typeName = behaviour.GetType().Name;
                if (typeName == "HDAdditionalCameraData" || typeName.Contains("AdditionalCameraData"))
                    continue;
                behaviour.enabled = false;
            }

            _camera = clone.GetComponent<Camera>();
            if (_camera == null)
            {
                UnityEngine.Object.Destroy(clone);
                return;
            }

            foreach (var extra in clone.GetComponentsInChildren<Camera>(true))
            {
                if (extra != _camera)
                    extra.enabled = false;
            }

            _camera.depth = -80f;
            _camera.enabled = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.04f, 0.14f, 0.32f, 1f);
            _camera.nearClipPlane = 1f;
            _camera.farClipPlane = 400f;
            _camera.orthographic = true;
            _camera.useOcclusionCulling = false;
            try
            {
                _camera.allowMSAA = false;
            }
            catch
            {
                // HDRP cameras may reject MSAA writes.
            }

            ConfigureHdrp(clone);
        }

        private static void EnsureTextureMatches(RectTransform mapRect)
        {
            var width = TextureHeight;
            var height = TextureHeight;
            if (mapRect != null)
            {
                var size = mapRect.rect.size;
                if (size.x >= 8f && size.y >= 8f)
                {
                    height = TextureHeight;
                    width = Mathf.Clamp(
                        Mathf.RoundToInt(TextureHeight * (size.x / size.y) / 16f) * 16,
                        64,
                        1024);
                }
            }

            if (_texture != null
                && Mathf.Abs(_texture.width - width) < 16
                && Mathf.Abs(_texture.height - height) < 16)
                return;

            ReleaseTexture();
            _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = RootName + "_RT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _texture.Create();
            if (_camera != null)
                _camera.targetTexture = _texture;
            BindTarget();
        }

        private static void BindTarget()
        {
            if (_target != null)
                _target.texture = _texture;
        }

        private static void ReleaseTexture()
        {
            if (_texture == null)
                return;

            if (_camera != null)
                _camera.targetTexture = null;
            if (_target != null && ReferenceEquals(_target.texture, _texture))
                _target.texture = null;
            _texture.Release();
            UnityEngine.Object.Destroy(_texture);
            _texture = null;
        }

        private static void ApplyCityMapMask()
        {
            if (_camera == null)
                return;

            try
            {
                var map = CityManager.IsInitialized ? CityManager.Instance?.cityMap : null;
                if (map == null)
                    return;

                var mask = PlayerPrefSettings.LowDetailCityMap
                    ? map.cityMapMaskLowDetail.value
                    : map.cityMapMask.value;
                if (mask == 0)
                    mask = map.cityMapMask.value | map.cityMapMaskLowDetail.value;
                if (mask != 0)
                    _camera.cullingMask = mask;
            }
            catch (Exception ex)
            {
                ModLog.Info("[WARN] Phone map camera mask failed: " + ex.Message);
            }
        }

        private static void ConfigureHdrp(GameObject cameraObject)
        {
            try
            {
                var data = cameraObject.GetComponent("HDAdditionalCameraData");
                if (data == null)
                    return;

                var type = data.GetType();
                type.GetProperty("volumeLayerMask")?.SetValue(data, (LayerMask)(-1));
                var clearMode = type.GetNestedType("ClearColorMode")
                    ?? Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData+ClearColorMode");
                var colorMode = type.GetProperty("clearColorMode");
                if (colorMode != null && clearMode != null)
                    colorMode.SetValue(data, Enum.Parse(clearMode, "Color"));
                type.GetProperty("backgroundColorHDR")?.SetValue(data, new Color(0.04f, 0.14f, 0.32f, 1f));
                type.GetProperty("customRenderingSettings")?.SetValue(data, false);
            }
            catch (Exception ex)
            {
                ModLog.Info("[WARN] Phone map HDRP camera setup failed: " + ex.Message);
            }
        }

        private static void Subscribe()
        {
            if (_subscribed)
                return;

            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            RenderPipelineManager.endCameraRendering += OnEndCamera;
            _subscribed = true;
        }

        private static void Unsubscribe()
        {
            if (!_subscribed)
                return;

            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            RenderPipelineManager.endCameraRendering -= OnEndCamera;
            _subscribed = false;
        }

        private static void OnBeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera)
                return;
            ForceMapLod(true);
            _lodForcedThisCamera = true;
        }

        private static void OnEndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera || !_lodForcedThisCamera)
                return;
            _lodForcedThisCamera = false;
            ForceMapLod(false);
        }

        private static void CacheLodGroups()
        {
            var all = UnityEngine.Object.FindObjectsOfType<LODGroup>();
            var count = 0;
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].GetComponent<LODCityMapSwitcher>() != null)
                    count++;
            }

            _lodGroups = new LODGroup[count];
            var write = 0;
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].GetComponent<LODCityMapSwitcher>() != null)
                    _lodGroups[write++] = all[i];
            }
        }

        private static void ForceMapLod(bool enable)
        {
            if (_lodGroups == null)
                return;

            var lod = 0;
            if (enable || GameState.IsCityMapOpen())
                lod = 1;

            for (var i = 0; i < _lodGroups.Length; i++)
            {
                if (_lodGroups[i] != null)
                    _lodGroups[i].ForceLOD(lod);
            }
        }

        private static void RestoreLod()
        {
            ForceMapLod(false);
            _lodForcedThisCamera = false;
        }
    }
}
