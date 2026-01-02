using Auth.Shared.Classes;
using AutoMapper;
using StudentApi.Classes;
using StudentApi.DTO;

namespace StudentApi.Mappings
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            

            CreateMap<EUser, UserDTO>();
            CreateMap<EUser, UserWithRolesDTO>();
            CreateMap<UpdateUserDTO, EUser>();
        }
    }
}