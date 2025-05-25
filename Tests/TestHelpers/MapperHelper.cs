using AutoMapper;
using Mapping.Mapping;

namespace Tests.TestHelpers
{
    public static class MapperHelper
    {
        public static IMapper CreateMapper()
        {
            var config = new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            return config.CreateMapper();
        }
    }
}