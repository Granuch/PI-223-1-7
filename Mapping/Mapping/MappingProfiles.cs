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

            // Order mappings
            CreateMap<Order, OrderDTO>()
                .ForMember(dest => dest.Book, opt => opt.MapFrom(src => src.Book));

            CreateMap<OrderDTO, Order>()
                .ForMember(dest => dest.Book, opt => opt.Ignore());

            // User mappings
            CreateMap<ApplicationUser, UserDTO>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.Roles, opt => opt.Ignore());

            CreateMap<UserDTO, ApplicationUser>()
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
                .ForMember(dest => dest.Orders, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore());

            // Role mapping
            CreateMap<ApplicationRole, RoleDTO>();
            CreateMap<RoleDTO, ApplicationRole>()
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedName, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore());

        }
    }

}



