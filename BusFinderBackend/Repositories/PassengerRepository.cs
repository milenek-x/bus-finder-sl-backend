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
        private readonly CollectionReference _passwordResetCodesCollection;

        public PassengerRepository(FirestoreDb firestoreDb)
        {
            _passengersCollection = firestoreDb.Collection("testPassengers");
            _busRoutesCollection = firestoreDb.Collection("testBusRoutes");
            _placesCollection = firestoreDb.Collection("testPlaces");
            _passwordResetCodesCollection = firestoreDb.Collection("testPasswordResetCodes");
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
                    var numberPart = id.Substring(9); // Extract the number part
                    if (int.TryParse(numberPart, out int n))
                    {
                        if (n > maxNumber)
                        {
                            maxNumber = n;
                        }
                    }
                }
            }

            int nextNumber = maxNumber + 1;
            var newPassengerId = $"passenger{nextNumber:D5}"; // Example format: passenger00001
            Console.WriteLine($"Generated new Passenger ID: {newPassengerId}"); // Log the new ID
            return newPassengerId;
        }

        public async Task<Passenger?> GetPassengerByEmailAsync(string email)
        {
            var snapshot = await _passengersCollection.WhereEqualTo("Email", email).GetSnapshotAsync();
            if (snapshot.Documents.Count > 0)
            {
                var passenger = snapshot.Documents[0].ConvertTo<Passenger>();
                passenger.PassengerId = snapshot.Documents[0].Id; // Set the PassengerId
                return passenger;
            }
            return null; // Return null if no passenger found
        }

        public async Task StoreOobCodeAsync(string email, string oobCode)
        {
            var resetCodeDocument = _passwordResetCodesCollection.Document(email);
            var data = new
            {
                OobCode = oobCode,
                Expiration = DateTime.UtcNow.AddHours(1) // Set expiration time (e.g., 1 hour)
            };
            await resetCodeDocument.SetAsync(data);
        }

        public async Task<string?> RetrieveOobCodeAsync(string email)
        {
            var document = await _passwordResetCodesCollection.Document(email).GetSnapshotAsync();
            if (document.Exists)
            {
                var data = document.ToDictionary();
                // Check if the code is expired
                if (data.ContainsKey("Expiration") && DateTime.UtcNow < ((Timestamp)data["Expiration"]).ToDateTime())
                {
                    return data["OobCode"].ToString();
                }
            }
            return null; // Return null if no valid oobCode found
        }

        public async Task DeleteOobCodeAsync(string email)
        {
            await _passwordResetCodesCollection.Document(email).DeleteAsync();
        }

        public async Task UpdateCurrentLocationAsync(string passengerId, double? latitude, double? longitude)
        {
            var document = _passengersCollection.Document(passengerId);
            await document.UpdateAsync(new Dictionary<string, object>
            {
                { "CurrentLocationLatitude", latitude ?? 0 }, // Default to 0 if null
                { "CurrentLocationLongitude", longitude ?? 0 } // Default to 0 if null
            });
        }
    }
} 