using System; using System.Collections.Generic; using UnityEngine;
namespace GasQueue {
public enum CarModel { Vaz2107, Rio, Niva, Gazelle }
public enum BrawlReason { Crash, CutIn }
public enum VendorKind { Canister, Pies }
public static class CarModels { public static CarModel Random(out bool taxi){ taxi=UnityEngine.Random.value<0.2f; return (CarModel)UnityEngine.Random.Range(0,4);} public static Color RandomPaint(CarModel m,bool t)=>Color.white; }
public class Brawler : MonoBehaviour { public static Brawler Active; public static Brawler Spawn(NpcCar c,TrafficManager t,BrawlReason r){ GameManager.Log($"brawler out ({r})"); return null; } }
public class Vendor : MonoBehaviour { public VendorKind Kind; float life; void Update(){ life+=Time.deltaTime; transform.position+=Vector3.forward*1.1f*Time.deltaTime; if(life>60){ UnityEngine.Object.Destroy(gameObject);} } public static Vendor Spawn(VendorKind k,TrafficManager t,float z,float d){ var go=new GameObject("Vendor"); var v=go.AddComponent<Vendor>(); v.Kind=k; go.transform.position=new Vector3(7.25f,0,z); t.Pedestrians.Add(go.transform); return v; } }
public enum GameState { Queueing, OutOfFuel, DrivingAway, Finished }
public class CarVisual : MonoBehaviour { public float length, width=1.86f, height=1.5f; public Vector3 driverDoorLocal=new Vector3(-1.45f,0,-0.45f); public int blinker; public Transform driverHead, driverTorso; public void Roll(float d){} public void Bounce(){} }
public static class CarFactory {
 public static CarVisual Build(string name,Color c,CarModel m,bool p,bool taxi=false){ var go=new GameObject(name); var v=go.AddComponent<CarVisual>(); v.length= m==CarModel.Gazelle?5.5f:m==CarModel.Niva?3.74f:m==CarModel.Rio?4.4f:4.14f; v.width=m==CarModel.Gazelle?2.06f:1.8f; return v; } }
public class CarDamage : MonoBehaviour { public float Front,Rear; public void Init(CarVisual v,Transform t){} public string Hit(bool f,float s,Vector3 v)=>null; }
public static class SoundFactory { public static AudioClip Horn=new AudioClip(); public static AudioSource Source3D(GameObject g,float v=1,float m=120)=>g.AddComponent<AudioSource>(); }
public static class SpeechBubble { public static int Count; public static void Show(Transform t,string p,float h){Count++; if (Sim.Verbose) Console.WriteLine($"  [{Time.time:F0}] {t.gameObject.name}: {p}");} }
public static class AngryDriver { public static void Spawn(NpcCar c,PlayerCar p,Transform r,float s){} }
public class Barrier : MonoBehaviour { public bool IsDown {get; private set;} public void SetDown(bool d)=>IsDown=d; public Obb Box=>Obb.Axis(transform.position+Vector3.left*3.4f,6.8f,0.3f); }
public class WalkerController : MonoBehaviour { public bool Active=>false; public Vector2 Position2=>Vector2.zero; public Obb Box=>default; }
public class GameManager : MonoBehaviour { public static GameManager Instance; public GameState State=GameState.Queueing; public bool FuelRanOut, PlayerFueled; public Barrier barrier; float t; public float delivery=60f;
 public void TriggerOutOfFuel(){FuelRanOut=true; State=GameState.OutOfFuel; barrier.SetDown(true); t=0; Log("OUT OF FUEL");}
 void Update(){ if(State==GameState.OutOfFuel){ t+=Time.deltaTime; if(t>delivery){barrier.SetDown(false); State=GameState.Queueing; Log("DELIVERY");} } }
 public static void Log(string s)=>Console.WriteLine($"[{Time.time:F0}s] {s}");
 public void OnPlayerGranted(Pump p)=>Log($"player granted pump {p.Number}"); public void ShowMessage(string s,float d=6){ if(Sim.Verbose) Log("MSG "+s);}
 public void OnSomeoneGaveUp(bool a)=>Log("gave up"); public void ShowMessage(string s)=>ShowMessage(s,6); public void OnPlayerHonkedAt(){Sim.HonkedAt++;} public void OnCutInBlocked()=>Log("cut-in blocked"); public void OnPlayerCutIn()=>Log("player cut-in!"); public void OnPlayerSqueezedIn()=>Log("player squeezed"); }
// Игрок-бот: стоит в очереди как человек, заезжает на выданную колонку, «заправляется», уезжает
public class PlayerCar : Vehicle { public override bool IsPlayer=>true; LanePath path; float s; int phase; float wait; public bool done; public static bool Sneaky; float lat; bool snuck;
 public void PlaceOnPath(LanePath p,float s0){path=p;s=s0;Place(p.PointAt(s0),p.TangentAt(s0));}
 void Update(){ float dt=Time.deltaTime; var gm=GameManager.Instance; float free=float.MaxValue;
  // Сценарий «выехал из очереди и встраиваюсь ближе к заправке»
  if(Sneaky && !snuck && phase==0 && Time.time>20){ phase=-1; GameManager.Log("player leaves queue"); }
  if(phase==-1){ // выезжаем влево и едем по соседнему ряду
    lat=Mathf.MoveTowards(lat,-3.5f,1.5f*dt); float f2=float.MaxValue; foreach(var n in traffic.Npcs){ if(Obb.AheadDistance(Box,n.Box,1.2f,20f,out float d2)) f2=Mathf.Min(f2,d2-2f);} 
    float st=Mathf.Clamp(f2,0,5f*dt); s+=st; Speed=st/dt; Place(path.PointAt(s)+path.RightAt(s)*lat, path.TangentAt(s));
    if(lat<=-3.49f && path.PointAt(s).z>-70f){ phase=-2; GameManager.Log($"player looking for gap at z={path.PointAt(s).z:F0}"); } return; }
  if(phase==-2){ // ждём дырку в очереди рядом и вклиниваемся
    float gapFront=float.MaxValue, gapBack=float.MinValue; foreach(var n in traffic.Npcs){ if(n.Path!=path||n.Role!=NpcRole.Queue) continue; if(n.S>s) gapFront=Mathf.Min(gapFront,n.S-n.Length/2); else gapBack=Mathf.Max(gapBack,n.S+n.Length/2);} 
    bool fits = gapFront-(s+Length/2)>0.8f && (s-Length/2)-gapBack>0.8f; wait+=dt;
    if(fits || lat>-3.49f){ lat=Mathf.MoveTowards(lat,0,1.5f*dt); Place(path.PointAt(s)+path.RightAt(s)*lat, path.TangentAt(s)); if(Mathf.Abs(lat)<0.01f){phase=0;snuck=true;wait=0;GameManager.Log($"player merged back at z={path.PointAt(s).z:F0}, idx={traffic.PlayerQueueIndex}");} }
    else if(wait>8){ s+=0.5f; }
    Speed=0; return; }
  if(phase==0){ foreach(var n in traffic.Npcs) if(n.Path==path && n.Role==NpcRole.Queue && n.S>s && Mathf.Abs(n.Offset)<1.2f) free=Mathf.Min(free,(n.S-n.Length/2)-(s+Length/2)-2.2f);
     free=Mathf.Min(free,path.Length-s); if(traffic.PlayerPump!=null){ path=traffic.PlayerPump.enterPath; s=0; phase=1; free=0; GameManager.Log("player -> pump"); } }
  else if(phase==1){ free=path.Length-s; if(free<0.05f){ wait+=dt; if(wait>30){ gm.PlayerFueled=true; path=traffic.PlayerPump.exitPath; s=0; phase=2; traffic.ReleasePlayerPump(); GameManager.Log("player fueled, leaving"); } } }
  else { free=path.Length-s; if(free<1){done=true;} }
  // избегаем NPC прямо перед собой
  foreach(var n in traffic.Npcs){ if(Obb.AheadDistance(Box,n.Box,1.2f,20f,out float d)) free=Mathf.Min(free,d-2f); }
  float step=Mathf.Clamp(free,0,4f*dt); Speed=step/dt; s+=step; Place(path.PointAt(s),path.TangentAt(s)); } }
}
