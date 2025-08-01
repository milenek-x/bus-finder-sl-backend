using Google.Cloud.Firestore;
using BusFinderBackend.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Repositories
{
    public class StaffRepository
    {
        private readonly CollectionReference _staffCollection;
        private readonly CollectionReference _passwordResetCodesCollection;
        private readonly FirestoreDb _firestoreDb;

        public StaffRepository(FirestoreDb firestoreDb)
        {
            _staffCollection = firestoreDb.Collection("testStaff");
            _passwordResetCodesCollection = firestoreDb.Collection("testPasswordResetCodes");
            _firestoreDb = firestoreDb;
        }

        public async Task<List<Staff>> GetAllStaffAsync()
        {
            var snapshot = await _staffCollection.GetSnapshotAsync();
            var staffList = new List<Staff>();
            foreach (var doc in snapshot.Documents)
            {
                var staff = doc.ConvertTo<Staff>();
                staff.StaffId = doc.Id; // Assuming StaffId is the document ID
                staffList.Add(staff);
            }
            return staffList;
        }

        public async Task<Staff?> GetStaffByIdAsync(string staffId)
        {
            var doc = await _staffCollection.Document(staffId).GetSnapshotAsync();
            if (doc.Exists)
            {
                var staff = doc.ConvertTo<Staff>();
                staff.StaffId = doc.Id; // Assuming StaffId is the document ID
                return staff;
            }
            return null;
        }

        public async Task<string> GenerateNextStaffIdAsync()
        {
            var snapshot = await _staffCollection.GetSnapshotAsync();
            int maxNumber = 0;

            foreach (var doc in snapshot.Documents)
            {
                var id = doc.Id; // Assuming the ID is the StaffId
                if (id.StartsWith("staff"))
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
            return $"staff{nextNumber.ToString("D3")}"; // Example format: staff001
        }

        public async Task AddStaffAsync(Staff staff)
        {
            if (string.IsNullOrEmpty(staff.StaffId))
            {
                staff.StaffId = await GenerateNextStaffIdAsync(); // Generate StaffId if not provided
            }

            await _staffCollection.Document(staff.StaffId).SetAsync(staff);
        }

        public async Task UpdateStaffAsync(string staffId, Staff staff)
        {
            await _staffCollection.Document(staffId).SetAsync(staff, SetOptions.Overwrite);
        }

        public async Task DeleteStaffAsync(string staffId)
        {
            await _staffCollection.Document(staffId).DeleteAsync();
        }

        public async Task<Staff?> GetStaffByEmailAsync(string email)
        {
            var snapshot = await _staffCollection.WhereEqualTo("Email", email).GetSnapshotAsync();
            if (snapshot.Documents.Count > 0)
            {
                var staff = snapshot.Documents[0].ConvertTo<Staff>();
                staff.StaffId = snapshot.Documents[0].Id; // Set the StaffId
                return staff;
            }
            return null; // Return null if no staff found
        }

        public async Task<List<Staff>> GetStaffByRoleAsync(string role)
        {
            var snapshot = await _staffCollection.WhereEqualTo("StaffRole", role).GetSnapshotAsync();
            var staffList = new List<Staff>();
            foreach (var doc in snapshot.Documents)
            {
                var staff = doc.ConvertTo<Staff>();
                staff.StaffId = doc.Id; // Assuming StaffId is the document ID
                staffList.Add(staff);
            }
            return staffList;
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
    }
}
