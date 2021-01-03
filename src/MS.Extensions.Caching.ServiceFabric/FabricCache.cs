using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FabricCache
{
    /// <summary>
    /// Eine Instanz dieser Klasse wird von der Service Fabric-Laufzeit für jedes Dienstreplikat erstellt.
    /// </summary>
    internal sealed class FabricCache : StatefulService
    {
        public FabricCache(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optionale Außerkraftsetzung, um Listener (z. B. HTTP, Dienstremoting, WCF usw.) für dieses Dienstreplikat zum Verarbeiten von Client- oder Benutzeranforderungen zu erstellen.
        /// </summary>
        /// <remarks>
        /// Weitere Informationen zur Dienstkommunikation finden Sie unter https://aka.ms/servicefabricservicecommunication.
        /// </remarks>
        /// <returns>Eine Sammlung von Listenern.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// Dies ist der Haupteinstiegspunkt für Ihr Dienstreplikat.
        /// Diese Methode wird ausgeführt, wenn dieses Replikat Ihres Diensts zum primären Replikat wird und Schreibstatus besitzt.
        /// </summary>
        /// <param name="cancellationToken">Wird abgebrochen, wenn Service Fabric dieses Dienstreplikat herunterfahren muss.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Ersetzen Sie den folgenden Beispielcode durch Ihre eigene Programmlogik, 
            //       oder entfernen Sie diese RunAsync-Außerkraftsetzung, wenn sie in Ihrem Dienst nicht benötigt wird.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // Wenn eine Ausnahme vor dem Aufruf von "CommitAsync" ausgelöst wird, wird die Transaktion abgebrochen, alle Änderungen werden 
                    // verworfen, und in den sekundären Replikaten wird nichts gespeichert.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
