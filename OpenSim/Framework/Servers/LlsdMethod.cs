using System;
using System.Collections.Generic;
using System.Text;

namespace OpenSim.Framework.Servers
{
    public delegate TResponse LlsdMethod<TResponse, TRequest>( TRequest request );
}
