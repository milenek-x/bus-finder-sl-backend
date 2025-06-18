using BusFinderBackend.Repositories;
using BusFinderBackend.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Services
{
    public class StaffService
    {
        private readonly StaffRepository _staffRepository;

        public StaffService(StaffRepository staffRepository)
        {
            _staffRepository = staffRepository;
        }

        public Task<List<Staff>> GetAllStaffAsync()
        {
            return _staffRepository.GetAllStaffAsync();
        }

        public Task<Staff?> GetStaffByIdAsync(string staffId)
        {
            return _staffRepository.GetStaffByIdAsync(staffId);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> AddStaffAsync(Staff staff)
        {
            if (string.IsNullOrEmpty(staff.StaffId))
            {
                staff.StaffId = await _staffRepository.GenerateNextStaffIdAsync();
            }

            await _staffRepository.AddStaffAsync(staff);
            return (true, null, null);
        }

        public Task UpdateStaffAsync(string staffId, Staff staff)
        {
            return _staffRepository.UpdateStaffAsync(staffId, staff);
        }

        public Task DeleteStaffAsync(string staffId)
        {
            return _staffRepository.DeleteStaffAsync(staffId);
        }
    }
}