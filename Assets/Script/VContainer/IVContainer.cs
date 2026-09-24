using System;

namespace GameDesignArchitecture.VContainer
{
    public interface IVContainer 
    {
        void Register<T>() where T : class;
    }
}