﻿/////////////////////////////////////////////////////////
//MIT License
//Copyright (c) 2020 Steffen Vetne
/////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//The weights are normalized in runtime
//now I have to getcomponent both though
//find the barycentric coordinates? 
//has to be readable
//make it use list for uv instead?

//This gives a smooth control similar to Terrain, to MeshRenderers 

namespace PrecisionSurfaceEffects
{
    [System.Serializable]
    public class SubMaterial
    {
        public int materialID;
        [ReadOnly]
        public Material material;
    }

    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider))]
    [DisallowMultipleComponent]
    [AddComponentMenu("PSE/Surface Markers/Surface Blend-Map Marker", -1000)]
    public sealed class SurfaceBlendMapMarker : Marker
    {
        //Fields
#if UNITY_EDITOR
        [ReadOnly]
        [SerializeField]
        private List<Color> lastSampledColors = new List<Color>();
#endif

        [Space(30)]
        [SerializeField]
        public BlendMap[] blendMaps = new BlendMap[1] { new BlendMap() };

        private Vector3[] vertices;
        private int[] triangles;

        private static readonly List<Vector2> temporaryUVs = new List<Vector2>();



        //Methods
        private void Add(SurfaceData sd, SurfaceBlends.NormalizedBlends blendResults, Color tint, float weightMultiplier, ref float totalWeight)
        {
            if (weightMultiplier <= 0.0000000001f)
                return;

            for (int i = 0; i < blendResults.result.Count; i++)
            {
                var blend = blendResults.result[i];
                blend.color *= tint;
                sd.AddBlend(blend, false, weightMultiplier, ref totalWeight);
            }
        }

        internal bool TryAddBlends(SurfaceData sd, Mesh mesh, int submeshID, Vector3 point, int triangleID, out float totalWeight)
        {
            totalWeight = 0;

            //Finds Barycentric
            var triangle = triangleID * 3;
            var t0 = triangles[triangle + 0];
            var t1 = triangles[triangle + 1];
            var t2 = triangles[triangle + 2];
            var a = vertices[t0];
            var b = vertices[t1];
            var c = vertices[t2];
            point = transform.InverseTransformPoint(point);
            var bary = new Barycentric(a, b, c, point);

#if UNITY_EDITOR
            lastSampledColors.Clear();
#endif

            float totalTotalWeight = 0;
            for (int i = 0; i < blendMaps.Length; i++)
            {
                var bm = blendMaps[i];
                bm.sampled = false;

                for (int ii = 0; ii < bm.subMaterials.Length; ii++)
                {
                    if (bm.subMaterials[ii].materialID == submeshID)
                    {
                        var uv = bary.Interpolate(bm.uvs[t0], bm.uvs[t1], bm.uvs[t2]);
                        uv = uv * new Vector2(bm.uvScaleOffset.x, bm.uvScaleOffset.y) + new Vector2(bm.uvScaleOffset.z, bm.uvScaleOffset.w); //?

                        Color color = bm.map.GetPixelBilinear(uv.x, uv.y); //this only works for clamp or repeat btw (not mirror etc.)
                        bm.sampledColor = color;

                        void SampleColor(BlendMap.SurfaceBlends2 sb2)
                        {
                            if (sb2.colorMap != null)
                                sb2.sampledColor = sb2.colorMap.GetPixelBilinear(uv.x, uv.y);
                            else
                                sb2.sampledColor = Color.white;
                        }
                        SampleColor(bm.r);
                        SampleColor(bm.g);
                        SampleColor(bm.b);
                        SampleColor(bm.a);

                        totalTotalWeight += bm.weight * (color.r + color.g + color.b + color.a);

#if UNITY_EDITOR
                        lastSampledColors.Add(color);
#endif

                        bm.sampled = true;
                        break;
                    }
                }
            }

            if (totalTotalWeight > 0)
            {
                float invTotalTotal = 1f / totalTotalWeight;

                for (int i = 0; i < blendMaps.Length; i++)
                {
                    var bm = blendMaps[i];

                    if (bm.sampled)
                    {
                        float invTotal = bm.weight * invTotalTotal;

                        var color = bm.sampledColor;
                        Add(sd, bm.r.result, bm.r.sampledColor, color.r * invTotal, ref totalWeight);
                        Add(sd, bm.g.result, bm.g.sampledColor, color.g * invTotal, ref totalWeight);
                        Add(sd, bm.b.result, bm.b.sampledColor, color.b * invTotal, ref totalWeight);
                        Add(sd, bm.a.result, bm.a.sampledColor, color.a * invTotal, ref totalWeight);
                    }
                }

                return true;
            }

            return false;
        }

        public override void Refresh()
        {
            base.Refresh();

            for (int i = 0; i < blendMaps.Length; i++)
            {
                var bm = blendMaps[i];
                bm.r.SortNormalize();
                bm.g.SortNormalize();
                bm.b.SortNormalize();
                bm.a.SortNormalize();
            }
        }

        

        //Datatypes
        [System.Serializable]
        public class BlendMap
        {
            //Fields
            public float weight = 1;

            [Header("Materials")]
            [SerializeField]
            public SubMaterial[] subMaterials = new SubMaterial[1] { new SubMaterial() };

            [Header("Texture")]
            public Texture2D map; //must be readable
            [Range(0, 7)]
            public int uvChannel = 0;
            public Vector4 uvScaleOffset = new Vector4(1, 1, 0, 0); //st

            [Header("Channel Blends")]
            public SurfaceBlends2 r = new SurfaceBlends2();
            public SurfaceBlends2 g = new SurfaceBlends2();
            public SurfaceBlends2 b = new SurfaceBlends2();
            public SurfaceBlends2 a = new SurfaceBlends2();

            internal Vector2[] uvs;

            internal bool sampled;
            internal Color sampledColor;

            [System.Serializable]
            public class SurfaceBlends2 : SurfaceBlends
            {
                [Header("Optional")]
                public Texture2D colorMap;

                internal Color sampledColor;
            }
        }



        //Lifecycle
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            lastSampledColors.Clear();

            Refresh();

            Awake();
        }
#endif

        protected override void Awake()
        {
            base.Awake();

            var mf = GetComponent<MeshFilter>();
            var m = mf.sharedMesh;

            vertices = m.vertices;
            triangles = m.triangles;
            
            for (int i = 0; i < blendMaps.Length; i++)
			{
                var bm = blendMaps[i];
                temporaryUVs.Clear();
                m.GetUVs(bm.uvChannel, temporaryUVs);
                bm.uvs = temporaryUVs.ToArray();
            }

            var mr = GetComponent<MeshRenderer>();
            var mats = mr.sharedMaterials;

            for (int i = 0; i < blendMaps.Length; i++)
            {
                var bm = blendMaps[i];

                for (int ii = 0; ii < bm.subMaterials.Length; ii++)
                {
                    var sm = bm.subMaterials[ii];

                    sm.materialID = Mathf.Clamp(sm.materialID, 0, mats.Length - 1);
                    sm.material = mats[sm.materialID];
                }
            }
        }
    }
}

/*
 * 

            //var mf = GetComponent<MeshFilter>();
            //var m = mf.sharedMesh;

            //for (int i = 0; i < blendMaps.Length; i++)
            //{
            //    var bm = blendMaps[i];
            //    bm.uvChannel = Mathf.Min(bm.uvChannel, m.)
            //}

*/