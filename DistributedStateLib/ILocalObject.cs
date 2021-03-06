﻿// Copyright (c) 2020 by Rob Jellinghaus.

namespace Distributed.State
{
    /// <summary>
    /// Base class for local object implementations that handle the local behavior of a distributed object.
    /// </summary>
    /// <remarks>
    /// Both owner and proxy objects contain an instance of the appropriate type of local object; this ensures the same
    /// behavior regardless of owner/proxy topology.
    /// 
    /// ILocalObject implements IDistributedInterface because it is possible to invoke methods from a distributed interface
    /// on a local object.
    /// </remarks>
    public interface ILocalObject : IDistributedInterface
    {
        /// <summary>
        /// The distributed object that contains this local object.
        /// </summary>
        /// <remarks>
        /// How this is connected/initialized is implementation-specific.
        /// </remarks>
        IDistributedObject DistributedObject { get; }
    }
}
