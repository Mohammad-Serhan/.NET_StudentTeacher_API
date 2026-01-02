using AutoMapper;
using StudentApi.Classes;
using StudentApi.DTO;

namespace StudentApi.Mappings
{
    public class TeacherProfile : Profile
    {
        public TeacherProfile()
        {
            // Basic mappings
            CreateMap<TeacherDTO, ETeacher>();
            CreateMap<ETeacher, TeacherDTO>();

            // CRUD operations
            CreateMap<InsertTeacherDTO, ETeacher>();
            CreateMap<UpdateTeacherDTO, ETeacher>();
            CreateMap<DeleteTeacherDTO, ETeacher>();
        }
    }
}