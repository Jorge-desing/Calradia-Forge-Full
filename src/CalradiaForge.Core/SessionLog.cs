using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CalradiaForge.Core
{
    public sealed class SessionLog
    {
        readonly object sync=new object(); readonly Queue<LogEntry> queue=new Queue<LogEntry>();
        readonly object persistenceSync=new object();
        volatile string persistenceError;
        public string Id { get; }=Guid.NewGuid().ToString("N");
        public string PersistenceError => persistenceError;
        public void Add(string module,string level,string message)
        {
            var r=new LogEntry { Time=DateTime.UtcNow.ToString("O"),Session=Id,Module=module,Level=level,Message=message?.Substring(0,Math.Min(message.Length,16000)) };
            lock(sync) { queue.Enqueue(r); while(queue.Count>2000) queue.Dequeue(); }
        }
        public List<LogEntry> Deserialize(string filter="") { lock(sync) return queue.Where(r=>string.IsNullOrEmpty(filter) || (r.Module+" "+r.Level+" "+r.Message+" "+r.Session).IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0).ToList(); }
        public void Clear()
        {
            lock(sync) { queue.Clear(); }
        }
        public void Persist(string folder)
        {
            // Periodic persistence and final shutdown persistence can overlap. Capture
            // the snapshot inside this gate so an older write cannot win the race.
            lock(persistenceSync) {
                var path=Path.Combine(folder,Id+".json");
                var operation="Write session";
                try {
                    Directory.CreateDirectory(folder);
                    Json.Save(path,Deserialize());
                    operation="Rotate sessions";
                    foreach(var file in new DirectoryInfo(folder).GetFiles("*.json").OrderByDescending(file=>file.LastWriteTimeUtc).Skip(20)) file.Delete();
                    persistenceError=null;
                }
                catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException) {
                    persistenceError=operation+" failed (0x"+ex.HResult.ToString("X8")+"): "+path+" — "+ex.Message;
                }
            }
        }
        public static List<LogEntry> Import(string file)
        {
            if(new FileInfo(file).Length>16*1024*1024) throw new InvalidDataException("Maximum 16 MB");
            return File.ReadLines(file).Take(2000).Select(l=>new LogEntry { Session="Imported",Module=Path.GetFileName(file),Level="Info",Message=l,Time="" }).ToList();
        }
    }
    public static class ReportExporter
    {
        public static void Export(string basePath,SessionReport i)
        {
            Json.Save(basePath+".json",i);
            File.WriteAllText(basePath+".html",ReportHtml.Render(i),Encoding.UTF8);
        }
    }
}
