﻿using System;
using System.Collections;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using UnityEngine;

namespace PathCreation.Examples {
    public class RoadMeshCreator : PathSceneTool
    {
        [Header("Configuration")] 
        public string holderIndex;
        [Range(-1.00f, 1.00f)] 
        public float yOffset;
        
        [Header ("Road Settings")]
        public float roadWidth = .4f;
        [Range (0, .5f)]
        public float thickness = .15f;
        public bool flattenSurface;
        public bool maskMode;

        [Header ("Material Settings")]
        public Material roadMaterial;
        public Material maskMaterial;
        public Material undersideMaterial;
        public float textureTiling = 1;

        [Header("Floor")] 
        public GameObject floor;
        public GameObject maskFloor;
        
        [SerializeField, HideInInspector]
        public GameObject meshHolder;
        public GameObject maskHolder;

        private MeshFilter meshFilter;
        private MeshFilter maskFilter;
        private MeshRenderer meshRenderer;
        private MeshRenderer maskRenderer;
        private Mesh mesh;
        private Mesh maskMesh;

        protected override void PathUpdated() {
            if (pathCreator != null) {
                AssignMeshComponents();
                AssignMaterials();
                CreateRoadMesh();
            }
        }

        public void CreateRoadMesh() 
        {
            Vector3[] verts = new Vector3[path.NumPoints * 8];
            Vector2[] uvs = new Vector2[verts.Length];
            Vector3[] normals = new Vector3[verts.Length];

            int numTris = 2 * (path.NumPoints - 1) + ((path.isClosedLoop) ? 2 : 0);
            int[] roadTriangles = new int[numTris * 3];
            int[] underRoadTriangles = new int[numTris * 3];
            int[] sideOfRoadTriangles = new int[numTris * 2 * 3];

            int vertIndex = 0;
            int triIndex = 0;

            // Vertices for the top of the road are layed out:
            // 0  1
            // 8  9
            // and so on... So the triangle map 0,8,1 for example, defines a triangle from top left to bottom left to bottom right.
            int[] triangleMap = { 0, 8, 1, 1, 8, 9 };
            int[] sidesTriangleMap = { 4, 6, 14, 12, 4, 14, 5, 15, 7, 13, 15, 5 };

            bool usePathNormals = !(path.space == PathSpace.xyz && flattenSurface);

            for (int i = 0; i < path.NumPoints; i++) {
                Vector3 localUp = (usePathNormals) ? Vector3.Cross (path.GetTangent (i), path.GetNormal (i)) : path.up;
                Vector3 localRight = (usePathNormals) ? path.GetNormal (i) : Vector3.Cross (localUp, path.GetTangent (i));

                // Find position to left and right of current path vertex
                Vector3 vertSideA = path.GetPoint (i) - localRight * Mathf.Abs (roadWidth);
                Vector3 vertSideB = path.GetPoint (i) + localRight * Mathf.Abs (roadWidth);

                // Add top of road vertices
                verts[vertIndex + 0] = vertSideA;
                verts[vertIndex + 1] = vertSideB;
                // Add bottom of road vertices
                verts[vertIndex + 2] = vertSideA - localUp * thickness;
                verts[vertIndex + 3] = vertSideB - localUp * thickness;

                // Duplicate vertices to get flat shading for sides of road
                verts[vertIndex + 4] = verts[vertIndex + 0];
                verts[vertIndex + 5] = verts[vertIndex + 1];
                verts[vertIndex + 6] = verts[vertIndex + 2];
                verts[vertIndex + 7] = verts[vertIndex + 3];

                // Set uv on y axis to path time (0 at start of path, up to 1 at end of path)
                uvs[vertIndex + 0] = new Vector2 (0, path.times[i]);
                uvs[vertIndex + 1] = new Vector2 (1, path.times[i]);

                // Top of road normals
                normals[vertIndex + 0] = localUp;
                normals[vertIndex + 1] = localUp;
                // Bottom of road normals
                normals[vertIndex + 2] = -localUp;
                normals[vertIndex + 3] = -localUp;
                // Sides of road normals
                normals[vertIndex + 4] = -localRight;
                normals[vertIndex + 5] = localRight;
                normals[vertIndex + 6] = -localRight;
                normals[vertIndex + 7] = localRight;

                // Set triangle indices
                if (i < path.NumPoints - 1 || path.isClosedLoop) {
                    for (int j = 0; j < triangleMap.Length; j++) {
                        roadTriangles[triIndex + j] = (vertIndex + triangleMap[j]) % verts.Length;
                        // reverse triangle map for under road so that triangles wind the other way and are visible from underneath
                        underRoadTriangles[triIndex + j] = (vertIndex + triangleMap[triangleMap.Length - 1 - j] + 2) % verts.Length;
                    }
                    for (int j = 0; j < sidesTriangleMap.Length; j++) {
                        sideOfRoadTriangles[triIndex * 2 + j] = (vertIndex + sidesTriangleMap[j]) % verts.Length;
                    }

                }

                vertIndex += 8;
                triIndex += 6;
            }

            mesh.Clear();
            maskMesh.Clear();
            
            mesh.vertices = verts;
            maskMesh.vertices = verts;
            
            mesh.uv = uvs;
            mesh.uv = uvs;
            
            mesh.normals = normals;
            maskMesh.normals = normals;
            
            mesh.subMeshCount = 3;
            maskMesh.subMeshCount = 3;
            
            mesh.SetTriangles(roadTriangles, 0);
            mesh.SetTriangles(underRoadTriangles, 1);
            mesh.SetTriangles(sideOfRoadTriangles, 2);
            mesh.RecalculateBounds();
            
            maskMesh.SetTriangles(roadTriangles, 0);
            maskMesh.SetTriangles(underRoadTriangles, 1);
            maskMesh.SetTriangles(sideOfRoadTriangles, 2);
            maskMesh.RecalculateBounds();
        }

        // Add MeshRenderer and MeshFilter components to this gameobject if not already attached
        public void AssignMeshComponents () 
        {
            if (meshHolder == null) {
                meshHolder = new GameObject ("(" + holderIndex + ") Road Mesh Holder");
            }
            
            if (maskHolder == null) {
                maskHolder = new GameObject ("(" + holderIndex + ") Mask Mesh Holder");
            }

            meshHolder.transform.rotation = Quaternion.identity;
            meshHolder.transform.position = new Vector3(0, yOffset, 0);
            meshHolder.transform.localScale = Vector3.one;
            
            maskHolder.transform.rotation = Quaternion.identity;
            maskHolder.transform.position = new Vector3(0, yOffset, 0);
            maskHolder.transform.localScale = Vector3.one;

            // Ensure mesh renderer and filter components are assigned
            if (!meshHolder.gameObject.GetComponent<MeshFilter>()) 
            {
                meshHolder.gameObject.AddComponent<MeshFilter>();
            }
            
            if (!maskHolder.gameObject.GetComponent<MeshFilter>()) 
            {
                maskHolder.gameObject.AddComponent<MeshFilter>();
            }
            
            if (!meshHolder.GetComponent<MeshRenderer>()) 
            {
                meshHolder.gameObject.AddComponent<MeshRenderer>();
            }
            
            if (!maskHolder.GetComponent<MeshRenderer>()) 
            {
                maskHolder.gameObject.AddComponent<MeshRenderer>();
            }

            meshRenderer = meshHolder.GetComponent<MeshRenderer>();
            meshFilter = meshHolder.GetComponent<MeshFilter>();

            maskRenderer = maskHolder.GetComponent<MeshRenderer>();
            maskFilter = maskHolder.GetComponent<MeshFilter>();
            
            if (mesh == null) {
                mesh = new Mesh();
            }
            
            if (maskMesh == null) {
                maskMesh = new Mesh();
            }
            
            meshFilter.sharedMesh = mesh;
            maskFilter.sharedMesh = mesh;
        }


        public void AssignMaterials() 
        {
            floor.SetActive(!maskMode);
            maskFloor.SetActive(maskMode);
            
            meshHolder.SetActive(!maskMode);
            maskHolder.SetActive(maskMode);
            
            if (roadMaterial != null && undersideMaterial != null) {
                meshRenderer.sharedMaterials = new Material[] { roadMaterial, undersideMaterial, undersideMaterial };
                meshRenderer.sharedMaterials[0].mainTextureScale = new Vector3 (1, textureTiling);
            }
            
            if (maskMaterial != null && undersideMaterial != null) {
                maskRenderer.sharedMaterials = new Material[] { maskMaterial, undersideMaterial, undersideMaterial };
                maskRenderer.sharedMaterials[0].mainTextureScale = new Vector3 (1, textureTiling);
            }
        }

        
    }
}