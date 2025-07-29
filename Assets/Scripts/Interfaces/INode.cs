using Unity.Entities;

namespace Junk.Web
{
    public interface INode
    {

    }
    
    public struct Death : IComponentData, INode
    {
    }
    
    public struct Freeze : IComponentData, INode
    {
    }
    
    public struct Relaxed : IComponentData, INode
    {
    }
    
    public struct Combat : IComponentData, INode
    {
    }
}