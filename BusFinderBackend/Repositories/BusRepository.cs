using BusFinderBackend.Model;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Repositories
{
    public class BusRepository
    {
        private readonly CollectionReference _busesCollection;
        private readonly CollectionReference _busRoutesCollection;

        public BusRepository(FirestoreDb firestoreDb)
        {
            _busesCollection = firestoreDb.Collection("testBuses");
            _busRoutesCollection = firestoreDb.Collection("testBusRoutes");
        }

        public async Task<List<Bus>> GetAllBusesAsync()
        {
            var buses = new List<Bus>();
            var snapshot = await _busesCollection.GetSnapshotAsync();

            foreach (var document in snapshot.Documents)
            {
                var bus = document.ConvertTo<Bus>();
                buses.Add(bus);
            }

            return buses;
        }

        public async Task<Bus?> GetBusByNumberPlateAsync(string numberPlate)
        {
            var document = await _busesCollection.Document(numberPlate).GetSnapshotAsync();
            return document.Exists ? document.ConvertTo<Bus>() : null;
        }

        public async Task AddBusAsync(Bus bus)
        {
            await _busesCollection.Document(bus.NumberPlate).SetAsync(bus);
        }

        public async Task UpdateBusAsync(string numberPlate, Bus bus)
        {
            await _busesCollection.Document(numberPlate).SetAsync(bus, SetOptions.Overwrite);
        }

        public async Task DeleteBusAsync(string numberPlate)
        {
            await _busesCollection.Document(numberPlate).DeleteAsync();
        }

        public async Task UpdateBusCapacityAsync(string numberPlate, bool busCapacity)
        {
            var document = _busesCollection.Document(numberPlate);
            await document.UpdateAsync(new Dictionary<string, object>
            {
                { "BusCapacity", busCapacity }
            });
        }

        public async Task UpdateSosStatusAsync(string numberPlate, bool sosStatus)
        {
            var document = _busesCollection.Document(numberPlate);
            await document.UpdateAsync(new Dictionary<string, object>
            {
                { "SosStatus", sosStatus }
            });
        }

        public async Task UpdateCurrentLocationAsync(string numberPlate, double? latitude, double? longitude)
        {
            var busDoc = _busesCollection.Document(numberPlate);
            var updates = new Dictionary<string, object>();

            updates["CurrentLocationLatitude"] = latitude ?? 0; // Set to 0 if latitude is null
            updates["CurrentLocationLongitude"] = longitude ?? 0; // Set to 0 if longitude is null

            await busDoc.UpdateAsync(updates);
        }

        public async Task<BusRoute?> GetBusRouteByNumberAsync(string routeNumber)
        {
            var document = await _busRoutesCollection.Document(routeNumber).GetSnapshotAsync();
            return document.Exists ? document.ConvertTo<BusRoute>() : null;
        }
    }
} 