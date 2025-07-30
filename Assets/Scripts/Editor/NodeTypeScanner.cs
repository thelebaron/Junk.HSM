using System;
using System.Collections.Generic;
using System.Linq;

namespace Junk.UI.Web.Editor
{
    public static class NodeTypeScanner
    {
        private static List<Type> cachedNodeTypes;
        
        public static List<Type> GetAllINodeStructTypes()
        {
            if (cachedNodeTypes != null)
                return cachedNodeTypes;
                
            cachedNodeTypes = new List<Type>();
            
            // Get all assemblies in the current domain
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                // Get all types from the assembly
                var types = assembly.GetTypes();
                
                foreach (var type in types)
                {
                    // Check if it's a struct that implements INode
                    if (type.IsValueType && 
                        !type.IsEnum && 
                        typeof(IWebNode).IsAssignableFrom(type) &&
                        type != typeof(IWebNode))
                    {
                        cachedNodeTypes.Add(type);
                    }
                }
            }
            
            // Sort by name for consistent ordering
            cachedNodeTypes = cachedNodeTypes.OrderBy(t => t.Name).ToList();
            
            return cachedNodeTypes;
        }
        
        public static string GetDisplayName(Type nodeType)
        {
            return nodeType.Name;
        }
    }
}
