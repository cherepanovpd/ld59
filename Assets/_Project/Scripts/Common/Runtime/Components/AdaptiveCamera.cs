using UnityEngine;

namespace Common.Runtime.Components
{
    [RequireComponent(typeof(Camera))]
    public class AdaptiveCamera : MonoBehaviour
    {
        public enum FitMode
        {
            FixedWidth,  // Фиксированная ширина, высота подстраивается
            FixedHeight, // Фиксированная высота, ширина подстраивается
            FitInside,   // Вся сцена помещается внутрь (без обрезания)
            FitOutside   // Сцена заполняет экран (возможно обрезание)
        }

        [Header("Режим адаптации")]
        public FitMode fitMode = FitMode.FixedWidth;
    
        [Header("Параметры")]
        public float fixedWidth = 20f;  // Желаемая ширина в мировых единицах
        public float fixedHeight = 10f; // Желаемая высота в мировых единицах
        public Bounds sceneBounds;      // Границы сцены для FitInside/FitOutside
    
        [Header("Настройки")]
        public bool updateInEditor = true;    // Обновлять в редакторе
        public bool smoothTransition = false; // Плавный переход
        public float transitionSpeed = 5f;    // Скорость плавного перехода
    
        private Camera cam;
        private float targetOrthographicSize;
        private float currentOrthographicSize;
        private int lastScreenWidth;
        private int lastScreenHeight;
    
        // Событие при изменении размера камеры
        public System.Action<Camera> onCameraResized;
    
        void Awake()
        {
            cam = GetComponent<Camera>();
            currentOrthographicSize = cam.orthographicSize;
            targetOrthographicSize = currentOrthographicSize;
        }
    
        void Start()
        {
            UpdateCameraSize();
        }
    
        void Update()
        {
            // Проверяем изменение разрешения
            if (cam.pixelWidth != lastScreenWidth || cam.pixelHeight != lastScreenHeight)
            {
                UpdateCameraSize();
                lastScreenWidth = cam.pixelWidth;
                lastScreenHeight = cam.pixelHeight;
            }
        
            // Плавный переход
            if (smoothTransition && Mathf.Abs(cam.orthographicSize - targetOrthographicSize) > 0.01f)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthographicSize, Time.deltaTime * transitionSpeed);
            }
        }
    
        [ContextMenu("Обновить размер камеры")]
        public void UpdateCameraSize()
        {
            if (cam == null) cam = GetComponent<Camera>();
        
            float aspect = (float) cam.pixelWidth /  cam.pixelHeight;
        
            switch (fitMode)
            {
                case FitMode.FixedWidth:
                    targetOrthographicSize = (fixedWidth / 2f) / aspect;
                    break;
                
                case FitMode.FixedHeight:
                    targetOrthographicSize = (fixedHeight / 2f) / aspect;
                    break;
                
                case FitMode.FitInside:
                    if (sceneBounds.size == Vector3.zero)
                    {
                        Debug.LogWarning("Scene bounds not set! Using FixedWidth mode.");
                        targetOrthographicSize = (fixedWidth / 2f) / aspect;
                    }
                    else
                    {
                        float sizeByHeight = sceneBounds.size.y / 2f;
                        float sizeByWidth = (sceneBounds.size.x / 2f) / aspect;
                        targetOrthographicSize = Mathf.Min(sizeByHeight, sizeByWidth);
                    }
                    break;
                
                case FitMode.FitOutside:
                    if (sceneBounds.size == Vector3.zero)
                    {
                        Debug.LogWarning("Scene bounds not set! Using FixedWidth mode.");
                        targetOrthographicSize = (fixedWidth / 2f) / aspect;
                    }
                    else
                    {
                        float sizeByHeight = sceneBounds.size.y / 2f;
                        float sizeByWidth = (sceneBounds.size.x / 2f) / aspect;
                        targetOrthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
                    }
                    break;
            }
        
            if (!smoothTransition)
            {
                cam.orthographicSize = targetOrthographicSize;
                currentOrthographicSize = targetOrthographicSize;
            }
        
            onCameraResized?.Invoke(cam);
        }
    
        // Вспомогательные методы для работы в редакторе
        void OnValidate()
        {
            if (updateInEditor && Application.isEditor && !Application.isPlaying)
            {
                if (cam == null) cam = GetComponent<Camera>();
                UpdateCameraSize();
            }
        }
    
        // Метод для автоматического определения границ сцены
        [ContextMenu("Auto Detect Scene Bounds")]
        public void AutoDetectSceneBounds()
        {
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("No renderers found in scene!");
                return;
            }
        
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
        
            sceneBounds = bounds;
            Debug.Log($"Scene bounds detected: center={bounds.center}, size={bounds.size}");
            UpdateCameraSize();
        }
    
        // Метод для получения текущих границ видимости камеры
        public void GetCameraBounds(out float left, out float right, out float bottom, out float top)
        {
            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
        
            Vector3 center = cam.transform.position;
            left = center.x - width / 2f;
            right = center.x + width / 2f;
            bottom = center.y - height / 2f;
            top = center.y + height / 2f;
        }
    
        // Визуализация границ в редакторе
        void OnDrawGizmosSelected()
        {
            if (cam == null) cam = GetComponent<Camera>();
        
            // Рисуем границы камеры
            Gizmos.color = Color.green;
            GetCameraBounds(out float left, out float right, out float bottom, out float top);
        
            Vector3 center = new Vector3((left + right) / 2f, (bottom + top) / 2f, 0);
            Vector3 size = new Vector3(right - left, top - bottom, 0);
            Gizmos.DrawWireCube(center, size);
        
            // Рисуем границы сцены (если заданы)
            if (sceneBounds.size != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(sceneBounds.center, sceneBounds.size);
            }
        }
    }
}