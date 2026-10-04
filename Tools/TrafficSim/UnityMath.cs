using System;
namespace UnityEngine {
public static class Mathf { public const float PI=(float)Math.PI; public const float Rad2Deg=57.29578f; public const float Deg2Rad=0.017453292f;
 public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x); public static float Tan(float a)=>(float)Math.Tan(a); public static float Sin(float a)=>(float)Math.Sin(a); public static float Cos(float a)=>(float)Math.Cos(a); public static float Exp(float a)=>(float)Math.Exp(a);
 public static float Repeat(float t,float l)=>Clamp(t-(float)Math.Floor(t/l)*l,0,l); public static float MoveTowards(float c,float t,float d)=>Math.Abs(t-c)<=d?t:c+Math.Sign(t-c)*d; public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
 public static int RoundToInt(float f)=>(int)Math.Round(f); public static float Floor(float f)=>(float)Math.Floor(f); public static float Ceil(float f)=>(float)Math.Ceiling(f); public static bool Approximately(float a,float b)=>Math.Abs(a-b)<1e-5f; public static float SmoothStep(float a,float b,float t){t=Clamp01(t);t=t*t*(3-2*t);return a+(b-a)*t;} public static float Sqrt(float f)=>(float)Math.Sqrt(f); public static float Abs(float f)=>Math.Abs(f); public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b); public static int Min(int a,int b)=>Math.Min(a,b);
 public static float Clamp01(float v)=>v<0?0:v>1?1:v; public static float Clamp(float v,float a,float b)=>v<a?a:v>b?b:v; public static int Clamp(int v,int a,int b)=>v<a?a:v>b?b:v; public static int CeilToInt(float f)=>(int)Math.Ceiling(f); public static float Sign(float f)=>f>=0?1:-1; }
public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
 public static Vector3 forward=>new Vector3(0,0,1); public static Vector3 right=>new Vector3(1,0,0); public static Vector3 up=>new Vector3(0,1,0); public static Vector3 zero=>new Vector3(0,0,0); public static Vector3 one=>new Vector3(1,1,1); public static Vector3 back=>new Vector3(0,0,-1); public static Vector3 left=>new Vector3(-1,0,0);
 public float magnitude=>(float)Math.Sqrt(sqrMagnitude); public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z); public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
 public static Vector3 MoveTowards(Vector3 c,Vector3 t,float d){var v=t-c;float m=v.magnitude;return m<=d||m==0?t:c+v/m*d;}
 public static bool operator==(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<1e-10f; public static bool operator!=(Vector3 a,Vector3 b)=>!(a==b); public override bool Equals(object o)=>o is Vector3 v&&v==this; public override int GetHashCode()=>0;
 public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); public static Vector3 operator-(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);
 public static Vector3 operator*(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f); public static Vector3 operator*(float f,Vector3 a)=>a*f; public static Vector3 operator/(Vector3 a,float f)=>new Vector3(a.x/f,a.y/f,a.z/f);
 public float sqrMagnitude=>x*x+y*y+z*z; public Vector3 normalized=>this/(float)Math.Sqrt(sqrMagnitude);
 public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z; public static float Distance(Vector3 a,Vector3 b)=>(float)Math.Sqrt((a-b).sqrMagnitude);
 public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t; public override string ToString()=>$"({x:F2},{y:F2},{z:F2})"; }
public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
 public static Vector2 zero=>new Vector2(0,0); public static Vector2 operator*(float f,Vector2 a)=>a*f;
 public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y); public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y); public static Vector2 operator-(Vector2 a)=>new Vector2(-a.x,-a.y);
 public static Vector2 operator*(Vector2 a,float f)=>new Vector2(a.x*f,a.y*f); public static Vector2 operator/(Vector2 a,float f)=>new Vector2(a.x/f,a.y/f);
 public float sqrMagnitude=>x*x+y*y; public float magnitude=>(float)Math.Sqrt(sqrMagnitude); public Vector2 normalized=>this/magnitude; public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y; public override string ToString()=>$"({x:F2},{y:F2})"; }
}
