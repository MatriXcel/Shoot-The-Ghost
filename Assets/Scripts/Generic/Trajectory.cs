using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct PathHitInfo
{
    public float t;
    public Vector3 normal;
    public PhysicsMaterial2D pB;
}

struct Path
{
    public Vector3 x0, v0;
    public float t;
}
public class Trajectory : MonoBehaviour
{
    float maxTime;
    float timeStep;
    float minSpeed;
    int maxBounce;

    float targetTime;
    float t = 0;


    Vector3 gravity;

    float planeQuadraticIntersection(Vector3 x0, Vector3 v0, Vector3 pos, Vector3 normal)
    {
        float a = Vector3.Dot((0.5f * this.gravity), normal);
        float b = Vector3.Dot(v0, normal);
        float c = Vector3.Dot((x0 - pos), normal);

        if(a != 0)
        {
            float d = Mathf.Sqrt(b * b - 4 * a * c);
            return (-b - d)/(2 * a);
        }
        else
            return -c / b;
    }

    Vector3 getVelocity(Vector3 v0, float t)
    {
        return this.gravity * t + v0;
    }
    Vector3 getPosition(Vector3 x0, Vector3 v0, float t)
    {
        return 0.5f * this.gravity * t * t + v0 * t + x0;
    }

    PathHitInfo CalculateSingle(Vector3 x0, Vector3 v0, LayerMask ignoreMask)
    {
        float t = 0;
        bool hit = false;
        
        RaycastHit2D rayInfo;

        do{
            Vector3 p0 = getPosition(x0, v0, t);
            Vector3 p1 = getPosition(x0, v0, t + this.timeStep);

            t = t + this.timeStep;
            rayInfo = Physics2D.Raycast(p0, p1 - p0, (p1 - p0).magnitude, ~ignoreMask);
            
            hit = rayInfo.collider;            
        }while(hit == false && t < this.maxTime);


        if(hit){
            t  = this.planeQuadraticIntersection(x0, v0, rayInfo.point, rayInfo.normal);
            return new PathHitInfo(){t = t, normal = rayInfo.normal, pB = rayInfo.collider.sharedMaterial};
        }

        return new PathHitInfo();
    }

    List<Path> Cast(Vector3 x0, Vector3 v0, PhysicsMaterial2D pA, LayerMask ignoreMask)
    {
        int bounce = 0;
        float t = 0;
        PathHitInfo pathHitInfo;
        float speedPow2 = this.minSpeed * this.minSpeed;
        List<Path> paths= new List<Path>();

        while(Vector3.Dot(v0, v0) >= speedPow2 && bounce <= this.maxBounce)
        {
            pathHitInfo = this.CalculateSingle(x0, v0, ignoreMask);
            paths.Add(new Path(){ x0 = x0, v0 = v0, t = pathHitInfo.t });

            float elast = (pA.bounciness + pathHitInfo.pB.bounciness)/2;
            float frict = (pA.friction + pathHitInfo.pB.friction)/2;
            float dot = 1- Mathf.Abs(Vector3.Dot(v0.normalized, pathHitInfo.normal));

            v0 = Vector3.Reflect(this.getVelocity(v0, t), pathHitInfo.normal) * elast + v0 * frict;
            bounce += 1;
        }

        return paths;
    }



    void Travel(Rigidbody rb2d, List<Path> paths)
    {
        Path path = paths[0];
        targetTime = 0;

        for(int i = 0; i < paths.Count; i++)
        {
            if(t >= targetTime)
            {
                path = paths[i];
                rb2d.velocity = path.v0;
            }

            targetTime = targetTime + path.t;
        }
        
        targetTime = 0;
    }
}
