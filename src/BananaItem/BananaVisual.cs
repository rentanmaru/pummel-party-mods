using System.Collections.Generic;
using UnityEngine;

namespace BananaItem
{
    // バナナの見た目（コードで作る簡易モデル）とアイコン
    internal static class BananaVisual
    {
        private static readonly Color BodyColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color TipColor = new Color(0.35f, 0.22f, 0.08f);

        private const int Segments = 14;   // 長さ方向の分割数
        private const int Sides = 10;      // 太さ方向の分割数
        private const float ArcRadius = 0.22f;
        private const float ArcHalfAngle = 1.0f; // ラジアン
        private const float MaxThickness = 0.055f;

        // 曲がった黄色い筒＋両端が茶色のモデルを作る
        public static GameObject CreateModel(string name)
        {
            var go = new GameObject(name);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            filter.sharedMesh = CreateMesh();
            renderer.sharedMaterials = new[] { CreateMaterial(BodyColor), CreateMaterial(TipColor) };
            return go;
        }

        // ボードのマスに置く用（ゆっくり回って上下する）
        public static GameObject CreatePlaced(Vector3 position)
        {
            GameObject go = Object.Instantiate(BananaDefinition.ModelTemplate);
            go.name = "PummelBananaPlaced";
            go.transform.position = position + Vector3.up * 0.25f;
            go.transform.localScale = Vector3.one * 1.8f;
            go.AddComponent<BananaSpinner>();
            return go;
        }

        private static Mesh CreateMesh()
        {
            var vertices = new List<Vector3>();
            var body = new List<int>();
            var tips = new List<int>();

            // 円弧に沿って輪を並べる（中心 (0, R, 0) の下側の弧＝にっこり形）
            for (int s = 0; s <= Segments; s++)
            {
                float t = (float)s / Segments;
                float angle = Mathf.Lerp(-ArcHalfAngle, ArcHalfAngle, t);
                Vector3 center = new Vector3(Mathf.Sin(angle) * ArcRadius, ArcRadius - Mathf.Cos(angle) * ArcRadius, 0f);
                Vector3 tangent = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Vector3 normal = Vector3.Cross(tangent, Vector3.forward).normalized;
                Vector3 binormal = Vector3.forward;

                // 真ん中が太く、両端が細い
                float thickness = Mathf.Max(0.008f, MaxThickness * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 0.6f));
                for (int k = 0; k < Sides; k++)
                {
                    float a = 2f * Mathf.PI * k / Sides;
                    vertices.Add(center + (normal * Mathf.Cos(a) + binormal * Mathf.Sin(a)) * thickness);
                }
            }

            // 輪と輪の間を三角形でつなぐ。両端の1区間だけ茶色
            for (int s = 0; s < Segments; s++)
            {
                List<int> target = (s == 0 || s == Segments - 1) ? tips : body;
                for (int k = 0; k < Sides; k++)
                {
                    int a = s * Sides + k;
                    int b = s * Sides + (k + 1) % Sides;
                    int c = (s + 1) * Sides + k;
                    int d = (s + 1) * Sides + (k + 1) % Sides;
                    target.AddRange(new[] { a, c, b, b, c, d });
                }
            }

            // 両端のふた
            AddCap(vertices, tips, 0, reverse: true);
            AddCap(vertices, tips, Segments * Sides, reverse: false);

            var mesh = new Mesh { name = "PummelBananaMesh" };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(body, 0);
            mesh.SetTriangles(tips, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddCap(List<Vector3> vertices, List<int> triangles, int ringStart, bool reverse)
        {
            Vector3 center = Vector3.zero;
            for (int k = 0; k < Sides; k++)
            {
                center += vertices[ringStart + k];
            }
            center /= Sides;
            int centerIndex = vertices.Count;
            vertices.Add(center);

            for (int k = 0; k < Sides; k++)
            {
                int a = ringStart + k;
                int b = ringStart + (k + 1) % Sides;
                if (reverse)
                {
                    triangles.AddRange(new[] { centerIndex, a, b });
                }
                else
                {
                    triangles.AddRange(new[] { centerIndex, b, a });
                }
            }
        }

        // ゲームに含まれているシェーダーを順に探す
        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.35f);
            }
            return material;
        }

        // 128x128 のアイコンを描く（透明な背景に、黒ふち付きの黄色い三日月）
        public static Texture2D CreateIcon()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PummelBananaIcon",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var outline = new Color(0.15f, 0.1f, 0.02f, 1f);
            var center = new Vector2(64f, 112f);  // 弧の中心（上側）
            const float radius = 70f;
            const float halfAngle = 1.05f;
            const float maxHalfWidth = 13f;

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float angle = Mathf.Atan2(p.x, -p.y); // 真下が0
                    float t = Mathf.InverseLerp(-halfAngle, halfAngle, angle);
                    Color color = Color.clear;

                    if (angle >= -halfAngle && angle <= halfAngle)
                    {
                        float halfWidth = Mathf.Max(2.5f, maxHalfWidth * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 0.6f));
                        float distance = Mathf.Abs(p.magnitude - radius);
                        if (distance <= halfWidth)
                        {
                            bool isTip = t < 0.08f || t > 0.92f;
                            color = isTip ? TipColor : BodyColor;
                        }
                        else if (distance <= halfWidth + 3f)
                        {
                            color = outline;
                        }
                    }
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }

    // マスに置いたバナナをゆっくり回して上下させる（見た目だけ）
    internal class BananaSpinner : MonoBehaviour
    {
        private Vector3 _basePosition;
        private float _phase;

        private void Start()
        {
            _basePosition = transform.position;
            _phase = Random.value * 10f;
        }

        private void Update()
        {
            transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
            transform.position = _basePosition + Vector3.up * (Mathf.Sin(Time.time * 2f + _phase) * 0.05f);
        }
    }
}
