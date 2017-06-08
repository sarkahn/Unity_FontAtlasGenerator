using UnityEngine;
using UnityEditor;
using FontAtlasGen.Util;
using System.Collections.Generic;

namespace FontAtlasGen.FontAtlasGenEditor
{
    /// <summary>
    /// EditorWindow for exporting a Font Atlas from a set of characters.
    /// </summary>
    public class FontAtlasGeneratorEditorWindow : EditorWindow
    {
        [SerializeField]
        Font font_;

        [SerializeField]
        Material mat_;
        Material Material_
        {
            get
            {
                if (mat_ == null)
                {
                    mat_ = new Material(Shader.Find("GUI/Text Shader"));
                    mat_.hideFlags = HideFlags.HideAndDontSave;
                }
                return mat_;
            }
        }

        Texture2D grabTexture_ = null;
        RenderTexture renderTexture_ = null;

        [SerializeField]
        int fontSize_ = 8;

        [SerializeField]
        string glyphString_ = "a";

        [SerializeField]
        string customString_ = "Custom";

        enum SelectedGlyphs
        {
            Custom = 0,
            Code_Page_437 = 1,
        };

        [SerializeField]
        SelectedGlyphs selectedGlyphs_ = SelectedGlyphs.Code_Page_437;

        [SerializeField]
        IntVector2 glyphDimensions_ = new IntVector2(8, 8);

        HashSet<char> toRemove_ = new HashSet<char>();

        /// <summary>
        /// Vertical offset applied to each glyph on the tilesheet. The proper value seems
        /// to vary from Font to Font, and as far as I can see the only way to get the right
        /// value is to tweak it manually.
        /// </summary>
        [SerializeField]
        int verticalOffset_ = 1;

        /// <summary>
        /// The maximum number of glyphs per row.
        /// </summary>
        [SerializeField]
        int columnCount_ = 16;

        /// <summary>
        /// Whether or not to draw a cell grid over the preview image. Can help when sizing/positioning glyphs.
        /// </summary>
        [SerializeField]
        bool drawPreviewGrid_ = false;

        [SerializeField]
        Color previewGridColor_ = new Color(50f / 255f, 205f / 255f, 150f / 255f, .35f);

        [SerializeField]
        Color backgroundColor_ = Color.black;

        [SerializeField]
        Color textColor_ = Color.white;

        enum UnsupportedGlyphHandling
        {
            // Do nothing, let unity handle it
            Fallback,
            // Replace any unsupported characters with an empty glyph
            Empty,
            // Remove unsupported characters from the input string
            Remove
        }

        [SerializeField]
        UnsupportedGlyphHandling unsupportedGlyphHandling_ = UnsupportedGlyphHandling.Empty;

        FontAtlasMesh mesh_ = new FontAtlasMesh();

        [MenuItem("Window/FontAtlasGenerator", false, 500)]
        static void MakeWindow()
        {
            GetWindow<FontAtlasGeneratorEditorWindow>().Show();
        }

        void OnDisable()
        {
            if (renderTexture_ != null)
                renderTexture_.Release();
        }

        void OnGUI()
        {
            bool updateTex = false;

            EditorGUI.BeginChangeCheck();

            DrawFontGUI();

            if (font_ == null)
                return;

            DrawGlyphGUI();

            updateTex = EditorGUI.EndChangeCheck();

            DrawPreviewGUI(updateTex);
        }

        void RebuildTexture(IntVector2 totalPixels)
        {
            toRemove_.Clear();

            Debug.Log("Before string op " + glyphString_);

            switch( unsupportedGlyphHandling_ )
            {
                case UnsupportedGlyphHandling.Empty:
                {
                    foreach( char ch in glyphString_ )
                    {
                        if (!font_.HasCharacter(ch))
                        {
                            //Debug.LogFormat("Char {0} unsupported", ch);
                            toRemove_.Add(ch);
                        }
                    }
                    foreach (char ch in toRemove_)
                    {
                        glyphString_ = glyphString_.Replace(ch, ' ');
                    }
                }
                break;

                case UnsupportedGlyphHandling.Remove:
                {
                    foreach (char ch in glyphString_)
                    {
                        if (!font_.HasCharacter(ch))
                        {
                            //Debug.LogFormat("Char {0} unsupported", ch);
                            toRemove_.Add(ch);
                        }
                    }
                    glyphString_ = CleanString(glyphString_, toRemove_);
                }
                break;
            }

            Debug.Log("After string op" + glyphString_);
            

            // Set up our font
            font_.RequestCharactersInTexture(glyphString_, fontSize_);
            font_.material.mainTexture.filterMode = FilterMode.Point;

            // Set up our material
            var mat = Material_;
            mat.mainTexture = font_.material.mainTexture;

            // Build the mesh
            mesh_.AddCharactersToMesh(glyphString_, font_, fontSize_, 16, glyphDimensions_, verticalOffset_);

            // Ensure our grab texture is the proper size
            if (grabTexture_ == null || grabTexture_.width != totalPixels.x || grabTexture_.height != totalPixels.y)
            {
                grabTexture_ = new Texture2D(totalPixels.x, totalPixels.y);
                grabTexture_.filterMode = FilterMode.Point;
            }

            // Recreate our render texture if needed
            if (renderTexture_ == null || !renderTexture_.IsCreated() ||
                renderTexture_.width != totalPixels.x ||
                renderTexture_.height != totalPixels.y)
            {
                if (renderTexture_ != null)
                {
                    renderTexture_.Release();
                    renderTexture_ = null;
                }

                renderTexture_ = new RenderTexture(totalPixels.x, totalPixels.y, 0);
                renderTexture_.filterMode = FilterMode.Point;
                renderTexture_.Create();
                renderTexture_.hideFlags = HideFlags.HideAndDontSave;
            }

            // Render our mesh into our render texture.
            var lastRT = RenderTexture.active;

            RenderTexture.active = renderTexture_;

            GL.PushMatrix();
            GL.Clear(true, true, backgroundColor_);

            mat.color = textColor_;

            mat.SetPass(0);
            GL.LoadPixelMatrix(0,
                totalPixels.x, 0,
                totalPixels.y);

            Graphics.DrawMeshNow(mesh_.Mesh_, Vector3.zero, Quaternion.identity);

            // Grab the our mesh from the render texture.
            grabTexture_.ReadPixels(new Rect(0, 0, totalPixels.x, totalPixels.y), 0, 0);
            grabTexture_.Apply(false);

            GL.PopMatrix();

            RenderTexture.active = lastRT;
        }

        /// <summary>
        /// Draw the gui for the font.
        /// </summary>
        void DrawFontGUI()
        {
            EditorGUILayout.LabelField("Font", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                font_ = (Font)EditorGUILayout.ObjectField(font_, typeof(Font), false);


                if (font_ != null)
                {
                    fontSize_ = EditorGUILayout.IntField("Font Size", fontSize_);
                }
            }
            EditorGUI.indentLevel--;
        }

        void DrawGlyphGUI()
        {
            EditorGUILayout.LabelField("Glyphs", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                selectedGlyphs_ = (SelectedGlyphs)EditorGUILayout.EnumPopup("Characters", selectedGlyphs_);

                switch (selectedGlyphs_)
                {
                    case SelectedGlyphs.Custom:
                    EditorGUI.indentLevel++;
                    {
                        customString_ = EditorGUILayout.TextField("Custom Characters", customString_);
                        glyphString_ = customString_;
                    }
                    EditorGUI.indentLevel--;
                    break;

                    case SelectedGlyphs.Code_Page_437:
                    glyphString_ = CODE_PAGE_437_STR_;
                    break;
                }

                glyphDimensions_.x = EditorGUILayout.IntField("Glyph Width", glyphDimensions_.x);
                glyphDimensions_.y = EditorGUILayout.IntField("Glyph Height", glyphDimensions_.y);

                unsupportedGlyphHandling_ = (UnsupportedGlyphHandling)EditorGUILayout.EnumPopup(
                    "Unsupported Glyph Handling", unsupportedGlyphHandling_);
                

            }
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Atlas", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                columnCount_ = EditorGUILayout.IntField("Column Count", columnCount_);

                string tooltip =
                    "Vertical offset applied to each glyph on the tilesheet. May need slight tweaking from font to font.";
                var vertOffsetContent = new GUIContent(
                    "Vertical Offset", tooltip);
                verticalOffset_ = EditorGUILayout.IntField(vertOffsetContent, verticalOffset_);
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Draw the preview of our tile sheet.
        /// </summary>
        /// <param name="updateTex">Whether or not we need to rebuild our texture.</param>
        void DrawPreviewGUI(bool updateTex)
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            {

                EditorGUI.BeginChangeCheck();

                backgroundColor_ = EditorGUILayout.ColorField("Background Color", backgroundColor_);

                textColor_ = EditorGUILayout.ColorField("Text Color", textColor_);

                updateTex |= EditorGUI.EndChangeCheck();

                EditorGUILayout.BeginHorizontal();
                {
                    drawPreviewGrid_ = EditorGUILayout.Toggle("Draw Preview Grid", drawPreviewGrid_);
                    if (drawPreviewGrid_)
                    {
                        previewGridColor_ = EditorGUILayout.ColorField(previewGridColor_);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;

            // Get the total pixel size of our tile sheet based on our grid and glyph settings
            int horGlyphCount = Mathf.Min(glyphString_.Length, columnCount_);
            int vertGlyphCount = Mathf.CeilToInt((float)glyphString_.Length / horGlyphCount);

            IntVector2 totalPixels = new IntVector2(
                glyphDimensions_.x * horGlyphCount,
                glyphDimensions_.y * vertGlyphCount);

            if (font_ == null || string.IsNullOrEmpty(glyphString_) ||
                totalPixels.x <= 0 || totalPixels.y <= 0)
                return;

            // Calculate the maximum size for our preview image
            var area = GUILayoutUtility.GetRect(totalPixels.x, totalPixels.y, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Get our max scale that will maintain our aspect ratio
            var expandedHorScale = Mathf.Floor(area.width / totalPixels.x);
            var exandedVertScale = Mathf.Floor(area.height / totalPixels.y);
            var scale = Mathf.Min(expandedHorScale, exandedVertScale);

            var totalArea = area.size;

            // Determine our new scaled size
            var scaledSize = area.size;
            scaledSize.x = totalPixels.x * scale;
            scaledSize.y = totalPixels.y * scale;
            area.size = scaledSize;

            // Center our preview image
            var remaining = totalArea - scaledSize;
            area.position += remaining / 2f;

            // Rebuild our texture if any settings have changed
            if (updateTex)
            {
                RebuildTexture(totalPixels);
            }

            if (grabTexture_ != null)
            {
                GUI.DrawTexture(area, grabTexture_);
            }



            if (drawPreviewGrid_)
            {
                var transparent = new Color(1, 1, 1, 0);
                for (int x = 0; x < horGlyphCount; ++x)
                {
                    for (int y = 0; y < vertGlyphCount; ++y)
                    {
                        float t = (x + y) * .5f;
                        t = (t - Mathf.Floor(t)) * 2f;

                        var col = Color.Lerp(transparent, previewGridColor_, t);

                        var size = glyphDimensions_ * scale;

                        var pos = new Vector2(x * size.x, y * size.y) + area.position;

                        var cellArea = new Rect(pos, size);

                        EditorGUI.DrawRect(cellArea, col);
                    }
                }
            }

            DrawWriteToDiskButton();
        }

        void DrawWriteToDiskButton()
        {
            var oldColor = GUI.color;
            GUI.color = Color.green;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Write To Disk", GUILayout.Width(100), GUILayout.Height(25)))
            {
                var fileName = font_.name + " Atlas";
                var path = EditorUtility.SaveFilePanelInProject("Save Texture", fileName, "png", "Save Font Atlas");

                if (!string.IsNullOrEmpty(path))
                {
                    var bytes = grabTexture_.EncodeToPNG();

                    if (bytes != null)
                    {
                        System.IO.File.WriteAllBytes(path, bytes);
                        AssetDatabase.Refresh();
                    }
                    else
                        Debug.LogErrorFormat("Error writing texture to disk");
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUI.color = oldColor;
        }

        const string CODE_PAGE_437_STR_ =
        @" ☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼ !""#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_`abcdefghijklmnopqrstuvwxyz{|}~⌂ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■ ";


        public static string CleanString(string str, HashSet<char> toRemove )
        {
            var result = new System.Text.StringBuilder(str.Length);

            for (int i = 0; i < str.Length; i++)
            {
                if (!toRemove.Contains(str[i]))
                    result.Append(str[i]);
            }
            return result.ToString();
        }
    }

}
