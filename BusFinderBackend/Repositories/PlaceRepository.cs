using Google.Cloud.Firestore;
using BusFinderBackend.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Repositories
{
    public class PlaceRepository
    {
        private readonly CollectionReference _placesCollection;
        private readonly FirestoreDb _firestoreDb;

        public PlaceRepository(FirestoreDb firestoreDb)
        {
            _placesCollection = firestoreDb.Collection("places");
            _firestoreDb = firestoreDb;
        }

        public async Task<List<Place>> GetAllPlacesAsync()
        {
            var snapshot = await _placesCollection.GetSnapshotAsync();
            var placesList = new List<Place>();
            foreach (var doc in snapshot.Documents)
            {
                var place = doc.ConvertTo<Place>();
                place.PlaceId = doc.Id; // Assuming PlaceId is the document ID
                placesList.Add(place);
            }
            return placesList;
        }

        public async Task<Place?> GetPlaceByIdAsync(string placeId)
        {
            var doc = await _placesCollection.Document(placeId).GetSnapshotAsync();
            if (doc.Exists)
            {
                var place = doc.ConvertTo<Place>();
                place.PlaceId = doc.Id; // Assuming PlaceId is the document ID
                return place;
            }
            return null;
        }

        public async Task AddPlaceAsync(Place place)
        {
            if (string.IsNullOrEmpty(place.PlaceId))
                throw new ArgumentException("PlaceId must be provided.");

            await _placesCollection.Document(place.PlaceId).SetAsync(place);
        }

        public async Task UpdatePlaceAsync(string placeId, Place place)
        {
            await _placesCollection.Document(placeId).SetAsync(place, SetOptions.Overwrite);
        }

        public async Task DeletePlaceAsync(string placeId)
        {
            await _placesCollection.Document(placeId).DeleteAsync();
        }

        public async Task<string> GenerateNextPlaceIdAsync()
        {
            var snapshot = await _placesCollection.GetSnapshotAsync();
            int maxNumber = 0;

            foreach (var doc in snapshot.Documents)
            {
                var id = doc.Id; // Assuming the ID is the PlaceId
                if (id.StartsWith("place"))
                {
                    var numberPart = id.Substring(5); // Extract the number part
                    if (int.TryParse(numberPart, out int n))
                    {
                        if (n > maxNumber)
                            maxNumber = n;
                    }
                }
            }

            int nextNumber = maxNumber + 1;
            return $"place{nextNumber.ToString("D3")}"; // Example format: place001
        }
    }
}
