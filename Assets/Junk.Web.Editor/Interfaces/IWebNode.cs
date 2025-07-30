using Unity.Entities;

namespace Junk.Web
{
    public interface IWebNode
    {

    }
    
    public struct Death : IComponentData, IWebNode
    {
    }
    
    public struct Freeze : IComponentData, IWebNode
    {
    }
    
    public struct Relaxed : IComponentData, IWebNode
    {
    }
    
    public struct Combat : IComponentData, IWebNode
    {
    }
}