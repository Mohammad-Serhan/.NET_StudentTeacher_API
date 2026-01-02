using AutoMapper;
using StudentApi.Classes;
using StudentApi.DTO;

namespace StudentApi.Mappings
{
    public class StudentProfile : Profile
    {
        public StudentProfile()
        {
            // Basic mappings
            CreateMap<StudentDTO, EStudent>();
            CreateMap<EStudent, StudentDTO>();

            // CRUD operations
            CreateMap<InsertStudentDTO, EStudent>();
            CreateMap<UpdateStudentDTO, EStudent>();
            CreateMap<DeleteStudentDTO, EStudent>();

            // Detailed view
            CreateMap<EStudent, StudentDetailsDTO>()
                .ForMember(dest => dest.TeacherName, opt => opt.Ignore()); // Will be set manually
        }
    }

    public class StudentDetailsDTO : StudentDTO
    {
        public new string TeacherName { get; set; } = string.Empty;
    }
}