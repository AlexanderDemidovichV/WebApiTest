using AutoMapper;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebApi22.Dtos;
using WebApi22.Entities;
using WebApi22.Helpers;

namespace WebApi22.Services
{
    public class UserService : IUserService
    {
        private static List<User> _users = new List<User>
        {
            new User { 
                Id = 1, 
                FirstName = "Admin", 
                LastName = "User", 
                UserName = "admin", 
                PasswordHash = Encoding.ASCII.GetBytes("admin"),
                PasswordSalt = Encoding.ASCII.GetBytes("salt"),
                Role = Roles.Admin 
            },
            new User { 
                Id = 2, 
                FirstName = "Normal", 
                LastName = "User", 
                UserName = "user", 
                PasswordHash = Encoding.ASCII.GetBytes("user"),
                PasswordSalt = Encoding.ASCII.GetBytes("salt"),
                Role = Roles.User 
            }
        };
        
        private IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UserService(IOptions<AppSettings> appSettings, IMapper mapper)
        {
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        public User Authenticate(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                return null;

            var user = _users.SingleOrDefault(x => x.UserName == userName);

            // check if username exists
            if (user == null)
                return null;

            // check if password is correct
            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;
            
            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);

            // remove password before returning
            
            return user;
        }

        public IEnumerable<UserDto> GetAll()
        {
            var users = _users;
            var userDtos = _mapper.Map<IList<UserDto>>(users);
            return userDtos;
        }

        public UserDto GetById(int userId)
        {
            var user = _users.FirstOrDefault(x => x.Id == userId);

            var userDto = _mapper.Map<UserDto>(user);
            return userDto;
        }

        public UserDto Create(UserDto userDto, string password)
        {
            // validation
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password is required");

            if (_users.Any(x => x.UserName == userDto.UserName))
                throw new Exception("Username \"" + userDto.UserName + "\" is already taken");

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            var user = _mapper.Map<User>(userDto);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            _users.Add(user);
            //_context.SaveChanges();

            return _mapper.Map<UserDto>(user);
        }

        public void Update(User user)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            var user = _users.Single(u => u.Id == id);
            if (user != null)
            {
                _users.Remove(user);
                //_context.SaveChanges();
            }
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }
    }
}
