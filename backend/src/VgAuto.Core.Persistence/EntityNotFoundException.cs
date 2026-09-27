using System;

namespace VgAuto.Core.Persistence
{ 
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string name) : base($"enitity '{name}' not found")
        {
        }
    }
}