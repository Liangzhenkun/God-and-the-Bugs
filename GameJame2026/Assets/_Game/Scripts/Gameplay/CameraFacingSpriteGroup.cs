using System.Collections.Generic;
using UnityEngine;

namespace GameJamRAC.Gameplay
{
    /// <summary>批量让一个父节点下的所有精灵始终面向当前主相机。</summary>
    [DisallowMultipleComponent]
    public class CameraFacingSpriteGroup : MonoBehaviour
    {
        [SerializeField] private bool includeInactive = true;
        [SerializeField, HideInInspector] private Transform[] visualRoots;

        private void Awake()
        {
            CameraFacingSprite singleSpriteFacing = GetComponent<CameraFacingSprite>();
            if (singleSpriteFacing != null) singleSpriteFacing.enabled = false;
            RefreshVisualRoots();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) RefreshVisualRoots();
        }

        private void LateUpdate()
        {
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (camera == null || visualRoots == null) return;

            foreach (Transform visualRoot in visualRoots)
            {
                if (visualRoot == null) continue;
                Vector3 directionToCamera = camera.transform.position - visualRoot.position;
                if (directionToCamera.sqrMagnitude < 0.0001f) continue;
                visualRoot.rotation = Quaternion.LookRotation(directionToCamera, camera.transform.up);
            }
        }

        [ContextMenu("刷新全部子精灵")]
        public void RefreshVisualRoots()
        {
            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(includeInactive);
            List<Transform> roots = new List<Transform>();
            foreach (SpriteRenderer sprite in sprites)
            {
                if (sprite == null || sprite.transform == transform || roots.Contains(sprite.transform)) continue;
                roots.Add(sprite.transform);
            }

            visualRoots = roots.ToArray();
        }
    }
}
