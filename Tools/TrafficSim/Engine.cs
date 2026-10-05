using System; using System.Collections.Generic; using System.Reflection; using System.Linq;
namespace UnityEngine {
public class DefaultExecutionOrder : Attribute { public int order; public DefaultExecutionOrder(int o){order=o;} }
public class RangeAttribute : Attribute { public RangeAttribute(float a,float b){} }
public class TooltipAttribute : Attribute { public TooltipAttribute(string s){} }
public class HeaderAttribute : Attribute { public HeaderAttribute(string s){} }
public class SerializeField : Attribute {} public class HideInInspector : Attribute {}
public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;} public static Color white=>new Color(1,1,1); public static Color red=>new Color(1,0,0); }
public struct Quaternion { public float x,y,z,w; public Quaternion(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}
 public static Quaternion identity=>new Quaternion(0,0,0,1);
 static Quaternion Axis(Vector3 a,float deg){float h=deg*Mathf.Deg2Rad/2;float s=Mathf.Sin(h);return new Quaternion(a.x*s,a.y*s,a.z*s,Mathf.Cos(h));}
 public static Quaternion Euler(float x,float y,float z)=>Axis(Vector3.up,y)*Axis(Vector3.right,x)*Axis(Vector3.forward,z);
 public static Quaternion Euler(Vector3 e)=>Euler(e.x,e.y,e.z);
 public static Quaternion operator*(Quaternion a,Quaternion b)=>new Quaternion(a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y, a.w*b.y+a.y*b.w+a.z*b.x-a.x*b.z, a.w*b.z+a.z*b.w+a.x*b.y-a.y*b.x, a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z);
 public static Vector3 operator*(Quaternion q,Vector3 v){var u=new Vector3(q.x,q.y,q.z);var t=2f*Vector3.Cross(u,v);return v+q.w*t+Vector3.Cross(u,t);}
 public static Quaternion LookRotation(Vector3 f)=>LookRotation(f,Vector3.up);
 public static Quaternion LookRotation(Vector3 f,Vector3 up){ f=f.normalized; float yaw=Mathf.Atan2(f.x,f.z)*Mathf.Rad2Deg; float pitch=-Mathf.Atan2(f.y,(float)Math.Sqrt(f.x*f.x+f.z*f.z))*Mathf.Rad2Deg; return Euler(pitch,yaw,0);} }
public static class Random { public static System.Random R=new System.Random(1); public static float value=>(float)R.NextDouble(); public static float Range(float a,float b)=>a+(b-a)*value; public static int Range(int a,int b)=>R.Next(a,b); public static Vector3 insideUnitSphere=>new Vector3(value-.5f,value-.5f,value-.5f); }
public static class Time { public static float deltaTime=0.05f; public static float time; }
public class Object { internal bool destroyed; public string name;
 public static bool operator==(Object a,Object b){bool an=ReferenceEquals(a,null)||a.destroyed; bool bn=ReferenceEquals(b,null)||b.destroyed; if(an||bn) return an&&bn; return ReferenceEquals(a,b);} public static bool operator!=(Object a,Object b)=>!(a==b);
 public override bool Equals(object o)=>ReferenceEquals(this,o); public override int GetHashCode()=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
 public static void Destroy(Object o){ if(o is GameObject g){ g.destroyed=true; foreach(var c in g.comps) c.destroyed=true; g.transform.destroyed=true; } else if(o!=null) o.destroyed=true; } }
public class Component : Object { public GameObject gameObject; public Transform transform=>gameObject.transform; public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>(); }
public class Behaviour : Component { public bool enabled=true; }
public class MonoBehaviour : Behaviour { public void Invoke(string m,float t){} }
public class Transform : Component { public Vector3 position; public Quaternion rotation=Quaternion.identity; public Vector3 localScale=Vector3.one; public Transform parent;
 public Vector3 forward=>rotation*Vector3.forward; public Vector3 right=>rotation*Vector3.right;
 public void SetParent(Transform p,bool w=true){parent=p;} public void SetPositionAndRotation(Vector3 p,Quaternion r){position=p;rotation=r;}
 public Vector3 TransformPoint(Vector3 p)=>position+rotation*Vector3.Scale(localScale,p); }
public class GameObject : Object { public static List<Component> All=new List<Component>(); internal List<Component> comps=new List<Component>(); public Transform transform; bool active=true;
 public bool activeSelf=>active; public bool activeInHierarchy=>active && !destroyed; public void SetActive(bool a){active=a;}
 public GameObject(string n=""){name=n; transform=new Transform{gameObject=this}; comps.Add(transform);}
 public T AddComponent<T>() where T:Component,new(){ var c=new T{gameObject=this}; comps.Add(c); All.Add(c); var m=typeof(T).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public); m?.Invoke(c,null); return c; }
 public T GetComponent<T>() where T:Component=>comps.OfType<T>().FirstOrDefault(); }
public class AudioClip : Object {}
public class AudioSource : Behaviour { public AudioClip clip; public float pitch=1,volume=1,spatialBlend; public bool loop; public void Play(){} public void Stop(){} public void PlayOneShot(AudioClip c,float v=1){} }
}
