using Common.Runtime.Components;

using Core;

using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera), typeof(ScreenShake))]
public class CameraManager : MonoBehaviour
{
    private Camera _camera;
    private ScreenShake _screenShake { get; set; }
    
    
    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _screenShake = GetComponent<ScreenShake>();
        
        // Ensure Physics2DRaycaster is present for OnMouseEnter events
        EnsureRaycaster();
        
        // Self-registration with G service locator
        if (G.Camera != null && G.Camera != this)
        {
            Debug.LogWarning("[Camera] Multiple CameraManager instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        G.Camera = this;
        G.EnsureSystem("Camera", G.Camera);
    }
    

    private void EnsureRaycaster()
    {
        if (_camera == null)
            return;

        var raycaster = _camera.GetComponent<Physics2DRaycaster>();
        if (raycaster == null)
        {
            Debug.LogWarning($"[CameraManager] Physics2DRaycaster missing on camera '{_camera.name}'. Adding one automatically.");
            _camera.gameObject.AddComponent<Physics2DRaycaster>();
        }
    }

    private void OnDestroy()
    {
        // Unregister from G
        if (G.Camera == this)
            G.Camera = null;
    }
    
    public void UpdateScreenShakeSetting(float newSetting) => _screenShake.UpdateScreenShakeSetting(newSetting);
}