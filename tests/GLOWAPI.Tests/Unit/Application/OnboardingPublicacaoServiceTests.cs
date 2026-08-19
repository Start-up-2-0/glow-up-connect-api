using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class OnboardingPublicacaoServiceTests
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IAssinaturaRepository> _assinaturaRepository = new();
    private readonly Mock<IAssinaturaEstabelecimentoRepository> _assinaturaEstabelecimentoRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IServicoRepository> _servicoRepository = new();
    private readonly Mock<IProfissionalServicoRepository> _profissionalServicoRepository = new();
    private readonly Mock<IHorarioFuncionamentoEstabelecimentoRepository> _horarioFuncionamentoRepository = new();
    private readonly Mock<IHorarioAtendimentoProfissionalRepository> _horarioAtendimentoProfissionalRepository = new();

    [Fact]
    public async Task ObterStatusAsync_DeveMarcarOnboardingPendente_QuandoLojaIncompleta()
    {
        var estabelecimento = CriarEstabelecimentoLoja();
        ConfigurarAssinaturaAtiva(TipoAssinatura.Estabelecimento);
        ConfigurarLojaIncompleta();

        var service = CreateService();
        var status = await service.ObterStatusAsync(estabelecimento.Id);

        Assert.True(status.OnboardingObrigatorioPendente);
        Assert.False(status.ProntoParaPublicacao);
        Assert.Equal(OnboardingPublicacaoService.EtapaEquipe, status.ProximaEtapa);
    }

    [Fact]
    public async Task RecalcularVisibilidadeAsync_DevePublicar_QuandoLojaCompleta()
    {
        var estabelecimento = CriarEstabelecimentoLoja();
        ConfigurarAssinaturaAtiva(TipoAssinatura.Estabelecimento);
        ConfigurarLojaCompleta();

        Estabelecimento? capturado = null;
        _estabelecimentoRepository
            .Setup(r => r.Atualizar(It.IsAny<Estabelecimento>()))
            .Callback<Estabelecimento>(e => capturado = e);
        _estabelecimentoRepository
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();
        var visivel = await service.RecalcularVisibilidadeAsync(estabelecimento.Id);

        Assert.True(visivel);
        Assert.NotNull(capturado);
        Assert.True(capturado!.VisivelPublicamente);
    }

    [Fact]
    public async Task ObterStatusAsync_DeveExigirPerfilServicosHorarios_QuandoAutonomo()
    {
        var estabelecimento = CriarEstabelecimentoAutonomo(perfilCompleto: false);
        ConfigurarAssinaturaAtiva(TipoAssinatura.ProfissionalAutonomo);
        _servicoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(estabelecimento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _horarioAtendimentoProfissionalRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(estabelecimento.Id, null, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService();
        var status = await service.ObterStatusAsync(estabelecimento.Id);

        Assert.True(status.OnboardingObrigatorioPendente);
        Assert.Equal(OnboardingPublicacaoService.EtapaPerfil, status.ProximaEtapa);
    }

    private void ConfigurarAssinaturaAtiva(TipoAssinatura tipoAssinatura)
    {
        _assinaturaRepository
            .Setup(r => r.ObterAssinaturaEfetivaPorEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Assinatura
            {
                Id = 1,
                Status = AssinaturaStatus.Ativa,
                TipoAssinatura = tipoAssinatura,
                EstabelecimentoId = 10,
            });
    }

    private Estabelecimento CriarEstabelecimentoLoja()
    {
        var estabelecimento = new Estabelecimento
        {
            Id = 10,
            Ativo = true,
            VisivelPublicamente = false,
            Nome = "Barbearia",
            Logo = "data:image/png;base64,abc",
            Telefone = "11999999999",
            Email = "contato@loja.com",
            CategoriaEstabelecimentoId = 1,
            Endereco = new Endereco
            {
                Cep = "01310100",
                Logradouro = "Av Paulista",
                Numero = "1000",
                Bairro = "Bela Vista",
                Cidade = "Sao Paulo",
                Estado = "SP",
                Latitude = -23.561414m,
                Longitude = -46.655881m,
            },
        };

        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdComEnderecoAsync(estabelecimento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estabelecimento);

        return estabelecimento;
    }

    private Estabelecimento CriarEstabelecimentoAutonomo(bool perfilCompleto)
    {
        var estabelecimento = new Estabelecimento
        {
            Id = 11,
            Ativo = true,
            VisivelPublicamente = false,
            Nome = perfilCompleto ? "Joao" : "",
            Logo = perfilCompleto ? "data:image/png;base64,abc" : "",
            Telefone = perfilCompleto ? "11999999999" : "",
            Email = perfilCompleto ? "joao@mail.com" : "",
            CategoriaEstabelecimentoId = perfilCompleto ? 2 : null,
            Endereco = perfilCompleto
                ? new Endereco
                {
                    Cep = "01310100",
                    Logradouro = "Av Paulista",
                    Numero = "1000",
                    Bairro = "Bela Vista",
                    Cidade = "Sao Paulo",
                    Estado = "SP",
                    Latitude = -23.561414m,
                    Longitude = -46.655881m,
                }
                : null,
        };

        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdComEnderecoAsync(estabelecimento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estabelecimento);

        return estabelecimento;
    }

    private void ConfigurarLojaIncompleta()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosComAgendamentoPorEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _servicoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _profissionalServicoRepository
            .Setup(r => r.ExisteVinculoAtivoPorEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _horarioFuncionamentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(It.IsAny<int>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _horarioAtendimentoProfissionalRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(It.IsAny<int>(), null, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private void ConfigurarLojaCompleta()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosComAgendamentoPorEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProfissionalEstabelecimento { Ativo = true, PodeReceberAgendamento = true }]);
        _servicoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _profissionalServicoRepository
            .Setup(r => r.ExisteVinculoAtivoPorEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _horarioFuncionamentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(It.IsAny<int>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new HorarioFuncionamentoEstabelecimento { Ativo = true }]);
        _horarioAtendimentoProfissionalRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(It.IsAny<int>(), null, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new HorarioAtendimentoProfissional { Ativo = true }]);
    }

    private OnboardingPublicacaoService CreateService() =>
        new(
            _estabelecimentoRepository.Object,
            _assinaturaRepository.Object,
            _assinaturaEstabelecimentoRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _servicoRepository.Object,
            _profissionalServicoRepository.Object,
            _horarioFuncionamentoRepository.Object,
            _horarioAtendimentoProfissionalRepository.Object);
}
