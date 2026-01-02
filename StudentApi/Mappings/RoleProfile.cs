using Auth.Shared.Classes;
using AutoMapper;
using StudentApi.Classes;
using StudentApi.DTO;

namespace StudentApi.Mappings
{
    public class RoleProfile : Profile
    {
        public RoleProfile()
        {
            CreateMap<ERole, RoleDTO>();
            CreateMap<RoleDTO, ERole>();
            CreateMap<RoleInsertDTO, ERole>();
            CreateMap<RoleUpdateDTO, ERole>();
        }
    }
}