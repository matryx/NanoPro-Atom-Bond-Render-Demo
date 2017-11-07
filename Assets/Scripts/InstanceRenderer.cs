using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Instance
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public Vector4 color;
    public int highlight;
}

public class InstanceRenderer : MonoBehaviour
{
    public List<InstanceGroup> instanceGroups = new List<InstanceGroup>();
    [SerializeField]
    Shader shader;
    [SerializeField]
    bool useOutlineColor = true;
    [SerializeField]
    Color outlineColor = Color.cyan;

    Material material_;

    void Awake()
    {
        material_ = new Material(shader);
        material_.SetInt("_UseOutlineColor", useOutlineColor ? 1 : 0);
        material_.SetColor("_OutlineColor", outlineColor);
    }

    void Update()
    {
        foreach (InstanceGroup group in instanceGroups)
        {
            group.Update();
            Graphics.DrawMeshInstancedIndirect(group.instanceMesh, 0,
            material_, new Bounds(Vector3.zero, Vector3.one * 50f), group.instanceArgsBuffer_, 0,
            group.propertyBlock_, UnityEngine.Rendering.ShadowCastingMode.On, true, LayerMask.NameToLayer("Default"));
        }
    }

    public void SetSharedBuffer(string name, ComputeBuffer buffer)
    {
        material_.SetBuffer(name, buffer);
    }

    void OnDisable()
    {
        foreach (InstanceGroup group in instanceGroups)
        {
            group.FinalizeBuffers();
        }
    }
}
