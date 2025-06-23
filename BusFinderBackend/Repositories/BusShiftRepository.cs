using BusFinderBackend.Model;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Repositories
{
    public class BusShiftRepository
    {
        private readonly CollectionReference _busShiftsCollection;

        public BusShiftRepository(FirestoreDb firestoreDb)
        {
            _busShiftsCollection = firestoreDb.Collection("testBusShifts"); // Ensure this matches your Firestore collection name
        }

        public async Task<List<BusShift>> GetAllBusShiftsAsync()
        {
            var busShifts = new List<BusShift>();
            var snapshot = await _busShiftsCollection.GetSnapshotAsync();

            foreach (var document in snapshot.Documents)
            {
                var busShift = document.ConvertTo<BusShift>();
                busShifts.Add(busShift);
            }

            return busShifts;
        }

        public async Task<BusShift?> GetBusShiftByIdAsync(string shiftId)
        {
            var document = await _busShiftsCollection.Document(shiftId).GetSnapshotAsync();
            return document.Exists ? document.ConvertTo<BusShift>() : null;
        }

        public async Task AddBusShiftAsync(BusShift busShift)
        {
            await _busShiftsCollection.Document(busShift.ShiftId).SetAsync(busShift);
        }

        public async Task UpdateBusShiftAsync(string shiftId, BusShift busShift)
        {
            await _busShiftsCollection.Document(shiftId).SetAsync(busShift, SetOptions.Overwrite);
        }

        public async Task DeleteBusShiftAsync(string shiftId)
        {
            await _busShiftsCollection.Document(shiftId).DeleteAsync();
        }

        public async Task<string> GenerateNextShiftIdAsync()
        {
            var busShifts = await GetAllBusShiftsAsync();
            int nextId = busShifts.Count + 1;
            return $"shift{nextId:D5}"; // Format as shift00001, shift00002, etc.
        }
    }
} 