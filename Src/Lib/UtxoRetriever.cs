using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class UtxoRetriever
    {
        public Utxo[] GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
        {
            return new[]
            {
                new Utxo(
                    "768c63e27a1c816a83dc7b07e78af673b2400de8849ea7e7b734ae1333d100d2", 
                    0, 
                    new [] { new UtxoValue("lovelace",10000000)}),
                new Utxo(
                    "4c4e67bafa15e742c13c592b65c8f74c769cd7d9af04c848099672d1ba391b49",
                    0,
                    new [] { new UtxoValue("lovelace",20000000)}),
                new Utxo(
                    "768c63e27a1c816a83dc7b07e78af673b2400de8849ea7e7b734ae1333d100d2",
                    0,
                    new [] { new UtxoValue("lovelace",30000000)}),
            };
        }
    }
}
