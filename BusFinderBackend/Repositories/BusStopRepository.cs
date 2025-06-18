using Google.Cloud.Firestore;
using BusFinderBackend.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Repositories
{
    public class BusStopRepository
    {
        private readonly CollectionReference _busStopsCollection;
        private readonly FirestoreDb _firestoreDb;

        public BusStopRepository(FirestoreDb firestoreDb)
        {
            _busStopsCollection = firestoreDb.Collection("testBusStops");
            _firestoreDb = firestoreDb;
        }

        public async Task<List<BusStop>> GetAllBusStopsAsync()
        {
            var snapshot = await _busStopsCollection.GetSnapshotAsync();
            var busStops = new List<BusStop>();
            foreach (var doc in snapshot.Documents)
            {
                var busStop = doc.ConvertTo<BusStop>();
                busStop.StopName = doc.Id; // Assuming StopName is the document ID
                busStops.Add(busStop);
            }
            return busStops;
        }

        public async Task<BusStop?> GetBusStopByNameAsync(string stopName)
        {
            // Directly access the document using the document ID
            var document = await _busStopsCollection.Document(stopName).GetSnapshotAsync();

            if (document.Exists)
            {
                return new BusStop
                {
                    StopName = document.Id, // Use the document ID as StopName
                    StopLatitude = document.GetValue<double>("StopLatitude"),
                    StopLongitude = document.GetValue<double>("StopLongitude")
                };
            }
            return null; // No bus stop found
        }

        public async Task AddBusStopAsync(BusStop busStop)
        {
            if (string.IsNullOrEmpty(busStop.StopName))
                throw new ArgumentException("StopName must be provided.");

            await _busStopsCollection.Document(busStop.StopName).SetAsync(busStop);
        }

        public async Task UpdateBusStopAsync(string stopName, BusStop busStop)
        {
            await _busStopsCollection.Document(stopName).SetAsync(busStop, SetOptions.Overwrite);
        }

        public async Task DeleteBusStopAsync(string stopName)
        {
            await _busStopsCollection.Document(stopName).DeleteAsync();
        }

        public async Task<List<BusStop>> SearchBusStopsByPartialNameAsync(string partialName)
        {
            var snapshot = await _busStopsCollection.GetSnapshotAsync();
            var busStops = new List<BusStop>();

            foreach (var doc in snapshot.Documents)
            {
                if (doc.Id.Contains(partialName, StringComparison.OrdinalIgnoreCase)) // Check if the document ID contains the partial name
                {
                    var busStop = doc.ConvertTo<BusStop>();
                    busStop.StopName = doc.Id; // Assuming StopName is the document ID
                    busStops.Add(busStop);
                }
            }
            return busStops;
        }
    }
}
