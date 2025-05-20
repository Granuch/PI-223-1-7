using AutoMapper;
using PI_223_1_7.Models;
using Mapping.DTOs;

namespace Mapping.Mapping
{
        public class MappingProfile : Profile
        {
            public MappingProfile()
            {
                // Book mappings
                CreateMap<Book, BookDTO>()
                    .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.IsAvaliable));

                CreateMap<BookDTO, Book>()
                    .ForMember(dest => dest.IsAvaliable, opt => opt.MapFrom(src => src.IsAvailable))
                    .ForMember(dest => dest.Orders, opt => opt.Ignore());

                CreateMap<Order, OrderDTO>()
                .ForMember(dest => dest.Book, opt => opt.MapFrom(src => src.Book));

                CreateMap<OrderDTO, Order>()
                .ForMember(dest => dest.Book, opt => opt.Ignore());

            }
        }
}
