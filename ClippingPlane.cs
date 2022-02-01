using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class ClippingPlane : MonoBehaviour
{
    //material we pass the values to
    [SerializeField] GameObject Printer;
    [SerializeField] Vector3 LevelUpJump;
    [SerializeField] Vector3 startingPos;
    [SerializeField] Vector3 endPos;
    [SerializeField] int Percent;
    [SerializeField] Material mat;
    [SerializeField] GameObject sphere;
    [SerializeField] GameObject SphereContainer;
    [SerializeField] float rayDistance;
    [SerializeField] int rayStep = 4;
    [SerializeField] int rayOffset = 0;
    [SerializeField] Vector3 rayVectorOffset;
    [SerializeField] int rayOffZone = 10;
    [SerializeField] LayerMask whatIsPrinted;
    [SerializeField] float pointSize = 0.2f;
    [Header("Tunnel Generation")]
    [SerializeField] int angularSegments = 3;
    [SerializeField] float innerRadius = 0.5f;
    [SerializeField] float outerRadius = 0.7f;
    private List<GameObject> points;
    [SerializeField] MeshFilter DynMeshFilter;
    Mesh mesh;
    Mesh DynMesh;
    List<Vector3> innerVertices;
    List<Vector3> outerVertices;
    bool RenderDynamically = false;
    int pointCount;
    private void Awake()
    {
        //transform.position = startingPos;
        mesh = new Mesh();
        mesh.name = "Tunnel";
        GetComponent<MeshFilter>().sharedMesh = mesh;
        points = new List<GameObject>();

        DynMesh = new Mesh();
        DynMesh.name = "Dynamic Mesh";
        DynMeshFilter.sharedMesh = DynMesh;

        SpawnPoints();
        
    }

    private void Update()
    {
        //create plane
        Plane plane = new Plane(transform.up, transform.position);
        //transfer values from plane to vector4
        Vector4 planeRepresentation = new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
        //pass vector to shader
        mat.SetVector("_Plane", planeRepresentation);
    }
    //execute every frame
    public void ClipUpdate()
    {
        if (RenderDynamically)
        {
            generateMeshDynamically();
            SetPrinterPos();
        }

    }

    public void GoNextLayer()
    {
        ClearPoints();
        SpawnPoints();

        transform.position += LevelUpJump;
    }

    public void SetPercent(int p)
    {
        Percent = p;
    }

    void SetPrinterPos()
    {
        int index = Mathf.Clamp(pointCount - (100 - Percent) * pointCount / 100 - 1, 0, 10000000);
        if (pointCount != 0)
            Printer.transform.position = points[index].transform.position + new Vector3(0, 1f, 0);
    }

    public void SpawnPoints()
    {

        for (int i = 0; i < 360; i += rayStep)
        {
            Quaternion rot = Quaternion.Euler(0, i + rayOffset, 0);
            RaycastHit hit;
            Physics.Raycast(transform.position, rot * transform.forward, out hit, rayDistance, whatIsPrinted);
            Vector3 point = hit.point + rot * transform.forward / rayOffZone + rayVectorOffset;
            GameObject g = new GameObject();
            g.name = "MGEZAVI";
            g.transform.position = point;
            g.transform.localScale = Vector3.one * pointSize;
            g.transform.parent = SphereContainer.transform;
            points.Add(g);


        }
        pointCount = points.Count;
        generateTunnelPoints();
        generateMesh();

    }

    public void ClearPoints()
    {
        foreach (GameObject g in points)
        {
            DestroyImmediate(g);
        }
        points.Clear();
        pointCount = 0;
        Percent = 0;
    }

    public void generateTunnelPoints()
    {
        mesh.Clear();
        innerVertices = new List<Vector3>();
        outerVertices = new List<Vector3>();
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 dir;
            if (i < points.Count - 1)
            {
                dir = points[i + 1].transform.position - points[i].transform.position;
            }
            else
            {
                dir = points[0].transform.position - points[i].transform.position;
            }
            points[i].transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            for (int a = 0; a < angularSegments; a++)
            {
                float t = a / (float)angularSegments;
                float angRad = t * Meth.TAU;
                Vector3 circularDir = Meth.GetUnitVectorByAngle(angRad);
                Vector3 innerVertex = points[i].transform.position + points[i].transform.rotation * circularDir * innerRadius;
                Vector3 outerVertex = points[i].transform.position + points[i].transform.rotation * circularDir * outerRadius;

                innerVertices.Add(innerVertex - transform.position);
                outerVertices.Add(outerVertex - transform.position);
            }
        }
        mesh.SetVertices(innerVertices);
        DynMesh.SetVertices(outerVertices);
        RenderDynamically = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (innerVertices != null)
        {
            foreach (Vector3 t in innerVertices)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(t + transform.position, 0.01f);
            }
        }
        if (outerVertices != null)
        {
            foreach (Vector3 t in outerVertices)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(t + transform.position, 0.01f);
            }
        }

    }

    public void generateMesh()
    {
        int pointCount = points.Count;
        List<int> triIndices = new List<int>();

        for (int i = 0; i < pointCount; i++)
        {
            //int Next = ((i + 1) % pointCount) * angularSegments;
            for (int a = 0; a < angularSegments; a++)
            {
                triIndices.Add(i * angularSegments + a);
                triIndices.Add(i * angularSegments + (a + 1) % angularSegments);
                triIndices.Add(((i + 1) % pointCount) * angularSegments + a);


                triIndices.Add(((i + 1) % pointCount) * angularSegments + a);
                triIndices.Add(i * angularSegments + (a + 1) % angularSegments);
                triIndices.Add(((i + 1) % pointCount) * angularSegments + (a + 1) % angularSegments);
            }
        }

        mesh.SetTriangles(triIndices, 0);
        mesh.RecalculateNormals();
    }
    public void generateMeshDynamically()
    {
        //DynMesh.Clear();
        List<int> triIndices = new List<int>();

        for (int i = 0; i < pointCount - (100 - Percent) * pointCount / 100; i++)
        {
            //int Next = ((i + 1) % pointCount) * angularSegments;
            for (int a = 0; a < angularSegments; a++)
            {
                triIndices.Add(i * angularSegments + a);
                triIndices.Add(i * angularSegments + (a + 1) % angularSegments);
                triIndices.Add(((i + 1) % pointCount) * angularSegments + a);


                triIndices.Add(((i + 1) % pointCount) * angularSegments + a);
                triIndices.Add(i * angularSegments + (a + 1) % angularSegments);
                triIndices.Add(((i + 1) % pointCount) * angularSegments + (a + 1) % angularSegments);
            }
        }

        DynMesh.SetTriangles(triIndices, 0);
        DynMesh.RecalculateNormals();
    }
}
