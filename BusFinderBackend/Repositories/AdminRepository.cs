using Google.Cloud.Firestore;
using BusFinderBackend.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Repositories
{
    public class AdminRepository
    {
        private readonly CollectionReference _adminsCollection;
        private readonly CollectionReference _passwordResetCodesCollection;
        private readonly FirestoreDb _firestoreDb;

        public AdminRepository(FirestoreDb firestoreDb)
        {
            _adminsCollection = firestoreDb.Collection("testAdmins");
            _passwordResetCodesCollection = firestoreDb.Collection("testPasswordResetCodes");
            _firestoreDb = firestoreDb;
        }

        public async Task<List<Admin>> GetAllAdminsAsync()
        {
            var snapshot = await _adminsCollection.GetSnapshotAsync();
            var admins = new List<Admin>();
            foreach (var doc in snapshot.Documents)
            {
                var admin = doc.ConvertTo<Admin>();
                admin.AdminId = doc.Id;
                admins.Add(admin);
            }
            return admins;
        }

        public async Task<Admin?> GetAdminByIdAsync(string adminId)
        {
            var doc = await _adminsCollection.Document(adminId).GetSnapshotAsync();
            if (doc.Exists)
            {
                var admin = doc.ConvertTo<Admin>();
                admin.AdminId = doc.Id;
                return admin;
            }
            return null;
        }

        public async Task<string> AddAdminAsync(Admin admin)
        {
            if (string.IsNullOrEmpty(admin.AdminId))
                throw new ArgumentException("AdminId must be provided.");

            await _adminsCollection.Document(admin.AdminId).SetAsync(admin);
            return admin.AdminId;
        }

        public async Task UpdateAdminAsync(string adminId, Admin admin)
        {
            await _adminsCollection.Document(adminId).SetAsync(admin, SetOptions.Overwrite);
        }

        public async Task DeleteAdminAsync(string adminId)
        {
            await _adminsCollection.Document(adminId).DeleteAsync();
        }

        public async Task<string> GenerateNextAdminIdAsync()
        {
            var snapshot = await _adminsCollection.GetSnapshotAsync();
            int maxNumber = 0;

            foreach (var doc in snapshot.Documents)
            {
                var id = doc.Id;
                if (id.StartsWith("admin"))
                {
                    var numberPart = id.Substring(5);
                    if (int.TryParse(numberPart, out int n))
                    {
                        if (n > maxNumber)
                            maxNumber = n;
                    }
                }
            }

            int nextNumber = maxNumber + 1;
            return $"admin{nextNumber.ToString("D3")}";
        }

        public async Task<Admin?> GetAdminByEmailAsync(string email)
        {
            var snapshot = await _adminsCollection.WhereEqualTo("Email", email).GetSnapshotAsync();
            if (snapshot.Documents.Count > 0)
            {
                var admin = snapshot.Documents[0].ConvertTo<Admin>();
                admin.AdminId = snapshot.Documents[0].Id;
                return admin;
            }
            return null;
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
    }
}
