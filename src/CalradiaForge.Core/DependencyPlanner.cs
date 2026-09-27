using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaForge.Core
{
    public sealed class DependencyPlan
    {
        public bool Complete { get; set; }
        public List<string> Order { get; set; }=new List<string>();
        public List<string> Blocked { get; set; }=new List<string>();
        public List<string> Notes { get; set; }=new List<string>();
    }

    public static class DependencyPlanner
    {
        public static DependencyPlan Create(IEnumerable<Module> modules)
        {
            if(modules==null)throw new ArgumentNullException(nameof(modules));
            var groups=modules.GroupBy(m=>m.Id,StringComparer.OrdinalIgnoreCase).ToArray();
            var result=new DependencyPlan();
            if(groups.Any(g=>g.Count()>1)) {result.Notes.Add("Duplicate module IDs prevent an unambiguous order.");return result;}
            var pending=groups.ToDictionary(g=>g.Key,g=>g.First(),StringComparer.OrdinalIgnoreCase);
            var complete=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while(pending.Count>0)
            {
                var ready=pending.Values.Where(m=>(m.Dependencies??new List<string>()).All(complete.Contains)).OrderBy(m=>m.Id,StringComparer.OrdinalIgnoreCase).ToArray();
                if(ready.Length==0)break;
                foreach(var module in ready){result.Order.Add(module.Id);complete.Add(module.Id);pending.Remove(module.Id);}
            }
            result.Blocked=pending.Keys.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
            result.Complete=result.Blocked.Count==0;
            result.Notes.Add("Required dependencies only; this does not replace mod-author load-order instructions or change the launcher.");
            if(!result.Complete)result.Notes.Add("Blocked modules have missing or circular dependencies, or depend on a blocked module.");
            return result;
        }
    }
}
