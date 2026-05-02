using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class EstabelecimentoUsuarioRepository : Repository<EstabelecimentoUsuario>, IEstabelecimentoUsuarioRepository
{
    public EstabelecimentoUsuarioRepository(ApplicationDbContext context) : base(context)
    {
    }
}
