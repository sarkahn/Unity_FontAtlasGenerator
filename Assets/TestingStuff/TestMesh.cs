using FontAtlasGen.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FontAtlasGen.TestingStuff
{
    /// <summary>
    /// Used to test the <seealso cref="FontAtlasMesh"/> in a scene.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class TestMesh : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        MeshFilter filter_;

        [SerializeField, HideInInspector]
        MeshRenderer renderer_;

        FontAtlasMesh mesh_ = new FontAtlasMesh();

        [SerializeField]
        string str_;

        [SerializeField]
        Font font_;

        [SerializeField]
        IntVector2 glyphSize_ = new IntVector2(5, 5);

        [SerializeField]
        int fontSize_ = 16;

        [SerializeField]
        int verticalAdjust_ = 0;

        bool doRebuild_ = false;

        [SerializeField]
        bool drawGizmoGrid_ = false;


        private void OnEnable()
        {
            Font.textureRebuilt += OnTextureRebuilt;

        }

        private void OnDisable()
        {
            Font.textureRebuilt -= OnTextureRebuilt;
        }

        void OnTextureRebuilt(Font font)
        {
            if (font != font_)
                return;

            doRebuild_ = true;
        }

        void RebuildMesh()
        {
            if (string.IsNullOrEmpty(str_) || font_ == null)
            {
                Debug.Log("NullStr");
                return;
            }

            int columns = 16;

            mesh_.AddCharactersToMesh(str_, font_, fontSize_, columns, glyphSize_, verticalAdjust_);

            filter_.sharedMesh = mesh_.Mesh_;

            if (renderer_.sharedMaterial != null)
                renderer_.sharedMaterial.mainTexture = font_.material.mainTexture;
        }

        private void Awake()
        {
            filter_ = GetComponent<MeshFilter>();
            renderer_ = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            doRebuild_ = true;
        }

        private void Update()
        {
            font_.RequestCharactersInTexture(str_, fontSize_);

            if (doRebuild_)
            {
                doRebuild_ = false;
                RebuildMesh();
            }
        }

        private void OnValidate()
        {
            glyphSize_.x = Mathf.Max(glyphSize_.x, 4);
            glyphSize_.y = Mathf.Max(glyphSize_.y, 4);

            font_.material.mainTexture.filterMode = FilterMode.Point;
            

            doRebuild_ = true;
        }

        // Draw gizmo grid over cells.
        private void OnDrawGizmos()
        {
            if (!drawGizmoGrid_)
                return;

            var oldColor = Gizmos.color;

            Color evenColor = new Color(0, 0, 0, 0);
            Color oddColor = new Color(0f, 0f, 0f, .45f);

            for (int x = 0; x < 16; ++x)
            {
                for (int y = 0; y < 16; ++y)
                {
                    float gridPos = x + y;
                    gridPos *= .5f;
                    gridPos = gridPos - Mathf.Floor(gridPos);
                    gridPos *= 2;

                    var col = Color.Lerp(evenColor, oddColor, gridPos);

                    var size = new Vector3(glyphSize_.x, glyphSize_.y);
                    var pos = new Vector3(x * glyphSize_.x, y * glyphSize_.y);
                    pos += size * .5f;

                    Gizmos.color = col;
                    Gizmos.DrawCube(pos, size);

                    //Gizmos.DrawCube()
                }
            }
            Gizmos.color = oldColor;
        }

    }

}