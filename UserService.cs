using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartAnalytics.HonestWine.DAL.EF;
using SmartAnalytics.HonestWine.DAL.EF.Helpers;
using SmartAnalytics.HonestWine.Security.Policy;

namespace SmartAnalytics.HonestWine.PIM.Services
{
    public class UserService : IUserService
    {
        private readonly IHonestWineRepository _repository;

        public UserService(IHonestWineRepository repository)
        {
            _repository = repository;
        }

        public ValueTask<int> GetUserStatus(int userId)
        {
            return _repository.ApplicationAreaUsers
                .Where(x => x.ApplicationUserId == userId)
                .Select(x => x.UserStatus)
                .FirstOrDefaultTryAsync();
        }
    }
}
