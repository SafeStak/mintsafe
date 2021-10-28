using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Abstractions
{
    public interface ITxBuilder
    {
        public Task<byte[]> BuildTxAsync(TxBuildCommand buildCommand);
    }
}
