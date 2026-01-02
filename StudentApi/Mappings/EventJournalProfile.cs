using AutoMapper;
using StudentApi.Classes;
using StudentApi.DTO;

namespace StudentApi.Mappings
{
    public class EventJournalProfile : Profile
    {
        public EventJournalProfile()
        {
            CreateMap<EEventJournal, EventLogDTO>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Will be set manually from User service
                .ForMember(dest => dest.CreationDateTime, opt => opt.MapFrom(src => src.CreationDateTime));
        }
    }
}