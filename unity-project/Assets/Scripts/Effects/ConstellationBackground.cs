using UnityEngine;
using System.Collections.Generic;

namespace PumpNumber.Effects
{
    /// <summary>
    /// 별자리 배경 — LineRenderer로 별자리 선 + 별 점 표시
    ///
    /// [Unity 에디터 설정법]
    /// 1. 빈 GameObject 생성 → "ConstellationBackground" 이름
    /// 2. 이 스크립트 부착
    /// 3. starDotPrefab: 작은 원형 SpriteRenderer (2x2, white, Additive)
    /// 4. lineRendererPrefab: LineRenderer (width: 0.5~1px, Material: Additive)
    /// 5. 실행하면 자동으로 8개 별자리 + 별 점이 생성됨
    ///
    /// [별자리 데이터]
    /// 정규화 좌표(0~1)로 저장, RectTransform 크기에 맞춰 실제 위치 계산
    /// </summary>
    public class ConstellationBackground : MonoBehaviour
    {
        [Header("=== 프리팹 ===")]
        [SerializeField] private GameObject starDotPrefab;       // 작은 원 스프라이트
        [SerializeField] private LineRenderer lineRendererPrefab; // 라인 프리팹

        [Header("=== 설정 ===")]
        [SerializeField] private RectTransform backgroundRect;   // 배경 영역
        [SerializeField] private Color lineColor = new Color(0.4f, 0.63f, 1f, 0.2f);
        [SerializeField] private Color starDotColor = new Color(0.7f, 0.82f, 1f, 0.5f);
        [SerializeField] private float lineWidth = 1f;
        [SerializeField] private float starDotScale = 3f;

        // 별자리 데이터 (정규화 좌표 0~1)
        private readonly float[][][] constellationData = new float[][][]
        {
            // 좌상단 별자리
            new float[][] {
                new float[]{0.08f, 0.92f}, new float[]{0.15f, 0.95f},
                new float[]{0.22f, 0.90f}, new float[]{0.18f, 0.82f}, new float[]{0.10f, 0.84f}
            },
            // 우상단
            new float[][] {
                new float[]{0.75f, 0.94f}, new float[]{0.82f, 0.97f},
                new float[]{0.88f, 0.92f}, new float[]{0.92f, 0.85f}, new float[]{0.85f, 0.82f}
            },
            // 좌측 중간
            new float[][] {
                new float[]{0.05f, 0.45f}, new float[]{0.12f, 0.50f},
                new float[]{0.18f, 0.42f}, new float[]{0.08f, 0.35f}
            },
            // 우측 중간
            new float[][] {
                new float[]{0.85f, 0.50f}, new float[]{0.90f, 0.55f},
                new float[]{0.95f, 0.45f}, new float[]{0.88f, 0.40f}
            },
            // 우하단
            new float[][] {
                new float[]{0.70f, 0.25f}, new float[]{0.78f, 0.30f},
                new float[]{0.85f, 0.22f}, new float[]{0.82f, 0.15f}, new float[]{0.72f, 0.12f}
            },
            // 좌하단
            new float[][] {
                new float[]{0.15f, 0.20f}, new float[]{0.22f, 0.25f},
                new float[]{0.28f, 0.18f}, new float[]{0.20f, 0.10f}
            },
            // 상단 중앙 작은 별자리
            new float[][] {
                new float[]{0.40f, 0.95f}, new float[]{0.48f, 0.98f}, new float[]{0.55f, 0.92f}
            },
            // 하단 중앙 작은 별자리
            new float[][] {
                new float[]{0.50f, 0.15f}, new float[]{0.58f, 0.18f}, new float[]{0.62f, 0.10f}
            },
        };

        private List<GameObject> spawnedObjects = new List<GameObject>();

        private void OnEnable()
        {
            GenerateConstellations();
        }

        private void OnDisable()
        {
            ClearConstellations();
        }

        /// <summary>
        /// 별자리 생성 — LineRenderer로 선, 작은 dot으로 별 표시
        /// </summary>
        public void GenerateConstellations()
        {
            ClearConstellations();

            if (backgroundRect == null) return;
            float w = backgroundRect.rect.width;
            float h = backgroundRect.rect.height;

            foreach (var constellation in constellationData)
            {
                // 1. LineRenderer로 별자리 선 그리기
                if (lineRendererPrefab != null)
                {
                    LineRenderer lr = Instantiate(lineRendererPrefab, transform);
                    lr.positionCount = constellation.Length;
                    lr.startWidth = lineWidth;
                    lr.endWidth = lineWidth;
                    lr.startColor = lineColor;
                    lr.endColor = lineColor;
                    lr.useWorldSpace = false;

                    for (int i = 0; i < constellation.Length; i++)
                    {
                        float x = (constellation[i][0] - 0.5f) * w;
                        float y = (constellation[i][1] - 0.5f) * h;
                        lr.SetPosition(i, new Vector3(x, y, 0));
                    }
                    spawnedObjects.Add(lr.gameObject);
                }

                // 2. 각 별 위치에 작은 dot 스프라이트 배치
                if (starDotPrefab != null)
                {
                    foreach (var star in constellation)
                    {
                        float x = (star[0] - 0.5f) * w;
                        float y = (star[1] - 0.5f) * h;

                        GameObject dot = Instantiate(starDotPrefab, transform);
                        dot.transform.localPosition = new Vector3(x, y, 0);
                        dot.transform.localScale = Vector3.one * starDotScale;

                        var sr = dot.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = starDotColor;

                        spawnedObjects.Add(dot);
                    }
                }
            }
        }

        public void ClearConstellations()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            spawnedObjects.Clear();
        }
    }
}
