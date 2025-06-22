using Google.Cloud.Firestore;
using BusFinderBackend.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BusFinderBackend.Repositories
{
    public class BusRouteRepository
    {
        private readonly CollectionReference _busRoutesCollection;
        private readonly FirestoreDb _firestoreDb;
        private readonly BusStopRepository _busStopRepository;

        public BusRouteRepository(FirestoreDb firestoreDb, BusStopRepository busStopRepository)
        {
            _busRoutesCollection = firestoreDb.Collection("testBusRoutes");
            _firestoreDb = firestoreDb;
            _busStopRepository = busStopRepository;
        }

        public async Task<List<BusRoute>> GetAllBusRoutesAsync()
        {
            var snapshot = await _busRoutesCollection.GetSnapshotAsync();
            var busRoutes = new List<BusRoute>();
            foreach (var doc in snapshot.Documents)
            {
                var busRoute = doc.ConvertTo<BusRoute>();
                busRoute.RouteNumber = doc.Id; // Assuming RouteNumber is the document ID
                busRoutes.Add(busRoute);
            }
            return busRoutes;
        }

        public async Task<BusRoute?> GetBusRouteByNumberAsync(string routeNumber)
        {
            var document = await _busRoutesCollection.Document(routeNumber).GetSnapshotAsync();

            if (document.Exists)
            {
                return new BusRoute
                {
                    RouteNumber = document.Id, // Use the document ID as RouteNumber
                    RouteName = document.GetValue<string>("RouteName"),
                    RouteStops = document.GetValue<List<string>>("RouteStops")
                };
            }
            return null; // No bus route found
        }

        public async Task AddBusRouteAsync(BusRoute busRoute)
        {
            if (string.IsNullOrEmpty(busRoute.RouteNumber))
                throw new ArgumentException("RouteNumber must be provided.");

            // Validate that all bus stops exist
            if (busRoute.RouteStops != null) // Check if RouteStops is not null
            {
                foreach (var stopName in busRoute.RouteStops)
                {
                    var existingBusStop = await _busStopRepository.GetBusStopByNameAsync(stopName);
                    if (existingBusStop == null)
                    {
                        throw new ArgumentException($"Bus stop '{stopName}' does not exist.");
                    }
                }
            }
            else
            {
                throw new ArgumentException("RouteStops cannot be null.");
            }

            await _busRoutesCollection.Document(busRoute.RouteNumber).SetAsync(busRoute);
        }

        public async Task UpdateBusRouteAsync(string routeNumber, BusRoute busRoute)
        {
            await _busRoutesCollection.Document(routeNumber).SetAsync(busRoute, SetOptions.Overwrite);
        }

        public async Task DeleteBusRouteAsync(string routeNumber)
        {
            await _busRoutesCollection.Document(routeNumber).DeleteAsync();
        }
    }
}
