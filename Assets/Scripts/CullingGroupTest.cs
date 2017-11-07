using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.VR;

[RequireComponent(typeof(InstanceRenderer))]
public class CullingGroupTest : MonoBehaviour
{
    InstanceRenderer instanceRenderer_;
    [SerializeField] Transform origin;
    [SerializeField] int count = 50000;
    [SerializeField, Tooltip("Changes won't take effect during runtime")] bool viewFrustumCulling = false;

    CullingGroup atomCullingGroup_;
    CullingGroup bondCullingGroup_;
    Instance[] instances_;
    // Instance[] bonds_;
    ComputeBuffer sharedInstanceBuffer_;
    BoundingSphere[] atomBounds_;
    BoundingSphere[] bondBounds_;
    int atomCount0_ = 0, atomCount1_ = 0, atomCount2_ = 0,
    atomCount3_ = 0, atomCount4_ = 0;
    int bondCount0_ = 0, bondCount1_ = 0, bondCount2_ = 0,
    bondCount3_ = 0, bondCount4_ = 0;
    int[] atomLod0_;
    int[] atomLod1_;
    int[] atomLod2_;
    int[] atomLod3_;
    int[] atomLod4_;
    int[] bondLod0_;
    int[] bondLod1_;
    int[] bondLod2_;
    int[] bondLod3_;
    int[] bondLod4_;
    float[] distances_ = new float[5] { 5f, 10f, 15f, 20f, 100f };
    InstanceGroup atomLOD0_;
    InstanceGroup atomLOD1_;
    InstanceGroup atomLOD2_;
    InstanceGroup atomLOD3_;
    InstanceGroup atomLOD4_;
    InstanceGroup bondLOD0_;
    InstanceGroup bondLOD1_;
    InstanceGroup bondLOD2_;
    InstanceGroup bondLOD3_;
    InstanceGroup bondLOD4_;

    Transform inverseTrans_;

    void Initialize()
    {
        instanceRenderer_ = GetComponent<InstanceRenderer>();

        instances_ = new Instance[count * 2];
        atomBounds_ = new BoundingSphere[count];
        bondBounds_ = new BoundingSphere[count * 2];

        GameObject go = new GameObject("LocalCam");
        // GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Camera localCam = new Camera();
        if (viewFrustumCulling)
        {
            localCam = go.AddComponent<Camera>();
            localCam.CopyFrom(Camera.main);
            localCam.stereoTargetEye = StereoTargetEyeMask.None;
        }
        inverseTrans_ = go.transform;
        inverseTrans_.position = origin.InverseTransformPoint(Camera.main.transform.position);
        Vector3 up = Camera.main.transform.up;
        Vector3 forward = Camera.main.transform.forward;
        up = origin.TransformDirection(up);
        forward = origin.TransformDirection(forward);
        inverseTrans_.rotation = Quaternion.LookRotation(forward, up);

        atomCullingGroup_ = new CullingGroup();
        bondCullingGroup_ = new CullingGroup();
        if (viewFrustumCulling)
        {
            atomCullingGroup_.targetCamera = localCam;
            atomCullingGroup_.onStateChanged = VisibilityChangeCallback;
            bondCullingGroup_.targetCamera = localCam;
            bondCullingGroup_.onStateChanged = VisibilityChangeCallback2;
        }
        else
        {
            atomCullingGroup_.targetCamera = Camera.main;
            bondCullingGroup_.targetCamera = Camera.main;
        }
        atomCullingGroup_.SetBoundingDistances(distances_);
        atomCullingGroup_.SetBoundingSpheres(atomBounds_);
        atomCullingGroup_.SetBoundingSphereCount(count);
        atomCullingGroup_.SetDistanceReferencePoint(inverseTrans_);

        bondCullingGroup_.SetBoundingDistances(distances_);
        bondCullingGroup_.SetBoundingSpheres(bondBounds_);
        bondCullingGroup_.SetBoundingSphereCount(count * 2);
        bondCullingGroup_.SetDistanceReferencePoint(inverseTrans_);

        atomLod0_ = new int[count];
        atomLod1_ = new int[count];
        atomLod2_ = new int[count];
        atomLod3_ = new int[count];
        atomLod4_ = new int[count];

        bondLod0_ = new int[count];
        bondLod1_ = new int[count];
        bondLod2_ = new int[count];
        bondLod3_ = new int[count];
        bondLod4_ = new int[count];

        sharedInstanceBuffer_ = new ComputeBuffer(count * 2, Marshal.SizeOf(typeof(Instance)), ComputeBufferType.Default);
        instanceRenderer_.SetSharedBuffer("_Particles", sharedInstanceBuffer_);
    }

    void AtomRandomFill()
    {
        int i;
        for (i = 0; i < count; i++)
        {
            Vector3 position = Random.insideUnitSphere * 50f;
            Vector3 rotation = Random.rotationUniform.eulerAngles;
            Vector4 color = Random.ColorHSV(0.25f, 0.55f, 0.9f, 1f, 0.7f, 1f);
            float scale = Random.Range(0.05f, 0.1f);
            int highlight = Random.Range(0, 2);
            instances_[i].position = position;
            instances_[i].rotation = new Vector3(45, 45, 45) * Mathf.Deg2Rad;
            instances_[i].color = color;
            instances_[i].scale = viewFrustumCulling ? Vector3.zero : Vector3.one * scale;
            instances_[i].highlight = highlight;

            Vector3 fwd = Random.onUnitSphere * scale * 2;
            instances_[i + count].position = position + fwd;
            float angle = Vector3.Angle(Vector3.up, fwd.normalized) * Mathf.Deg2Rad;
            Vector3 axis = Vector3.Cross(fwd.normalized, Vector3.up).normalized;
            instances_[i + count].rotation = axis * angle;
            instances_[i + count].color = color;
            instances_[i + count].scale = viewFrustumCulling ? new Vector3(scale, 0, scale) : new Vector3(scale, scale * 4, scale);
            instances_[i + count].highlight = highlight;

            atomBounds_[i].position = position;
            bondBounds_[i + count].position = position + fwd;
            atomBounds_[i].radius = scale;
            bondBounds_[i + count].radius = scale * 4;

            atomLod0_[i] = 0;
            atomLod1_[i] = 0;
            atomLod2_[i] = 0;
            atomLod3_[i] = 0;
            atomLod4_[i] = i;

            bondLod0_[i] = bondLod1_[i] = bondLod2_[i] = bondLod3_[i] = 0;
            bondLod4_[i] = i + count;
        }
        sharedInstanceBuffer_.SetData(instances_);

        atomCount0_ = atomCount1_ = atomCount2_ = atomCount3_ = 0;
        atomCount4_ = count;

        bondCount0_ = bondCount1_ = bondCount2_ = bondCount3_ = 0;
        bondCount4_ = count;

        atomLOD0_ = new InstanceGroup(CustomMesh.CreateSphere(24, 16), count, origin);
        atomLOD1_ = new InstanceGroup(CustomMesh.CreateSphere(16, 12), count, origin);
        atomLOD2_ = new InstanceGroup(CustomMesh.CreateSphere(10, 6), count, origin);
        atomLOD3_ = new InstanceGroup(CustomMesh.CreateSphere(8, 4), count, origin);
        atomLOD4_ = new InstanceGroup(CustomMesh.CreateSphere(6, 2), count, origin);

        // atomLOD0_ = new InstanceGroup(CustomMesh.CreateCylinder(1, 24), count, origin);
        // atomLOD1_ = new InstanceGroup(CustomMesh.CreateCylinder(1, 16), count, origin);
        // atomLOD2_ = new InstanceGroup(CustomMesh.CreateCylinder(1, 10), count, origin);
        // atomLOD3_ = new InstanceGroup(CustomMesh.CreateCylinder(1, 7), count, origin);
        // atomLOD4_ = new InstanceGroup(CustomMesh.CreateCylinder(1, 5), count, origin);

        bondLOD0_ = new InstanceGroup(CustomMesh.CreateCylinder(1, 24), count, origin);
        bondLOD1_ = new InstanceGroup(CustomMesh.CreateCylinder(1, 16), count, origin);
        bondLOD2_ = new InstanceGroup(CustomMesh.CreateCylinder(1, 10), count, origin);
        bondLOD3_ = new InstanceGroup(CustomMesh.CreateCylinder(1, 7), count, origin);
        bondLOD4_ = new InstanceGroup(CustomMesh.CreateCylinder(1, 5), count, origin);

        instanceRenderer_.instanceGroups.Add(atomLOD0_);
        instanceRenderer_.instanceGroups.Add(atomLOD1_);
        instanceRenderer_.instanceGroups.Add(atomLOD2_);
        instanceRenderer_.instanceGroups.Add(atomLOD3_);
        instanceRenderer_.instanceGroups.Add(atomLOD4_);

        instanceRenderer_.instanceGroups.Add(bondLOD0_);
        instanceRenderer_.instanceGroups.Add(bondLOD1_);
        instanceRenderer_.instanceGroups.Add(bondLOD2_);
        instanceRenderer_.instanceGroups.Add(bondLOD3_);
        instanceRenderer_.instanceGroups.Add(bondLOD4_);
    }

    void LODGrouping()
    {
        atomCount0_ = atomCullingGroup_.QueryIndices(0, atomLod0_, 0);
        atomLOD0_.IdBuffer_.SetData(atomLod0_);
        atomLOD0_.instanceCount = atomCount0_;

        atomCount1_ = atomCullingGroup_.QueryIndices(1, atomLod1_, 0);
        atomLOD1_.IdBuffer_.SetData(atomLod1_);
        atomLOD1_.instanceCount = atomCount1_;

        atomCount2_ = atomCullingGroup_.QueryIndices(2, atomLod2_, 0);
        atomLOD2_.IdBuffer_.SetData(atomLod2_);
        atomLOD2_.instanceCount = atomCount2_;

        atomCount3_ = atomCullingGroup_.QueryIndices(3, atomLod3_, 0);
        atomLOD3_.IdBuffer_.SetData(atomLod3_);
        atomLOD3_.instanceCount = atomCount3_;

        atomCount4_ = atomCullingGroup_.QueryIndices(4, atomLod4_, 0);
        atomLOD4_.IdBuffer_.SetData(atomLod4_);
        atomLOD4_.instanceCount = atomCount4_;

        bondCount0_ = bondCullingGroup_.QueryIndices(0, bondLod0_, count);
        bondLOD0_.IdBuffer_.SetData(bondLod0_);
        bondLOD0_.instanceCount = bondCount0_;

        bondCount1_ = bondCullingGroup_.QueryIndices(1, bondLod1_, count);
        bondLOD1_.IdBuffer_.SetData(bondLod1_);
        bondLOD1_.instanceCount = bondCount1_;

        bondCount2_ = bondCullingGroup_.QueryIndices(2, bondLod2_, count);
        bondLOD2_.IdBuffer_.SetData(bondLod2_);
        bondLOD2_.instanceCount = bondCount2_;

        bondCount3_ = bondCullingGroup_.QueryIndices(3, bondLod3_, count);
        bondLOD3_.IdBuffer_.SetData(bondLod3_);
        bondLOD3_.instanceCount = bondCount3_;

        bondCount4_ = bondCullingGroup_.QueryIndices(4, bondLod4_, count);
        bondLOD4_.IdBuffer_.SetData(bondLod4_);
        bondLOD4_.instanceCount = bondCount4_;
    }

    void UpdateCullingGroup()
    {
        inverseTrans_.position = origin.InverseTransformPoint(Camera.main.transform.position);
        Vector3 up = Camera.main.transform.up;
        Vector3 forward = Camera.main.transform.forward;
        up = origin.InverseTransformDirection(up);
        forward = origin.InverseTransformDirection(forward);
        inverseTrans_.rotation = Quaternion.LookRotation(forward, up);
    }

    void VisibilityChangeCallback(CullingGroupEvent e)
    {
        if (e.hasBecomeInvisible)
        {
            instances_[e.index].scale = Vector3.zero;
        }
        if (e.hasBecomeVisible)
        {
            instances_[e.index].scale = Vector3.one * atomBounds_[e.index].radius;
        }
    }

    void VisibilityChangeCallback2(CullingGroupEvent e)
    {
        if (e.hasBecomeInvisible)
        {
            if (e.index > count)
                instances_[e.index].scale.y = 0;
        }
        if (e.hasBecomeVisible)
        {
            if (e.index > count)
                instances_[e.index].scale.y = bondBounds_[e.index].radius;
        }
    }

    void Start()
    {
        Initialize();
        AtomRandomFill();
    }

    void Update()
    {
        UpdateCullingGroup();
        sharedInstanceBuffer_.SetData(instances_);
        LODGrouping();
    }

    void OnDisable()
    {
        if (sharedInstanceBuffer_ != null)
            sharedInstanceBuffer_.Release();
        sharedInstanceBuffer_ = null;

        atomCullingGroup_.Dispose();
        bondCullingGroup_.Dispose();
    }
}
