using System; using System.Collections.Generic; using System.Linq; using System.Reflection; using UnityEngine; using GasQueue;
public static class Sim { public static bool Verbose; public static int HonkedAt;
 static void Main(string[] args){
  float speedup = args.Length>0? float.Parse(args[0]) : 2f; int seed = args.Length>1? int.Parse(args[1]) : 1; float dur = args.Length>2? float.Parse(args[2]) : 1500; PlayerCar.Sneaky = args.Length>3 && args[3]=="sneaky";
  UnityEngine.Random.R=new System.Random(seed);
  var game=new GameObject("Game"); var settings=game.AddComponent<GameSettings>(); settings.testSpeedup=speedup;
  var root=new GameObject("World").transform;
  var bgo=new GameObject("Barrier"); bgo.transform.position=new Vector3(CityLayout.BarrierPostX,0,CityLayout.BarrierZ); var barrier=bgo.AddComponent<Barrier>();
  var pgo=new GameObject("PLAYER"); var pv=pgo.AddComponent<CarVisual>(); pv.length=4.3f; var player=pgo.AddComponent<PlayerCar>(); player.visual=pv;
  var tgo=new GameObject("Traffic"); var traffic=tgo.AddComponent<TrafficManager>(); player.traffic=traffic;
  var ggo=new GameObject("GM"); var gm=ggo.AddComponent<GameManager>(); gm.barrier=barrier; GameManager.Instance=gm; gm.delivery=settings.DeliveryDuration;
  traffic.Init(settings,player,barrier,root);
  var lastMove=new Dictionary<NpcCar,(Vector3 p,float t)>(); int served=0; var exiting=new HashSet<NpcCar>();
  var cache=new Dictionary<Type,MethodInfo>();
  float dt=Time.deltaTime; float nextReport=0;
  for(Time.time=0; Time.time<dur && !player.done; Time.time+=dt){
    var list=GameObject.All.Where(c=>!c.destroyed && c is MonoBehaviour && c.gameObject.activeSelf).OrderBy(c=>c.GetType().GetCustomAttribute<DefaultExecutionOrder>()?.order??0).ToList();
    foreach(var c in list){ if(c.destroyed) continue; var ty=c.GetType(); if(!cache.TryGetValue(ty,out var m)){ m=ty.GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public); cache[ty]=m; } m?.Invoke(c,null); }
    foreach(var n in traffic.Npcs){ if(n.Role==NpcRole.Exiting) exiting.Add(n); }
    foreach(var n in exiting.ToList()) if(n.destroyed){ served++; exiting.Remove(n);} 
    GameObject.All.RemoveAll(c=>c.destroyed);
    foreach(var n in traffic.Npcs){ if(!lastMove.TryGetValue(n,out var lm) || (n.Position-lm.p).magnitude>0.3f) lastMove[n]=(n.Position,Time.time); }
    if(Time.time>=nextReport){ nextReport+=60;
      var roles=traffic.Npcs.GroupBy(n=>n.Role).Select(g=>$"{g.Key}:{g.Count()}");
      Console.WriteLine($"t={Time.time:F0} served={served} playerIdx={traffic.PlayerQueueIndex} inQ={traffic.PlayerInQueue} pumps=[{string.Join(",",traffic.Pumps.Select(p=>p.Occupant==null?(p.reservedForPlayer?"P":"-"):p.Occupant.Role.ToString()))}] barrier={(barrier.IsDown?"DOWN":"up")} {string.Join(" ",roles)}");
      var stuck=traffic.Npcs.Where(n=>n.Role!=NpcRole.Queue && n.Role!=NpcRole.Fueling && Time.time-lastMove[n].t>40).ToList();
      foreach(var n in stuck) Console.WriteLine($"   STUCK {n.gameObject.name} {n.Role} path={n.Path.name} s={n.S:F1}/{n.Path.Length:F1} pos=({n.Position.x:F1},{n.Position.z:F1}) blockedBy={(n.BlockedBy==null?"-":n.BlockedBy.gameObject.name)} cut={n.Cut}");
      var head=traffic.Npcs.Where(n=>n.Role==NpcRole.Queue&&n.Path==traffic.QueuePath).OrderByDescending(n=>n.S).FirstOrDefault();
      if(head!=null) Console.WriteLine($"   head {head.gameObject.name} s={head.S:F2}/{traffic.QueuePath.Length:F2} speed={head.Speed:F2} blockedBy={(head.BlockedBy==null?"-":head.BlockedBy.gameObject.name)} idle={Time.time-lastMove[head].t:F0}s");
    }
  }
  Console.WriteLine($"END t={Time.time:F0} served={served} playerDone={player.done} fuelRanOut={gm.FuelRanOut}");
 } }
