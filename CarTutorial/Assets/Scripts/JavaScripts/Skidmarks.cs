using UnityEngine;
using System.Collections;

/////////
// Skidmarks.js
//
// This script controlles the skidmarks for the car. It registers the position, normal etc. of all the small sections of
// the skidmarks that combined makes up the entire skidmark mesh.
// A new mesh is auto generated whenever the skidmarks change.
 // Maximum number of marks total handled by one instance of the script.
 // The width of the skidmarks. Should match the width of the wheel that it is used for. In meters.
 // The distance the skidmarks is places above the surface it is placed upon. In meters.
 // The minimum distance between two marks places next to each other. 
// Variables for each mark created. Needed to generate the correct mesh.
[System.Serializable]
public class markSection : object
{
    public Vector3 pos;
    public Vector3 normal;
    public Vector4 tangent;
    public Vector3 posl;
    public Vector3 posr;
    public float intensity;
    public int lastIndex;
    public markSection()
    {
        this.pos = Vector3.zero;
        this.normal = Vector3.zero;
        this.tangent = Vector4.zero;
        this.posl = Vector3.zero;
        this.posr = Vector3.zero;
    }

}
[System.Serializable]
[UnityEngine.RequireComponent(typeof(MeshFilter))]
[UnityEngine.RequireComponent(typeof(MeshRenderer))]
public partial class Skidmarks : MonoBehaviour
{
    public int maxMarks;
    public float markWidth;
    public float groundOffset;
    public float minDistance;
    private int indexShift;
    private int numMarks;
    private markSection[] skidmarks;
    private bool updated;
    // Initiallizes the array holding the skidmark sections.
    public virtual void Awake()
    {
        this.skidmarks = new markSection[this.maxMarks];
        int i = 0;
        while (i < this.maxMarks)
        {
            this.skidmarks[i] = new markSection();
            i++;
        }
        if (((MeshFilter) this.GetComponent(typeof(MeshFilter))).mesh == null)
        {
            ((MeshFilter) this.GetComponent(typeof(MeshFilter))).mesh = new Mesh();
        }
    }

    // Function called by the wheels that is skidding. Gathers all the information needed to
    // create the mesh later. Sets the intensity of the skidmark section b setting the alpha
    // of the vertex color.
    public virtual int AddSkidMark(Vector3 pos, Vector3 normal, float intensity, int lastIndex)
    {
        if (intensity > 1)
        {
            intensity = 1f;
        }
        if (intensity < 0)
        {
            return -1;
        }
        markSection curr = this.skidmarks[this.numMarks % this.maxMarks];
        curr.pos = pos + (normal * this.groundOffset);
        curr.normal = normal;
        curr.intensity = intensity;
        curr.lastIndex = lastIndex;
        if (lastIndex != -1)
        {
            markSection last = this.skidmarks[lastIndex % this.maxMarks];
            Vector3 dir = curr.pos - last.pos;
            Vector3 xDir = Vector3.Cross(dir, normal).normalized;
            curr.posl = curr.pos + ((xDir * this.markWidth) * 0.5f);
            curr.posr = curr.pos - ((xDir * this.markWidth) * 0.5f);
            curr.tangent = new Vector4(xDir.x, xDir.y, xDir.z, 1);
            if (last.lastIndex == -1)
            {
                last.tangent = curr.tangent;
                last.posl = curr.pos + ((xDir * this.markWidth) * 0.5f);
                last.posr = curr.pos - ((xDir * this.markWidth) * 0.5f);
            }
        }
        this.numMarks++;
        this.updated = true;
        return this.numMarks - 1;
    }

    // If the mesh needs to be updated, i.e. a new section has been added,
    // the current mesh is removed, and a new mesh for the skidmarks is generated.
    public virtual void LateUpdate()
    {
        if (!this.updated)
        {
            return;
        }
        this.updated = false;
        Mesh mesh = ((MeshFilter) this.GetComponent(typeof(MeshFilter))).mesh;
        mesh.Clear();
        int segmentCount = 0;
        int j = 0;
        while ((j < this.numMarks) && (j < this.maxMarks))
        {
            if ((this.skidmarks[j].lastIndex != -1) && (this.skidmarks[j].lastIndex > (this.numMarks - this.maxMarks)))
            {
                segmentCount++;
            }
            j++;
        }
        Vector3[] vertices = new Vector3[segmentCount * 4];
        Vector3[] normals = new Vector3[segmentCount * 4];
        Vector4[] tangents = new Vector4[segmentCount * 4];
        Color[] colors = new Color[segmentCount * 4];
        Vector2[] uvs = new Vector2[segmentCount * 4];
        int[] triangles = new int[segmentCount * 6];
        segmentCount = 0;
        int i = 0;
        while ((i < this.numMarks) && (i < this.maxMarks))
        {
            if ((this.skidmarks[i].lastIndex != -1) && (this.skidmarks[i].lastIndex > (this.numMarks - this.maxMarks)))
            {
                markSection curr = this.skidmarks[i];
                markSection last = this.skidmarks[curr.lastIndex % this.maxMarks];
                vertices[(segmentCount * 4) + 0] = last.posl;
                vertices[(segmentCount * 4) + 1] = last.posr;
                vertices[(segmentCount * 4) + 2] = curr.posl;
                vertices[(segmentCount * 4) + 3] = curr.posr;
                normals[(segmentCount * 4) + 0] = last.normal;
                normals[(segmentCount * 4) + 1] = last.normal;
                normals[(segmentCount * 4) + 2] = curr.normal;
                normals[(segmentCount * 4) + 3] = curr.normal;
                tangents[(segmentCount * 4) + 0] = last.tangent;
                tangents[(segmentCount * 4) + 1] = last.tangent;
                tangents[(segmentCount * 4) + 2] = curr.tangent;
                tangents[(segmentCount * 4) + 3] = curr.tangent;
                colors[(segmentCount * 4) + 0] = new Color(0, 0, 0, last.intensity);
                colors[(segmentCount * 4) + 1] = new Color(0, 0, 0, last.intensity);
                colors[(segmentCount * 4) + 2] = new Color(0, 0, 0, curr.intensity);
                colors[(segmentCount * 4) + 3] = new Color(0, 0, 0, curr.intensity);
                uvs[(segmentCount * 4) + 0] = new Vector2(0, 0);
                uvs[(segmentCount * 4) + 1] = new Vector2(1, 0);
                uvs[(segmentCount * 4) + 2] = new Vector2(0, 1);
                uvs[(segmentCount * 4) + 3] = new Vector2(1, 1);
                triangles[(segmentCount * 6) + 0] = (segmentCount * 4) + 0;
                triangles[(segmentCount * 6) + 2] = (segmentCount * 4) + 1;
                triangles[(segmentCount * 6) + 1] = (segmentCount * 4) + 2;
                triangles[(segmentCount * 6) + 3] = (segmentCount * 4) + 2;
                triangles[(segmentCount * 6) + 5] = (segmentCount * 4) + 1;
                triangles[(segmentCount * 6) + 4] = (segmentCount * 4) + 3;
                segmentCount++;
            }
            i++;
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.tangents = tangents;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.uv = uvs;
    }

    public Skidmarks()
    {
        this.maxMarks = 1024;
        this.markWidth = 0.275f;
        this.groundOffset = 0.02f;
        this.minDistance = 0.1f;
    }

}