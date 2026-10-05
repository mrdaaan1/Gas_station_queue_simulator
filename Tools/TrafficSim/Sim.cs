using System; using System.Collections.Generic; using System.Linq; using System.Reflection; using UnityEngine; using GasQueue;
public static class Sim { public static bool Verbose; public static int HonkedAt;
 static void Main(string[] args){
  if(args.Length>0 && args[0]=="track"){ // точки трассы для картинки: ось, полосы, стены, профиль скорости
    var c=new LanePath("c",1,RaceLayout.Center(),false); var L=RaceLayout.Lane(false); var R=RaceLayout.Lane(true); var prof=new SpeedProfile(R,RaceLayout.TopSpeed,RaceLayout.CornerGrip,RaceLayout.Braking);
    foreach(var (name,path) in new[]{("C",c),("L",L),("R",R),("T",RaceLayout.TrafficLane())}) for(float q=0;q<path.Length;q+=2){var pt=path.PointAt(q); Console.WriteLine($"{name} {pt.x:F2} {pt.z:F2} {(name=="R"?prof.At(q):0):F1}");}
    foreach(float side in new[]{-1f,1f}){ var w=RaceLayout.Offset(RaceLayout.Center(),side*(RaceLayout.HalfWidth+0.35f)); foreach(var pt in w) Console.WriteLine($"W {pt.x:F2} {pt.z:F2} 0"); }
    Console.WriteLine($"LEN {c.Length:F0} {R.Length:F0}"); return; }
  float speedup = args.Length>0? float.Parse(args[0]) : 2f; int seed = args.Length>1? int.Parse(args[1]) : 1; float dur = args.Length>2? float.Parse(args[2]) : 1500; PlayerCar.Sneaky = args.Length>3 && args[3]=="sneaky"; bool race = args.Length>3 && args[3]=="race"; if (args.Length>4) Time.deltaTime = float.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture);
  UnityEngine.Random.R=new System.Random(seed); Verbose = System.Environment.GetEnvironmentVariable("SIM_VERBOSE")!=null;
  var game=new GameObject("Game"); var settings=game.AddComponent<GameSettings>(); settings.testSpeedup=speedup;
  var root=new GameObject("World").transform;
  var bgo=new GameObject("Barrier"); bgo.transform.position=new Vector3(CityLayout.BarrierPostX,0,CityLayout.BarrierZ); var barrier=bgo.AddComponent<Barrier>();
  var pgo=new GameObject("PLAYER"); var pv=pgo.AddComponent<CarVisual>(); pv.length=4.3f; var player=pgo.AddComponent<PlayerCar>(); player.visual=pv;
  var tgo=new GameObject("Traffic"); var traffic=tgo.AddComponent<TrafficManager>(); player.traffic=traffic;
  var ggo=new GameObject("GM"); var gm=ggo.AddComponent<GameManager>(); gm.barrier=barrier; GameManager.Instance=gm; gm.delivery=settings.DeliveryDuration;
  traffic.Init(settings,player,barrier,root,race);
  var racerLog=new Dictionary<NpcCar,string>(); traffic.RacerFinished+=r=>GameManager.Log($"FINISH {traffic.FinishOrder.Count}. {r.RacerName}");
  var lastMove=new Dictionary<NpcCar,(Vector3 p,float t)>(); int served=0; var exiting=new HashSet<NpcCar>();
  var gasIn=new HashSet<NpcCar>(); var gasFuel=new HashSet<NpcCar>(); var gasOut=new HashSet<NpcCar>(); var wasCutter=new HashSet<NpcCar>(); int cutIns=0; int overlapFrames=0; float worstOverlap=0;
  var cache=new Dictionary<Type,MethodInfo>();
  float dt=Time.deltaTime; float nextReport=0;
  for(Time.time=0; Time.time<dur && !player.done; Time.time+=dt){
    if(race && !traffic.RaceStarted && Time.time>4f){ traffic.StartRace(); GameManager.Log("GREEN"); }
    if(race) foreach(var r in traffic.Racers){ if(r.destroyed) continue; string st=r.Role+"/"+r.Path?.name; if(!racerLog.TryGetValue(r,out var was) || was!=st){ racerLog[r]=st; if(Verbose || r.Role!=NpcRole.Racing) GameManager.Log($"  {r.RacerName}: {st} z={r.Position.z:F0} v={r.Speed:F1}"); } }
    var list=GameObject.All.Where(c=>!c.destroyed && c is MonoBehaviour && c.gameObject.activeSelf).OrderBy(c=>c.GetType().GetCustomAttribute<DefaultExecutionOrder>()?.order??0).ToList();
    foreach(var c in list){ if(c.destroyed) continue; var ty=c.GetType(); if(!cache.TryGetValue(ty,out var m)){ m=ty.GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public); cache[ty]=m; } m?.Invoke(c,null); }
    foreach(var n in traffic.Npcs){ if(n.IsGas){ if(n.Role==NpcRole.ToGas) gasIn.Add(n); if(n.Role==NpcRole.GasFueling) gasFuel.Add(n); if(n.Role==NpcRole.Exiting) gasOut.Add(n);} if(n.Role==NpcRole.Exiting) exiting.Add(n); if(n.Role==NpcRole.Cutter) wasCutter.Add(n); else if(wasCutter.Remove(n) && n.Role==NpcRole.Queue) cutIns++; }
    var trace=System.Environment.GetEnvironmentVariable("SIM_TRACE");
    if(trace!=null && Mathf.Repeat(Time.time,2f)<dt) foreach(var n in traffic.Npcs.Where(n=>n.gameObject.name==trace)) Console.WriteLine($"  TRACE t={Time.time:F1} {n.Role} {n.Path?.name} s={n.S:F2} pos=({n.Position.x:F2},{n.Position.z:F2}) speed={n.Speed:F2} fwd=({n.Forward.x:F2},{n.Forward.z:F2})");
    // Наползание машин друг на друга (глубже 15 см)
    var ns=traffic.Npcs; bool anyOverlap=false;
    for(int i=0;i<ns.Count;i++) for(int j=i+1;j<ns.Count;j++){ var d=ns[i].Position-ns[j].Position; if(d.x*d.x+d.z*d.z>49) continue; if(Obb.Overlap(ns[i].Box,ns[j].Box,out var m) && m.magnitude>0.15f){ anyOverlap=true; if(m.magnitude>0.6f && System.Environment.GetEnvironmentVariable("SIM_OVERLAP")!=null) Console.WriteLine($"[{Time.time:F1}] overlap {m.magnitude:F2} {ns[i].name} {ns[i].Role} {ns[i].Path?.name} pos=({ns[i].Position.x:F1},{ns[i].Position.z:F1}) vs {ns[j].name} {ns[j].Role} {ns[j].Path?.name} pos=({ns[j].Position.x:F1},{ns[j].Position.z:F1})"); worstOverlap=Mathf.Max(worstOverlap,m.magnitude);} }
    if(anyOverlap) overlapFrames++;
    foreach(var n in exiting.ToList()) if(n.destroyed){ served++; exiting.Remove(n);} 
    GameObject.All.RemoveAll(c=>c.destroyed);
    foreach(var n in traffic.Npcs){ if(!lastMove.TryGetValue(n,out var lm) || (n.Position-lm.p).magnitude>0.3f) lastMove[n]=(n.Position,Time.time); }
    if(Time.time>=nextReport){ nextReport+=60;
      var roles=traffic.Npcs.GroupBy(n=>n.Role).Select(g=>$"{g.Key}:{g.Count()}");
      Console.WriteLine($"t={Time.time:F0} served={served} player=({player.Position.x:F1},{player.Position.z:F1}) head={traffic.PlayerIsHead} playerIdx={traffic.PlayerQueueIndex} inQ={traffic.PlayerInQueue} pumps=[{string.Join(",",traffic.Pumps.Select(p=>p.Occupant==null?(p.reservedForPlayer?"P":"-"):p.Occupant.Role.ToString()))}] barrier={(barrier.IsDown?"DOWN":"up")} {string.Join(" ",roles)}");
      var stuck=traffic.Npcs.Where(n=>n.Role!=NpcRole.Queue && n.Role!=NpcRole.Fueling && n.Role!=NpcRole.GasFueling && Time.time-lastMove[n].t>40).ToList();
      foreach(var n in stuck){ var f=typeof(NpcCar).GetField("targetOffset",BindingFlags.NonPublic|BindingFlags.Instance); var lb=typeof(NpcCar).GetField("laneBlockedTimer",BindingFlags.NonPublic|BindingFlags.Instance); var gu=typeof(NpcCar).GetField("giveUpTimer",BindingFlags.NonPublic|BindingFlags.Instance);
        Console.WriteLine($"   STUCK {n.gameObject.name} {n.Role} path={n.Path.name} s={n.S:F1}/{n.Path.Length:F1} pos=({n.Position.x:F1},{n.Position.z:F1}) blockedBy={(n.BlockedBy==null?"-":n.BlockedBy.gameObject.name+(n.BlockedBy is NpcCar bb?$"[{bb.Role} {bb.Path?.name} ({bb.Position.x:F1},{bb.Position.z:F1}) bb={(bb.BlockedBy==null?"-":bb.BlockedBy.gameObject.name)}]":""))} cut={n.Cut} off={n.Offset:F2}->{f.GetValue(n)} target={(n.TargetPath==null?"-":n.TargetPath.name)} laneBlocked={lb.GetValue(n)} giveUp={gu.GetValue(n)} free={traffic.FreeDistance(n,2f,out _):F2} moving={n.GetType().GetField("moving",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(n)} hold={n.GetType().GetField("holdTimer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(n)} speed={n.Speed:F2} touching={string.Join(",",traffic.Npcs.Where(o=>o!=n && Obb.Overlap(new Obb(n.Position+n.Forward*0.3f,n.Forward,n.Width+0.1f,n.Length+0.2f),o.Box)).Select(o=>o.gameObject.name+"/"+o.Role+"@("+o.Position.x.ToString("F1")+","+o.Position.z.ToString("F1")+")"))} playerTouch={Obb.Overlap(new Obb(n.Position+n.Forward*0.3f,n.Forward,n.Width+0.1f,n.Length+0.2f),player.Box)}"); }
      foreach(var v in traffic.Npcs.Where(n=>n.IsVip)) Console.WriteLine($"   VIP {v.Role} path={v.Path.name} s={v.S:F1}/{v.Path.Length:F1} blockedBy={(v.BlockedBy==null?"-":v.BlockedBy.gameObject.name)}");
      var head=traffic.Npcs.Where(n=>n.Role==NpcRole.Queue&&n.Path==traffic.QueuePath).OrderByDescending(n=>n.S).FirstOrDefault();
      if(head!=null) Console.WriteLine($"   head {head.gameObject.name} s={head.S:F2}/{traffic.QueuePath.Length:F2} speed={head.Speed:F2} blockedBy={(head.BlockedBy==null?"-":head.BlockedBy.gameObject.name)} idle={Time.time-lastMove[head].t:F0}s");
    }
  }
  if(race) Console.WriteLine($"RACE finished={traffic.FinishOrder.Count}/{traffic.Racers.Count}: {string.Join(", ",traffic.FinishOrder)}");
  Console.WriteLine($"GAS branched={gasIn.Count} fueled={gasFuel.Count} leftViaExit={gasOut.Count}");
  Console.WriteLine($"END t={Time.time:F0} served={served} playerDone={player.done} fuelRanOut={gm.FuelRanOut} cutIns={cutIns} overlapFrames={overlapFrames} worstOverlap={worstOverlap:F2}m");
 } }
