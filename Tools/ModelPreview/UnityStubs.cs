using System;
// Минимальные заглушки UnityEngine для сборки моделей вне редактора
namespace UnityEngine {
public static class Mathf {
 public const float PI=(float)Math.PI, Rad2Deg=57.29578f, Deg2Rad=0.017453292f;
 public static float Sin(float a)=>(float)Math.Sin(a); public static float Cos(float a)=>(float)Math.Cos(a); public static float Tan(float a)=>(float)Math.Tan(a);
 public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x); public static float Acos(float a)=>(float)Math.Acos(a); public static float Asin(float a)=>(float)Math.Asin(a);
 public static float Sqrt(float a)=>(float)Math.Sqrt(a); public static float Pow(float a,float b)=>(float)Math.Pow(a,b); public static float Exp(float a)=>(float)Math.Exp(a);
 public static float Abs(float a)=>Math.Abs(a); public static int Abs(int a)=>Math.Abs(a); public static float Sign(float a)=>a>=0?1f:-1f;
 public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); public static int Min(int a,int b)=>Math.Min(a,b); public static int Max(int a,int b)=>Math.Max(a,b);
 public static float Clamp(float v,float a,float b)=>v<a?a:v>b?b:v; public static int Clamp(int v,int a,int b)=>v<a?a:v>b?b:v; public static float Clamp01(float v)=>v<0?0:v>1?1:v;
 public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t); public static float LerpUnclamped(float a,float b,float t)=>a+(b-a)*t; public static float InverseLerp(float a,float b,float v)=>a==b?0:Clamp01((v-a)/(b-a));
 public static float SmoothStep(float a,float b,float t){t=Clamp01(t);t=t*t*(3-2*t);return a+(b-a)*t;}
 public static int RoundToInt(float f)=>(int)Math.Round(f); public static int FloorToInt(float f)=>(int)Math.Floor(f); public static int CeilToInt(float f)=>(int)Math.Ceiling(f); public static float Floor(float f)=>(float)Math.Floor(f);
 public static float Repeat(float t,float l)=>Clamp(t-(float)Math.Floor(t/l)*l,0,l); public static float MoveTowards(float c,float t,float d)=>Math.Abs(t-c)<=d?t:c+Math.Sign(t-c)*d; public static bool Approximately(float a,float b)=>Math.Abs(a-b)<1e-6f;
}
public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
 public static Vector2 zero=>new Vector2(0,0); public static Vector2 one=>new Vector2(1,1); public static Vector2 up=>new Vector2(0,1); public static Vector2 right=>new Vector2(1,0);
 public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y); public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y); public static Vector2 operator-(Vector2 a)=>new Vector2(-a.x,-a.y);
 public static Vector2 operator*(Vector2 a,float f)=>new Vector2(a.x*f,a.y*f); public static Vector2 operator*(float f,Vector2 a)=>a*f; public static Vector2 operator/(Vector2 a,float f)=>new Vector2(a.x/f,a.y/f);
 public float sqrMagnitude=>x*x+y*y; public float magnitude=>(float)Math.Sqrt(sqrMagnitude); public Vector2 normalized{get{float m=magnitude;return m>1e-12f?this/m:zero;}}
 public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y; public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a+(b-a)*Mathf.Clamp01(t); public static float Distance(Vector2 a,Vector2 b)=>(a-b).magnitude;
 public static bool operator==(Vector2 a,Vector2 b)=>(a-b).sqrMagnitude<1e-12f; public static bool operator!=(Vector2 a,Vector2 b)=>!(a==b); public override bool Equals(object o)=>o is Vector2 v&&v==this; public override int GetHashCode()=>x.GetHashCode()^y.GetHashCode();
 public override string ToString()=>$"({x:F3},{y:F3})"; }
public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
 public static Vector3 zero=>new Vector3(0,0,0); public static Vector3 one=>new Vector3(1,1,1); public static Vector3 up=>new Vector3(0,1,0); public static Vector3 down=>new Vector3(0,-1,0);
 public static Vector3 right=>new Vector3(1,0,0); public static Vector3 left=>new Vector3(-1,0,0); public static Vector3 forward=>new Vector3(0,0,1); public static Vector3 back=>new Vector3(0,0,-1);
 public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); public static Vector3 operator-(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);
 public static Vector3 operator*(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f); public static Vector3 operator*(float f,Vector3 a)=>a*f; public static Vector3 operator/(Vector3 a,float f)=>new Vector3(a.x/f,a.y/f,a.z/f);
 public float sqrMagnitude=>x*x+y*y+z*z; public float magnitude=>(float)Math.Sqrt(sqrMagnitude); public Vector3 normalized{get{float m=magnitude;return m>1e-12f?this/m:zero;}}
 public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x); public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
 public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z); public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t); public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
 public static Vector3 Normalize(Vector3 v)=>v.normalized;
 public static bool operator==(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<1e-12f; public static bool operator!=(Vector3 a,Vector3 b)=>!(a==b); public override bool Equals(object o)=>o is Vector3 v&&v==this; public override int GetHashCode()=>x.GetHashCode()^y.GetHashCode()^z.GetHashCode();
 public override string ToString()=>$"({x:F3},{y:F3},{z:F3})"; }
}
