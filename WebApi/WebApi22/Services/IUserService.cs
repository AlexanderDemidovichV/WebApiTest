using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApi22.Dtos;
using WebApi22.Entities;

namespace WebApi22.Services
{
    public interface IUserService
    {
        User Authenticate(string userName, string password);
        UserDto GetById(int userId);
        IEnumerable<UserDto> GetAll();
        UserDto Create(UserDto user, string password);
        void Update(User user);
        void Delete(int id);
    }
}
