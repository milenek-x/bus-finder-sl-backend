using BusFinderBackend.Model;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Repositories
{
    public class PassengerRepository
    {
        private readonly CollectionReference _passengersCollection;
        private readonly CollectionReference _busRoutesCollection;
        private readonly CollectionReference _placesCollection;

        public PassengerRepository(FirestoreDb firestoreDb)
        {
            _passengersCollection = firestoreDb.Collection("testPassengers");
            _busRoutesCollection = firestoreDb.Collection("testBusRoutes");
            _placesCollection = firestoreDb.Collection("testPlaces");
        }

        public async Task<List<Passenger>> GetAllPassengersAsync()
        {
            var passengers = new List<Passenger>();
            var snapshot = await _passengersCollection.GetSnapshotAsync();

            foreach (var document in snapshot.Documents)
            {
                var passenger = document.ConvertTo<Passenger>();
                passengers.Add(passenger);
            }

            return passengers;
        }

        public async Task<Passenger?> GetPassengerByIdAsync(string passengerId)
        {
            var document = await _passengersCollection.Document(passengerId).GetSnapshotAsync();
            return document.Exists ? document.ConvertTo<Passenger>() : null;
        }

        public async Task AddPassengerAsync(Passenger passenger)
        {
            await _passengersCollection.Document(passenger.PassengerId).SetAsync(passenger);
        }

        public async Task UpdatePassengerAsync(string passengerId, Passenger passenger)
        {
            await _passengersCollection.Document(passengerId).SetAsync(passenger, SetOptions.Overwrite);
        }

        public async Task DeletePassengerAsync(string passengerId)
        {
            await _passengersCollection.Document(passengerId).DeleteAsync();
        }

        public async Task AddFavoriteRouteAsync(string passengerId, string routeId)
        {
            // Check if the route exists
            var route = await _busRoutesCollection.Document(routeId).GetSnapshotAsync();
            if (!route.Exists)
            {
                throw new ArgumentException("Route does not exist.");
            }

            var document = _passengersCollection.Document(passengerId);
            await document.UpdateAsync(new Dictionary<string, object>
            {
                { "FavoriteRoutes", FieldValue.ArrayUnion(routeId) }
            });
        }

        public async Task AddFavoritePlaceAsync(string passengerId, string placeId)
        {
            // Check if the place exists
            var place = await _placesCollection.Document(placeId).GetSnapshotAsync();
            if (!place.Exists)
            {
                throw new ArgumentException("Place does not exist.");
            }

            var document = _passengersCollection.Document(passengerId);
            await document.UpdateAsync(new Dictionary<string, object>
            {
                { "FavoritePlaces", FieldValue.ArrayUnion(placeId) }
            });
        }

        public async Task RemoveFavoriteRouteAsync(string passengerId, string routeId)
        {
            var document = _passengersCollection.Document(passengerId);
            await document.UpdateAsync(new Dictionary<string, object>
            {
                { "FavoriteRoutes", FieldValue.ArrayRemove(routeId) }
            });
        }

        public async Task RemoveFavoritePlaceAsync(string passengerId, string placeId)
        {
            var document = _passengersCollection.Document(passengerId);
            await document.UpdateAsync(new Dictionary<string, object>
            {
                { "FavoritePlaces", FieldValue.ArrayRemove(placeId) }
            });
        }

        public async Task<string> GenerateNextPassengerIdAsync()
        {
            var snapshot = await _passengersCollection.GetSnapshotAsync();
            int maxNumber = 0;

            foreach (var doc in snapshot.Documents)
            {
                var id = doc.Id; // Assuming the ID is the PassengerId
                if (id.StartsWith("passenger"))
                {
                    var numberPart = id.Substring(8); // Extract the number part
                    if (int.TryParse(numberPart, out int n))
                    {
                        if (n > maxNumber)
                            maxNumber = n;
                    }
                }
            }

            int nextNumber = maxNumber + 1;
            return $"passenger{nextNumber:D5}"; // Example format: passenger00001
        }
    }
} 