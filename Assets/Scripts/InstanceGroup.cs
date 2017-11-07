using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class InstanceGroup
{
    public Mesh instanceMesh;
    public int instanceCount;
    public Transform origin;

    public ComputeBuffer IdBuffer_;
    public ComputeBuffer instanceArgsBuffer_;
    uint[] instanceArgs_ = new uint[5] { 0, 0, 0, 0, 0 };
    int cachedCount_ = -1;
    public MaterialPropertyBlock propertyBlock_;

    public InstanceGroup()
    {
        instanceMesh = CustomMesh.CreateSphere();
        instanceCount = 10000;
        origin.position = Vector3.zero;
        origin.rotation = Quaternion.identity;
        Initialize();
    }

    public InstanceGroup(Mesh mesh, int count, Transform t)
    {
        instanceMesh = mesh;
        instanceCount = count;
        origin = t;
        Initialize();
    }

    public void FinalizeBuffers()
    {
        if (IdBuffer_ != null)
            IdBuffer_.Release();
        IdBuffer_ = null;
        if (instanceArgsBuffer_ != null)
            instanceArgsBuffer_.Release();
        instanceArgsBuffer_ = null;
    }

    void Initialize()
    {
        // initialize compute buffers
        {
            IdBuffer_ = new ComputeBuffer(instanceCount, sizeof(int), ComputeBufferType.Default);
            instanceArgsBuffer_ = new ComputeBuffer(instanceArgs_.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
            instanceArgs_[0] = instanceMesh.GetIndexCount(0);
            instanceArgs_[1] = (uint)instanceCount;
            instanceArgsBuffer_.SetData(instanceArgs_);
        }

        // setup material property
        {
            propertyBlock_ = new MaterialPropertyBlock();
            propertyBlock_.SetBuffer("_Ids", IdBuffer_);
            propertyBlock_.SetMatrix("_localToWorld", origin.localToWorldMatrix);
        }

        cachedCount_ = instanceCount;
    }

    public void Update()
    {
        if(cachedCount_ != instanceCount){
            instanceArgs_[1] = (uint)instanceCount;
            instanceArgsBuffer_.SetData(instanceArgs_);
            cachedCount_ = instanceCount;
        }
        propertyBlock_.SetMatrix("_localToWorld", origin.localToWorldMatrix);
    }
}
