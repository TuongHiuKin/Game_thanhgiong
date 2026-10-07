using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Deterministic dressing over the existing playable layout. No gameplay colliders.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(180)]
public sealed class LegendLandscapePresentation : MonoBehaviour
{
    public const int ClusterBudget = 64, PlantBudget = 800, VertexBudget = 60000;
    [SerializeField, Range(0, 6)] int chapterIndex;
    [SerializeField] Transform hero;
    public int ClusterCount { get; private set; }
    public int PlantCount { get; private set; }
    public int ReedCount { get; private set; }
    public int FlowerCount { get; private set; }
    public int VertexCount { get; private set; }
    public int RendererCount { get; private set; }
    public int RouteViolationCount { get; private set; }
    public int PlacementDigest { get; private set; }
    public float MotionTime { get; private set; }
    public bool Built { get; private set; }
    readonly List<Bounds> routes = new(), water = new(), obstacles = new();
    readonly List<Vector3> clearings = new();
    readonly List<Material> materials = new();
    readonly List<Mesh> meshes = new();
    readonly List<Vector3> placements = new();
    readonly RaycastHit[] groundHits = new RaycastHit[48];
    GameObject generated;
    Bounds groundBounds;
    float supportCeiling;
    System.Random random;
    readonly Bucket grass = new(), reeds = new(), flowers = new(), stones = new();
    sealed class Bucket {
        public readonly List<Vector3> vertices = new(); public readonly List<int> triangles = new();
        public readonly List<Color> colors = new(); public readonly List<Vector2> uv = new();
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color) {
            int n=vertices.Count; vertices.Add(a);vertices.Add(b);vertices.Add(c);
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
            colors.Add(color);colors.Add(color);colors.Add(color);
            uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.up);
        }
        public void Clear(){vertices.Clear();triangles.Clear();colors.Clear();uv.Clear();}
    }
    public void Configure(int chapter, Transform player){chapterIndex=Mathf.Clamp(chapter,0,6);hero=player;}
    void Start(){Build();}
    void Build(){
        if(Built)return;
        if(!hero){var actor=FindAnyObjectByType<MountedHorseController>();if(actor)hero=actor.transform;}
        if(!hero)return;
        Shader shader=Shader.Find("ThanhGiong/LegendSurface");if(!shader)return;
        random=new System.Random(71007+chapterIndex*997);supportCeiling=hero.position.y+.9f;
        ScanLayout();Physics.SyncTransforms();
        generated=new GameObject("Legend Landscape Clusters");generated.transform.SetParent(transform,true);
        generated.transform.position=Vector3.zero;generated.transform.rotation=Quaternion.identity;generated.transform.localScale=Vector3.one;
        // Authored rhythm around the entrance: open route, darker wings, small warm accents.
        for(int i=0;i<12;i++){
            float angle=i*Mathf.PI/6;Vector3 p=hero.position+new Vector3(Mathf.Cos(angle)*Next(7,16),0,Mathf.Sin(angle)*Next(7,16));
            Cluster(p,false,9+random.Next(5));
        }
        // River plants follow the narrow bank axis. Never place dressing on the bridge/water.
        foreach(var bank in water){
            bool narrowX=bank.size.x<bank.size.z;
            for(int i=0;i<20&&ClusterCount<ClusterBudget;i++){
                float along=(i+.5f)/20;float side=i%2==0?-1:1;
                Vector3 p=narrowX?new Vector3(bank.center.x+side*(bank.extents.x+Next(.5f,1.8f)),0,Mathf.Lerp(bank.min.z,bank.max.z,along)):
                    new Vector3(Mathf.Lerp(bank.min.x,bank.max.x,along),0,bank.center.z+side*(bank.extents.z+Next(.5f,1.8f)));
                Cluster(p,true,12+random.Next(5));
            }
        }
        // Cluster rather than uniform scatter; negative space and playable metrics stay intact.
        for(int attempt=0;attempt<180&&ClusterCount<ClusterBudget;attempt++){
            Vector3 p=new Vector3(Next(groundBounds.min.x+3,groundBounds.max.x-3),0,Next(groundBounds.min.z+3,groundBounds.max.z-3));
            Cluster(p,false,9+random.Next(6));
        }
        FinishBucket(grass,"Meadow / moss leaves",shader,3,.065f,1.1f);
        FinishBucket(reeds,"Bank reeds / seed heads",shader,3,.095f,2.3f);
        FinishBucket(flowers,"Wild flowers / warm route accents",shader,3,.025f,.8f);
        FinishBucket(stones,"Broken moss bank stones",shader,0,0,1);
        foreach(Vector3 p in placements){if(!Allowed(p,false))RouteViolationCount++;unchecked{PlacementDigest=PlacementDigest*31+Mathf.RoundToInt(p.x*100);PlacementDigest=PlacementDigest*31+Mathf.RoundToInt(p.z*100);}}
        Built=true;
    }
    void ScanLayout(){
        groundBounds=new Bounds(hero.position,new Vector3(48,1,48));bool hasGround=false;
        var renderers=FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        Array.Sort(renderers,(a,b)=>string.CompareOrdinal(Path(a.transform),Path(b.transform)));
        foreach(var renderer in renderers){
            if(renderer.gameObject.scene!=gameObject.scene||renderer is ParticleSystemRenderer||renderer is LineRenderer||renderer is SkinnedMeshRenderer||renderer.GetComponent<TextMesh>())continue;
            string name=Path(renderer.transform).ToLowerInvariant();Bounds bounds=renderer.bounds;
            if(name.Contains("datlang")||name.Contains("forest floor")||name.Contains("ground_plane")){
                if(!hasGround){groundBounds=bounds;hasGround=true;}else groundBounds.Encapsulate(bounds);continue;
            }
            if(name.Contains("dongsong")||name.Contains("riverwater")||name.Contains("waterplane")){water.Add(bounds);continue;}
            if(name.Contains("duongdatuon")||name.Contains("sandat")||name.Contains("sanchien")||name.Contains("baichien")||name.Contains("caug")||name.Contains("bridge")||name.Contains("cầu ván")){
                bounds.Expand(new Vector3(1.4f,0,1.4f));routes.Add(bounds);continue;
            }
            if(!renderer.GetComponentInParent<MountedHorseController>()&&!renderer.GetComponentInParent<ThanhGiongEnemy>()&&bounds.size.y>1.2f){bounds.Expand(new Vector3(.6f,0,.6f));obstacles.Add(bounds);}
        }
        clearings.Add(hero.position);
        foreach(var enemy in FindObjectsByType<ThanhGiongEnemy>(FindObjectsInactive.Include))clearings.Add(enemy.transform.position);
        foreach(var pickup in FindObjectsByType<ThanhGiongCollectible>(FindObjectsInactive.Include))clearings.Add(pickup.transform.position);
        foreach(var beacon in FindObjectsByType<LegendCheckpointBeacon>(FindObjectsInactive.Include))clearings.Add(beacon.transform.position);
    }
    void Cluster(Vector3 anchor,bool bank,int requested){
        if(ClusterCount>=ClusterBudget||PlantCount>=PlantBudget||!Allowed(anchor,true)||!Ground(anchor,out anchor))return;
        foreach(var previous in placements)if(SquaredXZ(previous,anchor)<5)return;
        int added=0;
        for(int i=0;i<requested&&PlantCount<PlantBudget&&VertexCount<VertexBudget-300;i++){
            float angle=Next(0,Mathf.PI*2),radius=Mathf.Sqrt(Next(0,1))*Next(.65f,1.55f);
            Vector3 p=anchor+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
            if(!Allowed(p,false)||!Ground(p,out p))continue;
            float size=Next(.65f,1.2f);
            if(bank&&i%3!=0){Reed(p,size);ReedCount++;}
            else if(i%5==0){Flower(p,size);FlowerCount++;}
            else if(i%7==0)Stone(p,size);
            else Grass(p,size);
            placements.Add(p);PlantCount++;added++;
        }
        if(added>0)ClusterCount++;
    }
    bool Allowed(Vector3 p,bool anchor){
        foreach(var bounds in routes)if(ContainsXZ(bounds,p))return false;
        foreach(var bounds in water)if(ContainsXZ(bounds,p))return false;
        foreach(var bounds in obstacles)if(ContainsXZ(bounds,p))return false;
        for(int i=0;i<clearings.Count;i++)if(SquaredXZ(p,clearings[i])<(i==0?20.25f:anchor?9:6.25f))return false;
        return true;
    }
    bool Ground(Vector3 p,out Vector3 supported){
        int count=Physics.RaycastNonAlloc(new Vector3(p.x,supportCeiling+8,p.z),Vector3.down,groundHits,40,~0,QueryTriggerInteraction.Ignore);
        float top=float.NegativeInfinity;supported=p;
        for(int i=0;i<count;i++){
            var hit=groundHits[i];if(!hit.collider||hit.normal.y<.85f||hit.point.y>supportCeiling)continue;
            if(hit.collider.attachedRigidbody||hit.collider.GetComponentInParent<MountedHorseController>()||hit.collider.GetComponentInParent<ThanhGiongEnemy>())continue;
            string name=Path(hit.transform).ToLowerInvariant();if(name.Contains("bridge")||name.Contains("caug")||name.Contains("water")||name.Contains("dongsong"))continue;
            if(hit.point.y>top){top=hit.point.y;supported=hit.point+Vector3.up*.025f;}
        }
        return !float.IsNegativeInfinity(top);
    }
    void Grass(Vector3 p,float size){
        Color tint=new Color(Next(.27f,.42f),Next(.43f,.58f),Next(.25f,.36f));
        for(int i=0;i<5;i++){
            float angle=i*1.2566f+Next(-.4f,.4f);Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            Vector3 side=Vector3.Cross(direction,Vector3.up)*(.09f*size),tip=p+direction*Next(.1f,.35f)*size+Vector3.up*Next(.36f,.72f)*size;
            Add(grass,p-side,p+side,tip,tint);
            Add(grass,p+direction*.12f-side*.6f,p+direction*.12f+side*.6f,tip+direction*.08f,tint*.94f);
        }
    }
    void Reed(Vector3 p,float size){
        float height=Next(1.2f,2.1f)*size;Vector3 top=p+Vector3.up*height+new Vector3(Next(-.1f,.1f),0,Next(-.1f,.1f));
        Color stem=new Color(.43f,.51f,.29f);Ribbon(reeds,p,top,.028f*size,stem);
        for(int i=0;i<3;i++){
            float angle=Next(0,Mathf.PI*2);Vector3 side=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            Vector3 basePoint=Vector3.Lerp(p,top,.25f+i*.2f);
            Add(reeds,basePoint-side*.035f,basePoint+side*.035f,basePoint+side*.55f*size+Vector3.up*.25f*size,stem*1.1f);
        }
        Ribbon(reeds,top-Vector3.up*.27f*size,top+Vector3.up*.08f*size,.085f*size,new Color(.47f,.36f,.22f));
    }
    void Flower(Vector3 p,float size){
        Vector3 center=p+Vector3.up*Next(.35f,.62f)*size;
        Ribbon(flowers,p,center,.018f,new Color(.30f,.43f,.24f));
        Color petal=chapterIndex==5?new Color(.90f,.81f,.55f):random.Next(3)==0?new Color(.65f,.60f,.74f):new Color(.87f,.82f,.61f);
        for(int i=0;i<5;i++){
            float angle=i*Mathf.PI*2/5;Vector3 tip=center+new Vector3(Mathf.Cos(angle),.035f,Mathf.Sin(angle))*.17f*size;
            Vector3 side=new Vector3(-Mathf.Sin(angle),0,Mathf.Cos(angle))*.065f*size;
            Add(flowers,center,tip+side,tip-side,petal);
        }
        Add(flowers,center+new Vector3(-.06f,.012f,-.035f),center+new Vector3(.06f,.012f,-.035f),center+new Vector3(0,.012f,.07f),new Color(.86f,.65f,.25f));
    }
    void Stone(Vector3 p,float size){
        float r=.26f*size;Vector3 a=p+new Vector3(-r,0,-r*.6f),b=p+new Vector3(r,0,-r*.4f),c=p+new Vector3(.12f,0,r),top=p+Vector3.up*.16f*size;
        Color color=new Color(.40f,.44f,.34f);Add(stones,a,top,b,color);Add(stones,b,top,c,color*.90f);Add(stones,c,top,a,color*1.08f);
    }
    void Ribbon(Bucket bucket,Vector3 bottom,Vector3 top,float width,Color tint){
        foreach(Vector3 side in new[]{Vector3.right*width,Vector3.forward*width}){Add(bucket,bottom-side,bottom+side,top+side,tint);Add(bucket,bottom-side,top+side,top-side,tint);}
    }
    void Add(Bucket bucket,Vector3 a,Vector3 b,Vector3 c,Color tint){bucket.Triangle(a,b,c,tint);VertexCount+=3;}
    void FinishBucket(Bucket bucket,string label,Shader shader,int mode,float amplitude,float height){
        if(bucket.vertices.Count==0)return;
        Mesh mesh=new Mesh{name="Legend "+label};mesh.SetVertices(bucket.vertices);mesh.SetTriangles(bucket.triangles,0);mesh.SetColors(bucket.colors);mesh.SetUVs(0,bucket.uv);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        Material material=new Material(shader){name="Legend "+label};material.SetColor("_BaseColor",Color.white);material.SetFloat("_UseVertexColors",1);material.SetFloat("_Mode",mode);material.SetFloat("_Amplitude",amplitude);material.SetFloat("_Height",height);materials.Add(material);
        var go=new GameObject(label);go.transform.SetParent(generated.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;RendererCount++;
        bucket.Clear();
    }
    void Update(){if(!Built||Time.timeScale<=0)return;MotionTime+=Time.deltaTime;foreach(var material in materials)material.SetFloat("_MotionTime",MotionTime);}
    void OnDisable(){if(generated)generated.SetActive(false);}
    void OnEnable(){if(generated)generated.SetActive(true);}
    void OnDestroy(){foreach(var mesh in meshes)if(mesh)Destroy(mesh);foreach(var material in materials)if(material)Destroy(material);if(generated)Destroy(generated);}
    float Next(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
    static bool ContainsXZ(Bounds b,Vector3 p)=>p.x>=b.min.x&&p.x<=b.max.x&&p.z>=b.min.z&&p.z<=b.max.z;
    static float SquaredXZ(Vector3 a,Vector3 b){float x=a.x-b.x,z=a.z-b.z;return x*x+z*z;}
    static string Path(Transform t){string p=t.name;for(var parent=t.parent;parent;parent=parent.parent)p=parent.name+"/"+p;return p;}
}
