﻿/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

using System.Linq;

using UnityEditor;
using UnityEngine;

namespace MudBun
{
  [CustomEditor(typeof(MudRenderer), true)]
  [CanEditMultipleObjects]
  public class MudRendererEditor : MudRendererBaseEditor
  {
    protected override void LockMesh()
    {
      base.LockMesh();

      foreach (var renderer in targets.Select(x => (MudRenderer) x))
      {
        if (renderer == null)
          continue;

        if (renderer.MeshLocked)
          continue;

        DoLockMesh(renderer.transform, renderer.RecursiveLockMeshByEditor);
      }
    }

    private void DoLockMesh(Transform t, bool recursive, int depth = 0)
    {
      if (t == null)
        return;

      if (recursive)
      {
        for (int i = 0; i < t.childCount; ++i)
          DoLockMesh(t.GetChild(i), recursive, depth + 1);
      }

      var renderer = t.GetComponent<MudRenderer>();
      if (renderer == null)
        return;

      var prevMeshGenerationRenderableMeshMode = renderer.MeshGenerationRenderableMeshMode;
      if (MeshGenerationCreateNewObject.boolValue)
        renderer.MeshGenerationRenderableMeshMode = MudRendererBase.RenderableMeshMode.MeshRenderer;

      if (MeshGenerationCreateCollider.boolValue)
      {
        renderer.AddCollider(renderer.gameObject, false, null, MeshGenerationForceConvexCollider.boolValue, MeshGenerationCreateRigidBody.boolValue);
      }

      renderer.LockMesh(MeshGenerationAutoRigging.boolValue, false, null, (MudRendererBase.UVGenerationMode) MeshGenerationUVGeneration.intValue, MeshGenerationWeldVertices.boolValue);

      if (MeshGenerationCreateNewObject.boolValue)
        renderer.MeshGenerationRenderableMeshMode = prevMeshGenerationRenderableMeshMode;

      if (GenerateMeshAssetByEditor.boolValue)
      {
        renderer.ValidateAssetNames();

        string rootFolder = "Assets";
        string assetsFolder = "MudBun Generated Assets";
        string folderPath = $"{rootFolder}/{assetsFolder}";
        string assetName = renderer.GenerateMeshAssetByEditorName;

        if (!AssetDatabase.IsValidFolder(folderPath))
          AssetDatabase.CreateFolder(rootFolder, assetsFolder);

        Mesh mesh = null;
        Material mat = null;
        var meshFilter = renderer.GetComponent<MeshFilter>();
        var meshRenderer = renderer.GetComponent<MeshRenderer>();
        var skinnedMeshRenderer = renderer.GetComponent<SkinnedMeshRenderer>();
        if (meshRenderer != null)
        {
          if (meshFilter != null)
          {
            mesh = meshFilter.sharedMesh;
          }
          mat = meshRenderer.sharedMaterial;
        }
        else if (skinnedMeshRenderer != null)
        {
          mesh = skinnedMeshRenderer.sharedMesh;
          mat = skinnedMeshRenderer.sharedMaterial;
        }

        if (mesh != null)
        {
          string meshAssetPath = $"{folderPath}/{assetName}.mesh";
          AssetDatabase.CreateAsset(mesh, meshAssetPath);
          AssetDatabase.Refresh();

          Debug.Log($"MudBun: Saved mesh asset - \"{folderPath}/{assetName}.mesh\"");

          // somehow serialized properties get invalidated after asset database operations
          InitSerializedProperties();

          var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
          if (savedMesh != null)
          {
            if (meshFilter != null)
              meshFilter.sharedMesh = savedMesh;
          }
        }

        if (mat != null)
        {
          if (meshRenderer != null)
            meshRenderer.sharedMaterial = mat;
          else if (skinnedMeshRenderer != null)
            skinnedMeshRenderer.sharedMaterial = mat;
        }
      }

      if (depth == 0 
          && MeshGenerationCreateNewObject.boolValue)
      {
        var clone = Instantiate(renderer.gameObject);
        clone.name = renderer.name + " (Locked Mesh Clone)";

        if (MeshGenerationAutoRigging.boolValue)
        {
          var cloneRenderer = clone.GetComponent<MudRenderer>();
          cloneRenderer.RescanBrushesImmediate();
          cloneRenderer.DestoryAllBrushesImmediate();
        }
        else
        {
          DestroyAllChildren(clone.transform);
        }

        Undo.RegisterCreatedObjectUndo(clone, clone.name);
        DestroyImmediate(clone.GetComponent<MudRenderer>());
        Selection.activeObject = clone;

        renderer.UnlockMesh();
      }
    }

    protected override void GenerateSdf()
    {
      var renderer = (MudRendererBase)target;
      if (renderer == null)
        return;

      renderer.ValidateAssetNames();

      string rootFolder = "Assets";
      string assetsFolder = "MudBun Generated Assets";
      string folderPath = $"{rootFolder}/{assetsFolder}";
      string assetName = renderer.GenerateSdfByEditorName;

      if (!AssetDatabase.IsValidFolder(folderPath))
        AssetDatabase.CreateFolder(rootFolder, assetsFolder);

      Vector3Int size = GenerateSdfTextureSize.vector3IntValue;
      var sdf = new Texture3D(size.x, size.y, size.z, TextureFormat.RFloat, false);
      renderer.GenerateSdf(sdf, GenerateSdfCenter.vector3Value, GenerateSdfDimension.vector3Value);

      string sdfAssetPath = $"{folderPath}/{assetName}.asset";
      AssetDatabase.CreateAsset(sdf, sdfAssetPath);
      AssetDatabase.Refresh();

      renderer.MarkNeedsCompute();

      Debug.Log($"MudBun: Saved SDF texture - \"{folderPath}/{assetName}.asset\"");
    }
  }
}

